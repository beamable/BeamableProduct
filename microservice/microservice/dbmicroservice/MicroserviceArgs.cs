using System;
using System.IO;

namespace Beamable.Server
{
   public interface IMicroserviceArgs : IRealmInfo
   {
      string Host { get; }
      string Secret { get; }
      string NamePrefix { get; }
      string SdkVersionBaseBuild { get; }
      string SdkVersionExecution { get; }
      bool WatchToken { get; }
      public bool DisableCustomInitializationHooks { get; }
      public bool EmitOtel { get; }
      public bool EmitOtelMetrics { get; }
      public bool OtelMetricsIncludeRuntimeInstrumentation { get; }
      public bool OtelMetricsIncludeProcessInstrumentation { get; }

      public string Environment {
	      get
	      {
		      if (string.IsNullOrEmpty(Host)) return "none";
		      if (Host.Contains("dev")) return "platform-dev";
		      if (Host.Contains("staging")) return "platform-staging";
		      return "platform-prod";
	      }
      }
      public bool RateLimitWebsocket { get; }
      public int RateLimitWebsocketTokens { get; }
      public int RateLimitWebsocketPeriodSeconds { get; }
      public int RateLimitWebsocketTokensPerPeriod { get; }
      public int RateLimitWebsocketMaxQueueSize { get; }
      public string LogLevel { get; }
      public bool DisableLogTruncate { get; }
      public int LogTruncateLimit { get; }
      public int LogMaxCollectionSize { get; }
      public int LogMaxDepth { get; }
      public int LogDestructureMaxLength { get; }
   }

   public class MicroserviceArgs : IMicroserviceArgs
   {
	 
	   public string CustomerID { get; set; }
	   public string ProjectName { get; set; }
	   public string Secret { get; set; }
	   public string Host { get; set; }
	   public string NamePrefix { get; set; }
	   public string SdkVersionBaseBuild { get; set; }
	   public string SdkVersionExecution { get; set; }
	   public bool WatchToken { get; set; }
	   public bool DisableCustomInitializationHooks { get; set; }
	   public bool EmitOtel { get; set; }
	   public bool EmitOtelMetrics { get; set; }
	   public bool OtelMetricsIncludeRuntimeInstrumentation { get; set; }
	   public bool OtelMetricsIncludeProcessInstrumentation { get; set; }
	   public bool RateLimitWebsocket { get; set; }
	   public int RateLimitWebsocketTokens { get; set; }
	   public int RateLimitWebsocketPeriodSeconds { get; set; }
	   public int RateLimitWebsocketTokensPerPeriod { get; set; }
	   public int RateLimitWebsocketMaxQueueSize { get; set; }
	   public string LogLevel { get; set; }
	   public bool DisableLogTruncate { get; set; }
	   public int LogTruncateLimit { get; set; }
	   public int LogMaxCollectionSize { get; set; }
	   public int LogMaxDepth { get; set; }
	   public int LogDestructureMaxLength { get; set; }
   }

   public static class MicroserviceArgsExtensions
   {
      public static IMicroserviceArgs Copy(this IMicroserviceArgs args)
      {
         return new MicroserviceArgs
         {
            CustomerID = args.CustomerID,
            ProjectName = args.ProjectName,
            Secret = args.Secret,
            Host = args.Host,
            NamePrefix = args.NamePrefix,
            SdkVersionBaseBuild = args.SdkVersionBaseBuild,
            SdkVersionExecution = args.SdkVersionExecution,
            WatchToken = args.WatchToken,
            DisableCustomInitializationHooks = args.DisableCustomInitializationHooks,
            EmitOtel = args.EmitOtel,
            EmitOtelMetrics = args.EmitOtelMetrics,
            OtelMetricsIncludeProcessInstrumentation = args.OtelMetricsIncludeProcessInstrumentation,
            OtelMetricsIncludeRuntimeInstrumentation = args.OtelMetricsIncludeRuntimeInstrumentation,
            RateLimitWebsocket = args.RateLimitWebsocket,
            RateLimitWebsocketTokens = args.RateLimitWebsocketTokens,
            RateLimitWebsocketMaxQueueSize = args.RateLimitWebsocketMaxQueueSize,
            RateLimitWebsocketPeriodSeconds = args.RateLimitWebsocketPeriodSeconds,
            RateLimitWebsocketTokensPerPeriod = args.RateLimitWebsocketTokensPerPeriod,
            LogLevel = args.LogLevel,
            LogMaxDepth = args.LogMaxDepth,
            LogTruncateLimit = args.LogTruncateLimit,
            LogDestructureMaxLength = args.LogDestructureMaxLength,
            LogMaxCollectionSize = args.LogMaxCollectionSize,
            DisableLogTruncate = args.DisableLogTruncate
         };
      }
   }

   public class EnviornmentArgs : IMicroserviceArgs
   {
      public string CustomerID => Environment.GetEnvironmentVariable("CID");
      public string ProjectName => Environment.GetEnvironmentVariable("PID");
      public string Host => Environment.GetEnvironmentVariable("HOST");
      public string Secret => Environment.GetEnvironmentVariable("SECRET");
      public string NamePrefix => Environment.GetEnvironmentVariable("NAME_PREFIX") ?? "";
      public string SdkVersionExecution => Environment.GetEnvironmentVariable("BEAMABLE_SDK_VERSION_EXECUTION") ?? "";
      public bool WatchToken => (Environment.GetEnvironmentVariable("WATCH_TOKEN")?.ToLowerInvariant() ?? "") == "true";
      public bool DisableCustomInitializationHooks => (Environment.GetEnvironmentVariable("DISABLE_CUSTOM_INITIALIZATION_HOOKS")?.ToLowerInvariant() ?? "") == "true";
      public bool EmitOtel => (Environment.GetEnvironmentVariable("EMIT_OTEL")?.ToLowerInvariant() ?? "") == "true";
      public bool EmitOtelMetrics => (Environment.GetEnvironmentVariable("EMIT_OTEL_METRICS")?.ToLowerInvariant() ?? "") == "true";
      public bool OtelMetricsIncludeRuntimeInstrumentation => (Environment.GetEnvironmentVariable("OTEL_INCLUDE_RUNTIME_INSTRUMENTATION")?.ToLowerInvariant() ?? "") == "true";
      public bool OtelMetricsIncludeProcessInstrumentation => (Environment.GetEnvironmentVariable("OTEL_INCLUDE_PROCESS_INSTRUMENTATION")?.ToLowerInvariant() ?? "") == "true";

      public bool RateLimitWebsocket => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WS_DISABLE_RATE_LIMIT"));

      public int RateLimitWebsocketTokens
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_TOKENS"), out var limit))
		      {
			      limit = 20;
		      }
		      return limit;
	      }
      }

      public int RateLimitWebsocketPeriodSeconds 
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_PERIOD_SECONDS"), out var limit))
		      {
			      limit = 60;
		      }
		      return limit;
	      }
      }
      public int RateLimitWebsocketTokensPerPeriod 
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_TOKENS_PER_PERIOD"), out var limit))
		      {
			      limit = 15;
		      }
		      return limit;
	      }
      }

      public int RateLimitWebsocketMaxQueueSize
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_MAX_QUEUE_SIZE"), out var limit))
		      {
			      limit = 10000;
		      }

		      return limit;
	      }
      }

      public string LogLevel => Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "debug";

      public bool DisableLogTruncate => (Environment.GetEnvironmentVariable("DISABLE_LOG_TRUNCATE")?.ToLowerInvariant() ?? "") == "true";

      public int LogTruncateLimit
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("LOG_TRUNCATE_LIMIT"), out var val))
		      {
			      val = 1000;
		      }

		      return val;
	      }
      }
      public int LogMaxCollectionSize {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("LOG_DESTRUCTURE_MAX_COLLECTION_SIZE"), out var val))
		      {
			      val = 5;
		      }

		      return val;
	      }
      }
      public int LogMaxDepth{
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("LOG_DESTRUCTURE_MAX_DEPTH"), out var val))
		      {
			      val = 3;
		      }

		      return val;
	      }
      }
      public int LogDestructureMaxLength{
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("LOG_DESTRUCTURE_MAX_LENGTH"), out var val))
		      {
			      val = 50;
		      }

		      return val;
	      }
      }
      public string SdkVersionBaseBuild => File.ReadAllText(".beamablesdkversion").Trim();
   }
}
