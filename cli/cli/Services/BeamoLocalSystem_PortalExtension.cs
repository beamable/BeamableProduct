using Beamable.Common.Content;
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

	/// <summary>
	/// Runs a portal extension locally
	/// </summary>
	/// <param name="serviceDefinition"></param>
	public async Task RunLocalPortalExtension(BeamoServiceDefinition serviceDefinition, BeamoLocalSystem localSystem, PortalExtensionConfig config, IAppContext appContext, CancellationToken token = default)
	{
		// Check for dependencies
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
		{
			throw new CliException("Portal Extension dependencies are missing");
		}

		await RunMicroserviceForever(serviceDefinition, localSystem, config, appContext, token);
	}

	private async Task RunMicroserviceForever(BeamoServiceDefinition definition, BeamoLocalSystem localSystem, PortalExtensionConfig config, IAppContext appContext, CancellationToken token = default)
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
						Log.Error(e, $" Error while starting extension: {e.Message}. Stacktrace: {e.StackTrace}");
						throw;
					}

				})
				.ConfigureServices((dependency) =>
				{
					var observer = new PortalExtensionObserver
					{
						ExtensionMetaData = extension
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
				.OverrideConfig((microserviceConfig) =>
				{
					microserviceConfig.Attributes = new DefaultMicroserviceAttributes()
					{
						MicroserviceName = GetMicroName(extension.Name),
						ServiceType = GetServiceType(BeamoProtocolType.PortalExtension)
					};
					
					microserviceConfig.AddLoggerProvider = builder => AddPortalExtensionProvider(builder, appContext);
				})
				.RunForever();
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Portal Extension Discovery service failed to run. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}
	}

	private void AddPortalExtensionProvider(ILoggingBuilder builder, IAppContext appContext)
	{
		builder.ClearProviders();
		builder.AddProvider(new ExtensionAppLogProvider(appContext));
	}

	private string GetMicroName(string appName)
	{
		if (!string.IsNullOrEmpty(_computedMicroserviceName))
		{
			return _computedMicroserviceName;
		}

		_computedMicroserviceName = $"BeamPortalExtension_{appName}_{Guid.NewGuid()}";

		return _computedMicroserviceName;
	}

}

[Serializable]
public class PortalExtensionMountProperties
{
	public const string KEY_PAGE = "page";
	public const string KEY_SELECTOR = "selector";
	public const string KEY_NAV_GROUP = "navGroup";
	public const string KEY_NAV_LABEL = "navLabel";
	public const string KEY_NAV_ICON = "navIcon";
	public const string KEY_NAV_GROUP_ORDER = "navGroupOrder";
	public const string KEY_NAV_LABEL_ORDER = "navLabelOrder";
	
	[JsonProperty(KEY_PAGE)] public string Page;

	[JsonProperty(KEY_SELECTOR)] public string Selector;
	[JsonProperty(KEY_NAV_GROUP)] public OptionalString NavGroup;
	[JsonProperty(KEY_NAV_LABEL)] public OptionalString NavLabel;
	[JsonProperty(KEY_NAV_ICON)] public OptionalString NavIcon;
	[JsonProperty(KEY_NAV_GROUP_ORDER)] public OptionalInt NavGroupOrder;
	[JsonProperty(KEY_NAV_LABEL_ORDER)] public OptionalInt NavLabelOrder;

	[JsonProperty("args")] public Dictionary<string, string> Args = new Dictionary<string, string>();
}

[Serializable]
public class PortalExtensionPackageProperties
{
	[JsonProperty("version")] public string Version;

	[JsonProperty("portalExtension")] public bool IsPortalExtension;

	[JsonProperty("microserviceDependencies")]
	public List<string> MicroserviceDependencies;

	[JsonProperty("mount")] public PortalExtensionMountProperties Mount;
}

public class ExtensionAppLogProvider : ILoggerProvider
{
	private readonly IAppContext _appContext;

	public ExtensionAppLogProvider(IAppContext appContext)
	{
		_appContext = appContext;
	}
	
	public ILogger CreateLogger(string categoryName)
	{
		return new ExtensionLogger(_appContext);
	}

	public void Dispose()
	{
		// nothing to dispose for now
	}
}

public class ExtensionLogger : ILogger
{
	private readonly IAppContext _appContext;
	
	public ExtensionLogger(IAppContext appContext)
	{
		_appContext = appContext;
	}
	
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
		string message = formatter(state, exception);
		string parsedMessage = ParseMicroserviceLogMessages(message);

		// We always write Exceptions.
		if (exception != null)
		{
			BeamableZLoggerProvider.GlobalLogger.Log(logLevel, parsedMessage);
			return;
		}

		if (string.IsNullOrEmpty(parsedMessage))
		{
			return;
		}

		// Check if a non-microservice message matches the configured LogSwitch level.
		if (_appContext.LogSwitch.Level <= logLevel)
		{
			BeamableZLoggerProvider.GlobalLogger.Log(logLevel, parsedMessage);
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

	private static string ParseMicroserviceLogMessages(string logMessage)
	{
		switch (logMessage)
		{
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.READY_FOR_TRAFFIC_PREFIX): 
				return "Portal extension started successfully and is now running.";
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.STARTING_PREFIX):
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.SCANNING_CLIENT_PREFIX):
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.REGISTERING_STANDARD_SERVICES):
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.REGISTERING_CUSTOM_SERVICES):
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.SERVICE_PROVIDER_INITIALIZED):
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.EVENT_PROVIDER_INITIALIZED):
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.STORAGE_READY):
			case var _ when logMessage.StartsWith(Beamable.Common.Constants.Features.Services.Logs.GENERATED_CLIENT_PREFIX):
				return string.Empty;
			default:
				return logMessage;
		}
	}
}
