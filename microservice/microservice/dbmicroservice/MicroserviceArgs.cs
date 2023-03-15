using Beamable.Common;
using Beamable.Common.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Beamable.Server
{
	public interface IMicroserviceArgs : IRealmInfo
	{
		public IDependencyProviderScope ServiceScope { get; }
		public int HealthPort { get; }
		string Host { get; }
		string Secret { get; }
		string NamePrefix { get; }
		string SdkVersionBaseBuild { get; }
		string SdkVersionExecution { get; }
		bool WatchToken { get; }
		public bool DisableCustomInitializationHooks { get; }
		public string LogLevel { get; }
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
		public bool ForceStructuredLogs { get; }
		public bool ForceUnstructuredLogs { get; }
	}

	public class MicroserviceArgs : IMicroserviceArgs
   {
	   public IDependencyProviderScope ServiceScope { get; set; }
	   public int HealthPort { get; set; }
	   public string CustomerID { get; set; }
	   public string ProjectName { get; set; }
	   public string Secret { get; set; }
	   public string Host { get; set; }
	   public string NamePrefix { get; set; }
	   public string SdkVersionBaseBuild { get; set; }
	   public string SdkVersionExecution { get; set; }
	   public bool WatchToken { get; set; }
	   public bool DisableCustomInitializationHooks { get; set; }
	   public string LogLevel { get; set; }
	   public bool DisableLogTruncate { get; set; }
	   public int LogTruncateLimit { get; set; }
	   public int LogMaxCollectionSize { get; set; }
	   public int LogMaxDepth { get; set; }
	   public int LogDestructureMaxLength { get; set; }
	   public bool RateLimitWebsocket { get; set; }
	   public int RateLimitWebsocketTokens { get; set; }
	   public int RateLimitWebsocketPeriodSeconds { get; set; }
	   public int RateLimitWebsocketTokensPerPeriod { get; set; }
	   public int RateLimitWebsocketMaxQueueSize { get; set; }
	   public double RateLimitCPUMultiplierLow { get; set; }
	   public double RateLimitCPUMultiplierHigh { get; set; }
	   public int RateLimitCPUOffset { get; set; }
	   public int ReceiveChunkSize { get; set; }
	   public int SendChunkSize { get; set; }
	   public int BeamInstanceCount { get; set; }
	   public int RequestCancellationTimeoutSeconds { get; set; }
	   public bool ForceStructuredLogs { get; set; }
	   public bool ForceUnstructuredLogs { get; set; }
   }

   public static class MicroserviceArgsExtensions
   {
      public static IMicroserviceArgs Copy(this IMicroserviceArgs args, Action<MicroserviceArgs> configurator=null)
      {
         var next = new MicroserviceArgs
         {
	         ServiceScope = args.ServiceScope,
            CustomerID = args.CustomerID,
            ProjectName = args.ProjectName,
            Secret = args.Secret,
            Host = args.Host,
            NamePrefix = args.NamePrefix,
            SdkVersionBaseBuild = args.SdkVersionBaseBuild,
            SdkVersionExecution = args.SdkVersionExecution,
            WatchToken = args.WatchToken,
            DisableCustomInitializationHooks = args.DisableCustomInitializationHooks,
            LogLevel = args.LogLevel,
            LogMaxDepth = args.LogMaxDepth,
            LogTruncateLimit = args.LogTruncateLimit,
            LogDestructureMaxLength = args.LogDestructureMaxLength,
            LogMaxCollectionSize = args.LogMaxCollectionSize,
            DisableLogTruncate = args.DisableLogTruncate,
            RateLimitWebsocket = args.RateLimitWebsocket,
            RateLimitWebsocketTokens = args.RateLimitWebsocketTokens,
            RateLimitWebsocketMaxQueueSize = args.RateLimitWebsocketMaxQueueSize,
            RateLimitWebsocketPeriodSeconds = args.RateLimitWebsocketPeriodSeconds,
            RateLimitWebsocketTokensPerPeriod = args.RateLimitWebsocketTokensPerPeriod,
            SendChunkSize = args.SendChunkSize,
            ReceiveChunkSize = args.ReceiveChunkSize,
            RateLimitCPUMultiplierLow = args.RateLimitCPUMultiplierLow,
            RateLimitCPUMultiplierHigh = args.RateLimitCPUMultiplierHigh,
            RateLimitCPUOffset = args.RateLimitCPUOffset,
            BeamInstanceCount = args.BeamInstanceCount,
            RequestCancellationTimeoutSeconds = args.RequestCancellationTimeoutSeconds,
            HealthPort = args.HealthPort,
            ForceStructuredLogs = args.ForceStructuredLogs,
            ForceUnstructuredLogs = args.ForceUnstructuredLogs
         };
         
         configurator?.Invoke(next);
         return next;
      }
   }

   public class EnviornmentArgs : IMicroserviceArgs
   {
      public string CustomerID => Environment.GetEnvironmentVariable("CID");
      public string ProjectName => Environment.GetEnvironmentVariable("PID");
      public IDependencyProviderScope ServiceScope { get; }
      public int HealthPort {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("HEALTH_PORT"), out var val))
		      {
			      val = Constants.Features.Services.HEALTH_PORT;
		      }
		      return val;
	      }
      }
      public string Host => Environment.GetEnvironmentVariable("HOST");
      public string Secret => Environment.GetEnvironmentVariable("SECRET");
      public string NamePrefix => Environment.GetEnvironmentVariable("NAME_PREFIX") ?? "";
      public string SdkVersionExecution => Environment.GetEnvironmentVariable("BEAMABLE_SDK_VERSION_EXECUTION") ?? "";
      public bool WatchToken => (Environment.GetEnvironmentVariable("WATCH_TOKEN")?.ToLowerInvariant() ?? "") == "true";
      public bool DisableCustomInitializationHooks => (Environment.GetEnvironmentVariable("DISABLE_CUSTOM_INITIALIZATION_HOOKS")?.ToLowerInvariant() ?? "") == "true";

      public int RequestCancellationTimeoutSeconds {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("REQUEST_TIMEOUT_SECONDS"), out var val))
		      {
			      val = 10;
		      }
		      return val;
	      }
      }

      public bool ForceStructuredLogs => (Environment.GetEnvironmentVariable("FORCE_STRUCTURED_LOGS")?.ToLowerInvariant() ?? "") == "true";
      public bool ForceUnstructuredLogs => (Environment.GetEnvironmentVariable("FORCE_UNSTRUCTURED_LOGS")?.ToLowerInvariant() ?? "") == "true";
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
			      val = 15;
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

      public int LogDestructureMaxLength
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("LOG_DESTRUCTURE_MAX_LENGTH"), out var val))
		      {
			      val = 250;
		      }

		      return val;
	      }
      }

      /// <summary>
      /// By default, rate limiting is on, so if you pass anything to WS_DISABLE_RATE_LIMIT, it'll disable it.
      /// </summary>
      public bool RateLimitWebsocket => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WS_DISABLE_RATE_LIMIT"));

      public int RateLimitWebsocketTokens
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_TOKENS"), out var limit))
		      {
			      limit = 1000;
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
			      limit = 1000;
		      }
		      return limit;
	      }
      }
      
      public int BeamInstanceCount 
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("BEAM_INSTANCE_COUNT"), out var limit))
		      {
			      limit = 10;
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
			      limit = 100000;
		      }
		      return limit;
	      }
      }

      public double RateLimitCPUMultiplierLow
      {
	      get
	      {
		      if (!double.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_CPU_MULT_LOW"), out var limit))
		      {
			      limit = -.2;
		      }
		      return limit;
	      }
      }

      public double RateLimitCPUMultiplierHigh
      {
	      get
	      {
		      if (!double.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_CPU_MULT_HIGH"), out var limit))
		      {
			      limit = .1;
		      }
		      return limit;
	      }
      }

      public int RateLimitCPUOffset 
      {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RATE_LIMIT_CPU_OFFSET"), out var limit))
		      {
			      limit = 0;
		      }
		      return limit;
	      }
      }


      public int ReceiveChunkSize {
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_RECEIVE_CHUNK_SIZE"), out var limit))
		      {
			      limit = 65536;
		      }
		      return limit;
	      }
      }

      public int SendChunkSize{
	      get
	      {
		      if (!int.TryParse(Environment.GetEnvironmentVariable("WS_SEND_CHUNK_SIZE"), out var limit))
		      {
			      limit = 65536;
		      }
		      return limit;
	      }
      }

      public string SdkVersionBaseBuild => File.ReadAllText(".beamablesdkversion").Trim();
   }
}
