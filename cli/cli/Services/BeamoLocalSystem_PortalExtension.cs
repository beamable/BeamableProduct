using Beamable.Server;
using Beamable.Server.Api.Notifications;
using cli.Portal;
using cli.Services.PortalExtension;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	private string _computedMicroserviceName;

	public class PortalExtensionPackageInfo
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("beamable")]
		public PortalExtensionPackageProperties BeamableProperties { get; set; }
	}

	public class PortalExtensionPackageProperties
	{
		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("beamPortalExtension")]
		public bool IsPortalExtension { get; set; }

		[JsonProperty("portalExtensionType")]
		public string PortalExtensionType { get; set; }

		[JsonProperty("microserviceDependencies")]
		public List<string> MicroserviceDependencies { get; set; }
	}

	/// <summary>
	/// Runs a portal extension locally
	/// </summary>
	/// <param name="serviceDefinition"></param>
	public async Task RunLocalPortalExtension(BeamoServiceDefinition serviceDefinition, BeamoLocalSystem localSystem, IAppContext appContext, PortalExtensionConfig config, CancellationToken token = default)
	{
		// Check for dependencies
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
		{
			throw new CliException("Portal Extension dependencies are missing");
		}

		await RunMicroserviceForever(serviceDefinition, localSystem, appContext, config, token);
	}

	private async Task RunMicroserviceForever(BeamoServiceDefinition definition, BeamoLocalSystem localSystem, IAppContext appContext, PortalExtensionConfig config, CancellationToken token = default)
	{
		var extension = definition.PortalExtensionDefinition;
		try
		{
			Environment.SetEnvironmentVariable("BEAM_ALLOW_STARTUP_WITHOUT_ATTRIBUTES_RESOURCE", "true");
			await BeamServer
				.Create()
				.InitializeServices((provider) =>
				{
					try
					{
						var observer = provider.GetService<PortalExtensionObserver>();
						var notification = provider.GetService<IMicroserviceNotificationsApi>();
						var attributes = provider.GetService<MicroserviceAttribute>();

						observer.ConfigureServiceData(notification, attributes, localSystem.BeamoManifest);
						observer.InstallDeps();
						observer.BuildExtension();
						PortalExtensionAddDependencyCommand.GenerateDependenciesClients(extension.AbsolutePath,
							localSystem.BeamoManifest);
					}
					catch (CliException e)
					{
						Log.Error($" Error while starting extension: {e.Message}. Stacktrace: {e.StackTrace}");
					}

				})
				.ConfigureServices((dependency) =>
				{
					if (!Enum.TryParse(extension.Type, out PortalExtensionType type))
					{
						throw new CliException(
							$"Extension type = [{type}] could not be deserialized. The valid values for it are: [{string.Join(", ", Enum.GetNames(typeof(PortalExtensionType)))}]");
					}

					var observer = new PortalExtensionObserver();
					observer.AppFilesPath = extension.AbsolutePath;
					observer.ExtensionMetaData = new ExtensionBuildMetaData()
					{
						ExtensionName = extension.Name, ExtensionType = type.ToString()
					};

					if (config.fileExtensionsToObserve != null)
					{
						observer.FileExtensions =  config.fileExtensionsToObserve;
					}

					// Get the file observation started
					try
					{
						var _ = observer.StartExtensionFileWatcher(token);
					}
					catch (Exception e)
					{
						throw new CliException(
							$"Portal extension file observer failed. Error: [{e.Message}] StackTrace: [{e.StackTrace}]");
					}

					dependency.AddSingleton(observer);
				})
				.IncludeRoutes<PortalExtensionDiscoveryService>(routePrefix: "")
				.OverrideConfig((config) =>
				{
					config.Attributes = new DefaultMicroserviceAttributes()
					{
						MicroserviceName = GetMicroName(extension.Name),
						ServiceType = "PortalExtension"
					};

					config.AddLoggerProvider = (builder) =>
					{
						SetLoggerProvider(builder, appContext);
					};
				})
				.RunForever();
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Portal Extension Discovery service failed to run. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}
	}

	private void SetLoggerProvider(ILoggingBuilder builder, IAppContext appContext)
	{
		//TODO should we keep this? Or just get rid of it?
		Action readyForTraffic = () =>
		{
			if (TryBuildPortalUrl(appContext, out string portalUrl))
			{
				Console.WriteLine($"Portal URL: {portalUrl}");
			}
			else
			{
				Console.WriteLine("Couldn't generate Portal URL for extension app.");
			}
		};

		builder.ClearProviders();
		builder.AddProvider(new ExtensionAppLogProvider(readyForTraffic));
	}

	private string GetMicroName(string appName)
	{
		if (!string.IsNullOrEmpty(_computedMicroserviceName))
		{
			return _computedMicroserviceName;
		}

		_computedMicroserviceName = ComputedPortalExtensionName(appName);

		return _computedMicroserviceName;
	}

	public static string ComputedPortalExtensionName(string appName)
	{
		return $"BeamPortalExtension_{appName}";
	}

	private bool TryBuildPortalUrl(IAppContext context, out string portalUrl)
	{
		var cid = context.CustomerID;
		var pid = context.Pid;

		var treatedHost = context.Host.Replace("/socket", "")
			.Replace("wss", "https")
			.Replace("dev.", "dev-")
			.Replace("api", "portal");
		portalUrl = $"{treatedHost}/{cid}/games/{pid}/realms/{pid}/extensions";

		return true;
	}

}

public class ExtensionAppLogProvider : ILoggerProvider
{
	private Action _onExtensionAppReady;

	public ExtensionAppLogProvider(Action onExtensionAppReady = null)
	{
		_onExtensionAppReady = onExtensionAppReady;
	}

	public ILogger CreateLogger(string categoryName)
	{
		return (ILogger)new OverrideLogger(_onExtensionAppReady);
	}

	public void Dispose()
	{
		// nothing to dispose for now
	}
}

public class OverrideLogger : ILogger
{
	private Action _readyForTraffic;

	public OverrideLogger(Action readyForTraffic = null)
	{
		if (readyForTraffic != null)
		{
			_readyForTraffic += readyForTraffic;
		}
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
		string message = formatter(state, exception);

		if (exception != null)
		{
			throw new CliException(
				$"An exception happened while running local Portal Extension. Message = [{exception.Message}]\n Stacktrace = [{exception.StackTrace}]");
		}

		// Uncomment this to debug if something is going wrong with the local service
		//Console.WriteLine($"Portal Extension Local Microservice: {message}");

		if (message.Contains(Beamable.Common.Constants.Features.Services.Logs.READY_FOR_TRAFFIC_PREFIX))
		{
			_readyForTraffic?.Invoke();
		}
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}

	public IDisposable BeginScope<TState>(TState state) where TState : notnull
	{
		return NullDisposable.Instance;
	}

	class NullDisposable : IDisposable
	{
		public static readonly IDisposable Instance = new NullDisposable();

		public void Dispose()
		{
		}
	}
}
