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
    /// <summary>
    /// How long an outbound platform request waits for its reply, in seconds. 0 means no deadline.
    /// Set with <c>BEAM_OUTBOUND_REQUEST_TIMEOUT_SECONDS</c>.
    /// </summary>
    public int OutboundRequestTimeoutSeconds { get; }
    /// <summary>
    /// How long one websocket frame may stay blocked on send before the connection is treated as dead, in seconds.
    /// 0 turns the check off. Set with <c>WS_SEND_TIMEOUT_SECONDS</c>.
    /// </summary>
    public int SocketSendTimeoutSeconds { get; }
    /// <summary>
    /// How long a sent request may go unanswered, with no other reply arriving on its connection, before the
    /// connection is treated as broken, in seconds. 0 turns the check off. Set with <c>WS_STALL_TIMEOUT_SECONDS</c>.
    /// </summary>
    public int SocketStallTimeoutSeconds { get; }
    /// <summary>
    /// The websocket ping/pong keep-alive timeout, in seconds (net9 or later). 0 keeps unsolicited pongs only.
    /// Set with <c>WS_KEEPALIVE_TIMEOUT_SECONDS</c>.
    /// </summary>
    public int WebSocketKeepAliveTimeoutSeconds { get; }
    /// <summary>
    /// When true, request deadlines fail the caller and broken connections are replaced. When false (the default),
    /// those checks only log a warning and record a metric. Set with <c>BEAM_SOCKET_RELIABILITY_ENFORCE</c>.
    /// </summary>
    public bool EnforceSocketReliability { get; }
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
    public bool OtelExportAllSpans { get; }
    public string OtelExporterRetryMaxSize { get; }
    public bool AllowStartupWithoutBeamableSettings { get; }
    public int MaxUniqueEventBindingCount { get; }
    void SetResolvedCid(string resolvedCid);
}
