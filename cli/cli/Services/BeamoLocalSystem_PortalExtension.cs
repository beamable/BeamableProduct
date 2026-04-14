using Beamable.Common.Content;
using Beamable.Server;
using Beamable.Server.Api.Notifications;
using Beamable.Server.Common;
using cli.Portal;
using cli.Services.PortalExtension;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry.Resources;
using System.Text.RegularExpressions;

namespace cli.Services;

 using ServiceLogs = Beamable.Common.Constants.Features.Services.Logs;

public partial class BeamoLocalSystem
{
	public static string GetBeamIdAsPortalExtension(string beamoId) => $"{beamoId}_portalExtension";
	
	public static readonly Regex PORTAL_EXTENSION_SERVICE_REGEX = new(@"^BeamPortalExtension_(?<serviceName>.+?)_(?<randomGuid>[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})$", RegexOptions.Compiled);
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
	/// <param name="onProgress">Optional callback invoked with (progressRatio, message) as the service starts. A ratio of 1 means the service is ready for traffic.</param>
	public async Task RunLocalPortalExtension(BeamoServiceDefinition serviceDefinition, BeamoLocalSystem localSystem, PortalExtensionConfig config, IAppContext appContext, ResourceBuilder resourceBuilder, Action<float, string> onProgress = null, CancellationToken token = default)
	{
		// Check for dependencies
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
		{
			throw new CliException("Portal Extension dependencies are missing");
		}

		await RunMicroserviceForever(serviceDefinition, localSystem, config, appContext, resourceBuilder, onProgress, token);
	}

	private async Task RunMicroserviceForever(BeamoServiceDefinition definition, BeamoLocalSystem localSystem, PortalExtensionConfig config, IAppContext appContext, ResourceBuilder resourceBuilder, Action<float, string> onProgress = null, CancellationToken token = default)
	{
		var newCliProvider = DefaultActivityProvider.CreateCliServiceProvider();
		var beamActivity = newCliProvider.Create($"Running Portal Extension: {definition.BeamoId}");
		
		beamActivity.SetTag(TelemetryAttributes.PortalExtensionName(definition.BeamoId));
		
		// Reset so each run gets a fresh sink.
		_portalExtensionSink = null;
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
						observer.ConfigureServiceData(notification, attributes, beamActivity, localSystem.BeamoManifest);
						observer.InstallDeps();
						observer.BuildExtension();
						
						// We can dispose beamActivity here now as we are only getting the Install and Build traces, as the 
						// Portal Extension run forever until we force it to stop, we don't need wait for that
						beamActivity.Dispose();
						
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
					var microserviceName = GetMicroName(extension.Name);
					microserviceConfig.Attributes = new DefaultMicroserviceAttributes()
					{
						MicroserviceName = microserviceName,
						ServiceType = GetServiceType(BeamoProtocolType.PortalExtension)
					};
					
					microserviceConfig.AddLoggerProvider = (builder, debugLogProcessor) => AddPortalExtensionProvider(builder, appContext, debugLogProcessor, microserviceName, onProgress);
				})
				.RunForever();
			
			beamActivity.Dispose();
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Portal Extension Discovery service failed to run. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}
	}

	// The stable sink from the first ConfigureLogging call, shared across both calls so
	// that pre-flight logs and first-logger messages all reach ContainerDiagnosticService.
	private DebugLogProcessor _portalExtensionSink;

	/// <summary>
	/// Called by <see cref="MicroserviceStartupUtil.ConfigureLogging"/> via
	/// <see cref="IBeamServiceConfig.AddLoggerProvider"/> for each of the two
	/// <c>ConfigureLogging</c> invocations.
	/// <list type="bullet">
	///   <item><b>First call</b> — registers <see cref="ExtensionAppLogProvider"/> with the
	///     provided <paramref name="debugLogProcessor"/> as the sink, pre-subscribes the
	///     <paramref name="microserviceName"/> channel, and returns the same sink.</item>
	///   <item><b>Second call</b> — registers a new <see cref="ExtensionAppLogProvider"/>
	///     pointing at the first (stable) sink, drains any messages buffered in the second
	///     temporary sink, and returns the first sink so <see cref="ContainerDiagnosticService"/>
	///     keeps using it.</item>
	/// </list>
	/// </summary>
	private DebugLogProcessor AddPortalExtensionProvider(ILoggingBuilder builder, IAppContext appContext, DebugLogProcessor debugLogProcessor, string microserviceName, Action<float, string> onProgress = null)
	{
		builder.ClearProviders();
		if (_portalExtensionSink == null)
		{
			// First ConfigureLogging call: this is the stable sink. Pre-subscribe using the
			// microservice name so messages produced before ContainerDiagnosticService
			// construction are buffered rather than dropped.
			_portalExtensionSink = debugLogProcessor;
			_portalExtensionSink.GetMessageSubscription(microserviceName);
			builder.AddProvider(new ExtensionAppLogProvider(_portalExtensionSink, appContext, onProgress));
			// Return the same sink; ctx.debugLogProcessor stays as-is.
			return debugLogProcessor;
		}

		// Second ConfigureLogging call: point the new builder at the stable first sink.
		builder.AddProvider(new ExtensionAppLogProvider(_portalExtensionSink, appContext, onProgress));
		// Drain any messages that landed in the temporary second sink into the channel
		// of the stable first sink, then discard the temporary subscription.
		var tmpEarly = debugLogProcessor.GetMessageSubscription(microserviceName);
		var earlyChannel = _portalExtensionSink.GetMessageSubscription(microserviceName);
		while (tmpEarly.Reader.TryRead(out var msg))
			earlyChannel.Writer.TryWrite(msg);
		debugLogProcessor.ReleaseSubscription(microserviceName);
		// Return the stable first sink so ctx.debugLogProcessor is updated to point at it.
		return _portalExtensionSink;
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
	
	public static bool IsMatchingPortalExtensionService(string service, string expectedServiceName)
	{
		if (service == expectedServiceName)
			return true;
		var match = PORTAL_EXTENSION_SERVICE_REGEX.Match(service ?? string.Empty);
		return match.Success && match.Groups["serviceName"].Value == expectedServiceName;
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
	private readonly DebugLogProcessor _sink;
	private readonly IAppContext _appContext;
	private readonly Action<float, string> _onProgress;

	public ExtensionAppLogProvider(DebugLogProcessor sink, IAppContext appContext, Action<float, string> onProgress = null)
	{
		_sink = sink;
		_appContext = appContext;
		_onProgress = onProgress;
	}

	public ILogger CreateLogger(string categoryName) => new ExtensionLogger(_sink, _appContext, _onProgress);

	public void Dispose() { }
}

public class ExtensionLogger : ILogger
{
	private readonly DebugLogProcessor _debugLogProcessor;
	private readonly IAppContext _appContext;
	private readonly Action<float, string> _onProgress;

	public ExtensionLogger(DebugLogProcessor debugLogProcessor, IAppContext appContext, Action<float, string> onProgress = null)
	{
		_debugLogProcessor = debugLogProcessor;
		_appContext = appContext;
		_onProgress = onProgress;
	}
	
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
		string message = formatter(state, exception);
		string parsedMessage = ParseMicroserviceLogMessages(message);

		// Signal readiness before any early-return so the callback always fires even
		// if the log level would otherwise suppress this message.
		if (message.StartsWith(ServiceLogs.READY_FOR_TRAFFIC_PREFIX))
		{
			_onProgress?.Invoke(1f, parsedMessage);
		}

		if (string.IsNullOrEmpty(parsedMessage))
		{
			return;
		}
		
		// This is send to the Logger service, we don't need to check for logLevel or if it is an exception.
		var ts = DateTimeOffset.UtcNow.ToString("o");
		var level = logLevel.ToString();
		var json =
			$"{{\"__t\":{JsonConvert.SerializeObject(ts)},\"__m\":{JsonConvert.SerializeObject(parsedMessage)},\"__l\":{JsonConvert.SerializeObject(level)}}}";
		_debugLogProcessor.WriteRawMessage(json);

		// Exceptions are always forwarded regardless of LogSwitch level.
		if (exception == null && _appContext.LogSwitch.Level > logLevel)
		{
			return;
		}
		BeamableZLoggerProvider.GlobalLogger.Log(logLevel, parsedMessage);
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
			case var _ when logMessage.StartsWith(ServiceLogs.READY_FOR_TRAFFIC_PREFIX):
				return ServiceLogs.PORTAL_EXTENSION_RUNNING;
			case var _ when logMessage.StartsWith(ServiceLogs.STARTING_PREFIX):
			case var _ when logMessage.StartsWith(ServiceLogs.SCANNING_CLIENT_PREFIX):
			case var _ when logMessage.StartsWith(ServiceLogs.REGISTERING_STANDARD_SERVICES):
			case var _ when logMessage.StartsWith(ServiceLogs.REGISTERING_CUSTOM_SERVICES):
			case var _ when logMessage.StartsWith(ServiceLogs.SERVICE_PROVIDER_INITIALIZED):
			case var _ when logMessage.StartsWith(ServiceLogs.EVENT_PROVIDER_INITIALIZED):
			case var _ when logMessage.StartsWith(ServiceLogs.STORAGE_READY):
			case var _ when logMessage.StartsWith(ServiceLogs.GENERATED_CLIENT_PREFIX):
				return string.Empty;
			default:
				return logMessage;
		}
	}
}
