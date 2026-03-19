using Beamable.Common.Dependencies;

namespace Beamable.Server;


public enum LogOutputType
{
    DEFAULT, STRUCTURED, UNSTRUCTURED, FILE, STRUCTURED_AND_FILE
}

public interface IMicroserviceArgs : IRealmInfo, IActivityProviderArgs
{
    public IDependencyProviderScope ServiceScope { get; }
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
    void SetResolvedCid(string resolvedCid);
}
