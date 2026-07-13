using Beamable.Common.Content;
using Beamable.Server;
using Beamable.Server.Api.Notifications;
using Beamable.Server.Common;
using cli.Portal;
using cli.Services.PortalExtension;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.CommandLine.Binding;
using System.Text.RegularExpressions;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace cli.Services;

 using ServiceLogs = Beamable.Common.Constants.Features.Services.Logs;

public partial class BeamoLocalSystem
{
	public static string GetBeamIdAsPortalExtension(string beamoId) => $"{beamoId}_portalExtension";

	public static readonly Regex PORTAL_EXTENSION_SERVICE_REGEX = new(@"^BeamPortalExtension_(?<serviceName>.+?)_(?<randomGuid>[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})$", RegexOptions.Compiled);

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
	public async Task RunLocalPortalExtension(BeamoServiceDefinition serviceDefinition, BeamoLocalSystem localSystem, PortalExtensionConfig config, IAppContext appContext, BeamActivity beamActivity, Action<float, string> onProgress = null, CancellationToken token = default)
	{
		// Check for dependencies
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
		{
			throw new CliException("Portal Extension dependencies are missing");
		}

		await RunMicroserviceForever(serviceDefinition, localSystem, config, appContext, beamActivity, onProgress, token);
	}

	private async Task RunMicroserviceForever(BeamoServiceDefinition definition, BeamoLocalSystem localSystem, PortalExtensionConfig config, IAppContext appContext, BeamActivity beamActivity, Action<float, string> onProgress = null, CancellationToken token = default)
	{
		var extension = definition.PortalExtensionDefinition;
		beamActivity.SetTag(TelemetryAttributes.PortalExtensionName(extension.Name));

		// The portal URL is resolved from the --portal-url override (parsed into the BindingContext)
		// falling back to the host; grab the BindingContext here while we still have the DI provider.
		var binding = _provider.GetService<BindingContext>();

		// The portal accepts either the numeric CID or the customer alias in the URL path. Resolve the
		// alias once (the call is cheap/cached) so the "open in browser" URL is human-readable; fall back
		// to the numeric CID if the customer has no alias or the lookup fails.
		string alias = null;
		try
		{
			var customer = await _realmApi.GetCustomerData();
			alias = customer?.Alias;
		}
		catch (Exception e)
		{
			Log.Debug($"Could not resolve customer alias for portal extension URL, falling back to CID. Error: {e.Message}");
		}

		// Per-call state so concurrent extensions in one `beam project run` invocation
		// don't share microservice names or log sinks.
		var runtime = new PortalExtensionRuntime(extension.Name);

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

						// Make sure each file: library dependency still points at its real location before we
						// install/build. Auto-repairs a moved library or throws a clear error if one is missing.
						PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(extension, _configService.BeamableWorkspace);

						observer.InstallDeps();

						// Fail fast if the extension and any of its libraries disagree on a shared package version
						// (@beamable/portal-toolkit, react) by comparing the installed versions against each library's
						// declared peerDependency ranges.
						PortalExtensionAddLibraryCommand.ValidateLibraryPeerDependencies(extension);

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
					microserviceConfig.Attributes = new DefaultMicroserviceAttributes()
					{
						MicroserviceName = runtime.MicroserviceName,
						ServiceType = GetServiceType(BeamoProtocolType.PortalExtension)
					};

					microserviceConfig.AddLoggerProvider = (builder, debugLogProcessor) =>
						runtime.AddLoggerProvider(builder, appContext, binding, alias, debugLogProcessor, extension, onProgress);
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

	/// <summary>
	/// Per-extension runtime state. One instance is created for each call to
	/// <see cref="RunMicroserviceForever"/> so that concurrent extensions hosted by a single
	/// `beam project run` invocation each get a unique microservice name and an independent
	/// log sink. The sink in particular must be per-extension because
	/// <see cref="MicroserviceStartupUtil.ConfigureLogging"/> calls back twice and the second
	/// call has to drain into the first call's stable sink.
	/// </summary>
	private sealed class PortalExtensionRuntime
	{
		public string MicroserviceName { get; }

		// The stable sink from the first ConfigureLogging call. Both calls land on this
		// instance so the second one can drain its buffered messages into the first sink.
		private DebugLogProcessor _sink;

		public PortalExtensionRuntime(string appName)
		{
			MicroserviceName = $"BeamPortalExtension_{appName}_{Guid.NewGuid()}";
		}

		/// <summary>
		/// Called by <see cref="MicroserviceStartupUtil.ConfigureLogging"/> via
		/// <see cref="IBeamServiceConfig.AddLoggerProvider"/> for each of the two
		/// <c>ConfigureLogging</c> invocations.
		/// <list type="bullet">
		///   <item><b>First call</b> — registers <see cref="ExtensionAppLogProvider"/> with the
		///     provided <paramref name="debugLogProcessor"/> as the sink, pre-subscribes the
		///     microservice-name channel, and returns the same sink.</item>
		///   <item><b>Second call</b> — registers a new <see cref="ExtensionAppLogProvider"/>
		///     pointing at the first (stable) sink, drains any messages buffered in the second
		///     temporary sink, and returns the first sink so <see cref="ContainerDiagnosticService"/>
		///     keeps using it.</item>
		/// </list>
		/// </summary>
		public DebugLogProcessor AddLoggerProvider(ILoggingBuilder builder, IAppContext appContext, BindingContext binding, string alias, DebugLogProcessor debugLogProcessor, PortalExtensionDef extension, Action<float, string> onProgress = null)
		{
			builder.ClearProviders();
			if (_sink == null)
			{
				// First ConfigureLogging call: this is the stable sink. Pre-subscribe using the
				// microservice name so messages produced before ContainerDiagnosticService
				// construction are buffered rather than dropped.
				_sink = debugLogProcessor;
				_sink.GetMessageSubscription(MicroserviceName);
				builder.AddProvider(new ExtensionAppLogProvider(_sink, appContext, binding, alias, extension, onProgress));
				// Return the same sink; ctx.debugLogProcessor stays as-is.
				return debugLogProcessor;
			}

			// Second ConfigureLogging call: point the new builder at the stable first sink.
			builder.AddProvider(new ExtensionAppLogProvider(_sink, appContext, binding, alias, extension, onProgress));
			// Drain any messages that landed in the temporary second sink into the channel
			// of the stable first sink, then discard the temporary subscription.
			var tmpEarly = debugLogProcessor.GetMessageSubscription(MicroserviceName);
			var earlyChannel = _sink.GetMessageSubscription(MicroserviceName);
			while (tmpEarly.Reader.TryRead(out var msg))
				earlyChannel.Writer.TryWrite(msg);
			debugLogProcessor.ReleaseSubscription(MicroserviceName);
			// Return the stable first sink so ctx.debugLogProcessor is updated to point at it.
			return _sink;
		}
	}

	public static string BuildPortalExtensionUrl(BindingContext binding, IAppContext context, string alias, PortalExtensionDef extension)
	{
		if (context == null)
		{
			return null;
		}

		var treatedHost = PortalCommand.GetPortalBaseUrl(binding, context, PortalType.Console);
		if (string.IsNullOrEmpty(treatedHost) || string.IsNullOrEmpty(context.Cid) || string.IsNullOrEmpty(context.Pid))
		{
			return null;
		}

		// Prefer the human-readable customer alias in the URL; fall back to the numeric CID
		// when no alias was resolved.
		var customerSegment = string.IsNullOrEmpty(alias) ? context.Cid : alias;
		var baseUrl = $"{treatedHost}/{customerSegment}/games/{context.Pid}/realms/{context.Pid}";
		// An extension can declare multiple mounts; pick the first one with a
		// non-empty page to use as the "open in browser" landing target. If a
		// future iteration adds explicit selection (e.g. a --mount flag), this
		// is where to plumb it.
		var mountPath = extension?.Properties?.Mounts?
			.Select(m => NormalizePortalExtensionMountPage(m?.Page))
			.FirstOrDefault(p => !string.IsNullOrEmpty(p));
		if (!string.IsNullOrEmpty(mountPath))
		{
			return $"{baseUrl}/{mountPath}?refresh_token={context.RefreshToken}";
		}

		return $"Invalid Mount Path for Extension {extension.Name}";
	}

	public static string BuildPortalExtensionReadyMessage(BindingContext binding, IAppContext context, string alias, PortalExtensionDef extension)
	{
		var portalUrl = BuildPortalExtensionUrl(binding, context, alias, extension);
		if (string.IsNullOrEmpty(portalUrl))
		{
			return ServiceLogs.PORTAL_EXTENSION_RUNNING;
		}

		return $"{ServiceLogs.PORTAL_EXTENSION_RUNNING} Portal URL: {portalUrl}";
	}

	public static string NormalizePortalExtensionMountPage(string mountPage)
	{
		return string.IsNullOrWhiteSpace(mountPage) ? null : mountPage.Trim().Trim('/');
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
	public const string KEY_NAV_DESCRIPTION = "navDescription";
	public const string KEY_NAV_COLOR = "navColor";
	public const string KEY_NAV_GROUP_ORDER = "navGroupOrder";
	public const string KEY_NAV_LABEL_ORDER = "navLabelOrder";
	public const string KEY_FILTERS = "filters";

	// For #extension-page mounts the page path defines the hub hierarchy: a single segment
	// declares a hub that is a full page of its own, and a nested path
	// places that extension inside the "cars" hub. The nav group is a separate,
	// additional way to organize extensions inside a given hub.
	[JsonProperty(KEY_PAGE)] public string Page;

	[JsonProperty(KEY_SELECTOR)] public string Selector;

	// Optional grouping used to organize extensions within a hub's navigation.
	[JsonProperty(KEY_NAV_GROUP)]
	[JsonConverter(typeof(OptionalConverter))]
	public OptionalString NavGroup;

	[JsonProperty(KEY_NAV_LABEL)]
	[JsonConverter(typeof(OptionalConverter))]
	public OptionalString NavLabel;

	[JsonProperty(KEY_NAV_ICON)]
	[JsonConverter(typeof(OptionalConverter))]
	public OptionalString NavIcon;

	// Short tagline used by the portal hub picker as the dropdown subtitle.
	// Only consumed for hub-declaration mounts (single-segment `Page`).
	[JsonProperty(KEY_NAV_DESCRIPTION)]
	[JsonConverter(typeof(OptionalConverter))]
	public OptionalString NavDescription;

	// CSS color for the hub-picker badge background — hex, CSS variable, or
	// gradient. Only consumed for hub-declaration mounts.
	[JsonProperty(KEY_NAV_COLOR)]
	[JsonConverter(typeof(OptionalConverter))]
	public OptionalString NavColor;

	[JsonProperty(KEY_NAV_GROUP_ORDER)]
	[JsonConverter(typeof(OptionalConverter))]
	public OptionalInt NavGroupOrder;

	[JsonProperty(KEY_NAV_LABEL_ORDER)]
	[JsonConverter(typeof(OptionalConverter))]
	public OptionalInt NavLabelOrder;

	[JsonProperty("args")] public Dictionary<string, string> Args = new Dictionary<string, string>();

	// Conditions the portal evaluates before mounting the extension into the
	// agentic portal. Keys are dotted paths (e.g. "account.role", "account.tier")
	// matched against the viewer's context; the CLI scaffolds these with default
	// values so the extension mounts everywhere until the author narrows them.
	[JsonProperty(KEY_FILTERS)] public Dictionary<string, string> Filters = new Dictionary<string, string>();
}

[Serializable]
public class PortalExtensionPackageProperties
{
	[JsonProperty("version")] public string Version;

	[JsonProperty("portalExtension")] public bool IsPortalExtension;

	[JsonProperty("portalExtensionLib")] public bool IsPortalExtensionLib;

	[JsonProperty("microserviceDependencies")]
	public List<string> MicroserviceDependencies;

	/// <summary>
	/// The service-group tags this extension belongs to. Mirrors the BeamServiceGroup
	/// (PROP_BEAM_SERVICE_GROUP) property used by microservices, letting extensions be
	/// included/excluded via --with-group/--without-group.
	/// </summary>
	[JsonProperty("serviceGroups")]
	public List<string> ServiceGroups;

	/// <summary>
	/// Mount declarations. An extension binds to every entry independently —
	/// each match triggers its own mount call with a distinct shadow root.
	/// A single bundle can contribute both a sidebar widget and a full page,
	/// or a hub-declaration extension can also seed a default nav item.
	/// </summary>
	[JsonProperty("mounts")] public List<PortalExtensionMountProperties> Mounts;
}

public class ExtensionAppLogProvider : ILoggerProvider
{
	private readonly DebugLogProcessor _sink;
	private readonly IAppContext _appContext;
	private readonly BindingContext _binding;
	private readonly string _alias;
	private readonly PortalExtensionDef _extension;
	private readonly Action<float, string> _onProgress;

	public ExtensionAppLogProvider(DebugLogProcessor sink, IAppContext appContext, BindingContext binding, string alias, PortalExtensionDef extension, Action<float, string> onProgress = null)
	{
		_sink = sink;
		_appContext = appContext;
		_binding = binding;
		_alias = alias;
		_extension = extension;
		_onProgress = onProgress;
	}

	public ILogger CreateLogger(string categoryName) => new ExtensionLogger(_sink, _appContext, _binding, _alias, _extension, _onProgress);

	public void Dispose() { }
}

public class ExtensionLogger : ILogger
{
	private readonly DebugLogProcessor _debugLogProcessor;
	private readonly IAppContext _appContext;
	private readonly BindingContext _binding;
	private readonly string _alias;
	private readonly PortalExtensionDef _extension;
	private readonly Action<float, string> _onProgress;

	public ExtensionLogger(DebugLogProcessor debugLogProcessor, IAppContext appContext, BindingContext binding, string alias, PortalExtensionDef extension, Action<float, string> onProgress = null)
	{
		_debugLogProcessor = debugLogProcessor;
		_appContext = appContext;
		_binding = binding;
		_alias = alias;
		_extension = extension;
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

	private string ParseMicroserviceLogMessages(string logMessage)
	{
		switch (logMessage)
		{
			case var _ when logMessage.StartsWith(ServiceLogs.READY_FOR_TRAFFIC_PREFIX):
				return BeamoLocalSystem.BuildPortalExtensionReadyMessage(_binding, _appContext, _alias, _extension);
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
