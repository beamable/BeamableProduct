using Beamable.Common.Dependencies;

namespace Beamable.Server;


public enum LogOutputType
{
    DEFAULT, STRUCTURED, UNSTRUCTURED, FILE, STRUCTURED_AND_FILE
}

public interface IMicroserviceArgs : IRealmInfo, IActivityProviderArgs
{
    public IDependencyProviderScope ServiceScope { get; }
    /// <summary>
    /// The zone id for a zone-scoped service. Empty/null for realm-scoped services (which use
    /// <see cref="IRealmInfo.ProjectName"/> instead).
    /// </summary>
    string Zid { get; }
    public int HealthPort { get; }
    string Host { get; }
    string Secret { get; }
		
    string SdkVersionBaseBuild { get; }
    string SdkVersionExecution { get; }
    bool WatchToken { get; }
    public bool DisableCustomInitializationHooks { get; }
    public string LogLevel { get; }
    public string OapiGenLogLevel { get; }
    public bool DisableLogTruncate { get; }
    public int LogTruncateLimit { get; }
    public int LogMaxCollectionSize { get; }
    public int LogMaxDepth { get; }
    public int LogDestructureMaxLength { get; }
    public bool RateLimitWebsocket { get; }
    public int RateLimitWebsocketTokens { get; }
    public int RateLimitWebsocketPeriodSeconds { get; }
    public int RateLimitWebsocketTokensPerPeriod { get; }
    public int RateLimitWebsocketMaxQueueSize { get; }
    public double RateLimitCPUMultiplierLow { get; }
    public double RateLimitCPUMultiplierHigh { get; }
    public int RateLimitCPUOffset { get; }
    public int ReceiveChunkSize { get; }
    public int SendChunkSize { get; }
    public int BeamInstanceCount { get; }
    public int RequestCancellationTimeoutSeconds { get; }
    public LogOutputType LogOutputType { get; }
    public string LogOutputPath { get; }
    public bool EnableDangerousDeflateOptions { get; }
    public bool DisableOutboundWsCompression { get; }
    public string MetadataUrl { get; }
    public string RefreshToken { get; }
    public string AccountEmail { get; }
    public long AccountId { get; }
    public int RequireProcessId { get; }
    public string OtelExporterOtlpProtocol { get; }
    public string OtelExporterOtlpEndpoint { get; }
    public string OtelExporterOtlpHeaders { get; }
    public bool UseLocalOtel { get; }
    public bool SkipLocalEnv { get; }
    public bool SkipAliasResolve { get; }
    public bool OtelExporterShouldRetry { get; }
    public bool OtelExporterStandardEnabled { get; }
    public string OtelExporterRetryMaxSize { get; }
    public bool AllowStartupWithoutBeamableSettings { get; }
    public int MaxUniqueEventBindingCount { get; }

    /// <summary>
    /// Seconds a request from this service to the Beamable platform (over the websocket) may wait for its
    /// response before it is failed with a timeout. A response that never arrives (dropped by the gateway,
    /// or the socket died mid-request) would otherwise pin the awaiting request handler forever.
    /// 0 disables the timeout. Environment variable: BEAM_PLATFORM_REQUEST_TIMEOUT_SECONDS (default 30).
    /// </summary>
    public int PlatformRequestTimeoutSeconds { get; }

    /// <summary>
    /// Maximum number of client requests this process will handle at the same time. Requests beyond the
    /// limit are answered immediately with a 503 so the gateway retries them on another instance instead
    /// of queueing them behind a backlog that cannot be served inside the gateway's timeout.
    /// 0 disables the limit. Environment variable: BEAM_MAX_CONCURRENT_REQUESTS (default 500).
    /// </summary>
    public int MaxConcurrentRequests { get; }

    /// <summary>
    /// Floor applied to the .NET thread pool's minimum worker and IO thread counts. The runtime default is
    /// the processor count, which is 1 on a 0.25 vCPU task; a single blocking call in a request handler then
    /// serializes every other request behind it while the pool slowly injects threads.
    /// 0 keeps the runtime default. Environment variable: BEAM_MIN_THREADPOOL_THREADS (default 32).
    /// </summary>
    public int MinThreadPoolThreads { get; }

    /// <summary>
    /// When true, requests whose gateway deadline (the X-BEAM-DEADLINE header) has already passed are
    /// processed anyway. By default they are dropped, because the gateway has already answered the caller
    /// with a timeout and will discard the late response.
    /// Environment variable: BEAM_DISABLE_STALE_REQUEST_DROP.
    /// </summary>
    public bool DisableStaleRequestDrop { get; }

    /// <summary>
    /// Seconds to wait for the gateway to answer a websocket ping before the connection is considered dead
    /// and re-established (requires net9.0 or later). 0 disables the check.
    /// Environment variable: WS_KEEP_ALIVE_TIMEOUT_SECONDS (default 30).
    /// </summary>
    public int WebsocketKeepAliveTimeoutSeconds { get; }

    void SetResolvedCid(string resolvedCid);
}
