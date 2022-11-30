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
      public int RateLimitWebsocketPeriodMinutes { get; }
      public int RateLimitWebsocketTokensPerPeriod { get; }
      public int RateLimitWebsocketMaxQueueSize { get; }
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
      public int RateLimitWebsocketPeriodMinutes { get; set; }
      public int RateLimitWebsocketTokensPerPeriod { get; set; }
      public int RateLimitWebsocketMaxQueueSize { get; set; }
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
            RateLimitWebsocketPeriodMinutes = args.RateLimitWebsocketPeriodMinutes,
            RateLimitWebsocketTokensPerPeriod = args.RateLimitWebsocketTokensPerPeriod
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

      public bool RateLimitWebsocket => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WS_RATE_LIMIT"));

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

      public int RateLimitWebsocketPeriodMinutes 
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_PERIOD_MINUTES"), out var limit))
		      {
			      limit = 1;
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
      public string SdkVersionBaseBuild => File.ReadAllText(".beamablesdkversion").Trim();
   }
}
