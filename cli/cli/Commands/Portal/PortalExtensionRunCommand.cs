using Beamable.Common.Api;
using Beamable.Server;
using cli.Services.PortalExtension;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace cli.Portal;

public class PortalExtensionRunCommandArgs : CommandArgs
{
	public string AppName;
}

public class PortalExtensionRunCommand : AppCommand<PortalExtensionRunCommandArgs>
{
	private CancellationTokenSource _tokenSource;
	private string ComputedMicroserviceName;

	public PortalExtensionRunCommand() : base("run", "Runs the specified Portal Extension project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("portal-extension-name", "The name of the portal extension to run"), (arg, i) => arg.AppName = i);
	}

	public override async Task Handle(PortalExtensionRunCommandArgs args)
	{
		// Check for dependencies
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
		{
			throw new CliException("Portal Extension dependencies are missing");
		}

		// Check if service exists
		var service =
			args.BeamoLocalSystem.BeamoManifest.PortalExtensionDefinitions.FirstOrDefault(p => p.Name == args.AppName);

		if (service == null)
		{
			throw new CliException($"Couldn't find a Portal Extension service with the name: [{args.AppName}]");
		}

		// run a microservice that will be feeding portal with new builds of the portal extension app
		await RunMicroserviceForever(service.AbsolutePath, args);
	}

	private async Task RunMicroserviceForever(string fullPath, PortalExtensionRunCommandArgs args)
	{
		try
		{
			//TODO put this into a background worker, show the portal URL to the extensions page
			await BeamServer
				.Create()
				.ConfigureServices((dependency) =>
				{
					var observer = new PortalExtensionObserver();
					observer.AppFilesPath = fullPath;

					// Get the file observation started
					_tokenSource = new CancellationTokenSource();
					try
					{
						var _ = observer.StartExtensionFileWatcher(_tokenSource.Token);
					}
					catch (Exception e)
					{
						throw new CliException(
							$"Portal extension file observer failed. Error: [{e.Message}] StackTrace: [{e.StackTrace}]");
					}

					dependency.AddSingleton(observer);
					Log.Information("Created the observer service");
				})
				.IncludeRoutes<PortalExtensionDiscoveryService>(routePrefix: "")
				.OverrideConfig((config) =>
				{
					config.Attributes = new DefaultMicroserviceAttributes()
					{
						MicroserviceName = GetMicroName(args.AppName)
					};

					config.AddLoggerProvider = (builder) =>
					{
						SetLoggerProvider(builder, args);
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

	private void SetLoggerProvider(ILoggingBuilder builder, PortalExtensionRunCommandArgs args)
	{
		Action readyForTraffic = () =>
		{
			if (TryBuildPortalUrl(args, out string portalUrl))
			{
				Log.Information($"Portal URL: {portalUrl}");
			}
		};

		builder.ClearProviders();
		builder.AddProvider(new ExtensionAppLogProvider(readyForTraffic));
	}

	private string GetMicroName(string appName)
	{
		if (!string.IsNullOrEmpty(ComputedMicroserviceName))
		{
			return ComputedMicroserviceName;
		}

		return $"BeamPortalExtension_{appName}_{Guid.NewGuid()}";
	}

	private bool TryBuildPortalUrl(PortalExtensionRunCommandArgs args, out string portalUrl)
	{
		var cid = args.AppContext.CustomerID;
		var pid = args.AppContext.Pid;
		var microName = "micro_" + GetMicroName(args.AppName);
		var refreshToken = args.AppContext.RefreshToken;

		if (string.IsNullOrEmpty(refreshToken))
		{
			Log.Verbose("not generating portal url, because no refresh token exists.");
			portalUrl = "";
			return false;
		}

		var queryArgs = new List<string>
		{
			$"refresh_token={refreshToken}",
			$"routingKey={ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine()}"
		};
		var joinedQueryString = string.Join("&", queryArgs);
		var treatedHost = args.AppContext.Host.Replace("/socket", "")
			.Replace("wss", "https")
			.Replace("dev.", "dev-")
			.Replace("api", "portal");
		portalUrl = $"{treatedHost}/{cid}/games/{pid}/realms/{pid}/microservices/{microName}/extensions?{joinedQueryString}";

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

	public bool SupportScopes => false;

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
			Console.WriteLine($"\nTEST Exception: {exception.Message}");
		}

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
