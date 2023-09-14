using Beamable.Common;
using Beamable.Common.Dependencies;
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
		public LogOutputType LogOutputType { get; }
		public string LogOutputPath { get; }
		public bool EnableDangerousDeflateOptions { get; }
		public string MetadataUrl { get; }
	}

	public enum LogOutputType
	{
		DEFAULT, STRUCTURED, UNSTRUCTURED, FILE
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
		public LogOutputType LogOutputType { get; set; }
		public string LogOutputPath { get; set; }
		public bool EnableDangerousDeflateOptions { get; set; }
		public string MetadataUrl { get; set; }
	}

	public static class MicroserviceArgsExtensions
	{
		public static IMicroserviceArgs Copy(this IMicroserviceArgs args, Action<MicroserviceArgs> configurator = null)
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
				LogOutputType = args.LogOutputType,
				LogOutputPath = args.LogOutputPath,
				EnableDangerousDeflateOptions = args.EnableDangerousDeflateOptions,
				MetadataUrl = args.MetadataUrl
			};
			configurator?.Invoke(next);
			return next;
		}
	}

	public class EnvironmentArgs : IMicroserviceArgs
	{
		public string CustomerID => Environment.GetEnvironmentVariable("CID");
		public string ProjectName => Environment.GetEnvironmentVariable("PID");
		public IDependencyProviderScope ServiceScope { get; }

		public int HealthPort
		{
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

		public bool WatchToken =>
			(Environment.GetEnvironmentVariable("WATCH_TOKEN")?.ToLowerInvariant() ?? "") == "true";

		public bool DisableCustomInitializationHooks =>
			(Environment.GetEnvironmentVariable("DISABLE_CUSTOM_INITIALIZATION_HOOKS")?.ToLowerInvariant() ?? "") ==
			"true";

		static bool IsEnvironmentVariableTrue(string key) =>
			(Environment.GetEnvironmentVariable(key)?.ToLowerInvariant() ?? string.Empty) == "true";

		static int GetIntFromEnvironmentVariable(string key, int defaultValue)
		{
			if (!int.TryParse(Environment.GetEnvironmentVariable(key), out int val))
			{
				val = defaultValue;
			}

			return val;
		}

		static double GetDoubleFromEnvironmentVariable(string key, double defaultValue)
		{
			if (!double.TryParse(Environment.GetEnvironmentVariable(key), out double val))
			{
				val = defaultValue;
			}

			return val;
		}

		public int RequestCancellationTimeoutSeconds => GetIntFromEnvironmentVariable("REQUEST_TIMEOUT_SECONDS", 10);

		public LogOutputType LogOutputType
		{
			get
			{
				if (!string.IsNullOrEmpty(LogOutputPath))
				{
					return LogOutputType.FILE;
				}

				var arg = Environment.GetEnvironmentVariable("LOG_TYPE")?.ToLowerInvariant();
				switch (arg)
				{
					case "structured":
						return LogOutputType.STRUCTURED;
					case "unstructured":
						return LogOutputType.UNSTRUCTURED;
					case "file":
						return LogOutputType.FILE;
					default:
						return LogOutputType.DEFAULT;
				}
			}
		}

		public string LogOutputPath => Environment.GetEnvironmentVariable("LOG_PATH");
		public string LogLevel => Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "debug";
		public bool DisableLogTruncate => IsEnvironmentVariableTrue("DISABLE_LOG_TRUNCATE");
		public int LogTruncateLimit => GetIntFromEnvironmentVariable("LOG_TRUNCATE_LIMIT", 1000);
		public int LogMaxCollectionSize => GetIntFromEnvironmentVariable("LOG_DESTRUCTURE_MAX_COLLECTION_SIZE", 15);
		public int LogMaxDepth => GetIntFromEnvironmentVariable("LOG_DESTRUCTURE_MAX_DEPTH", 3);

		public int LogDestructureMaxLength => GetIntFromEnvironmentVariable("LOG_DESTRUCTURE_MAX_LENGTH", 250);

		/// <summary>
		/// By default, rate limiting is on, so if you pass anything to WS_DISABLE_RATE_LIMIT, it'll disable it.
		/// </summary>
		public bool RateLimitWebsocket =>
			string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WS_DISABLE_RATE_LIMIT"));

		public int RateLimitWebsocketTokens => GetIntFromEnvironmentVariable("WS_RATE_LIMIT_TOKENS", 1000);
		public int RateLimitWebsocketPeriodSeconds => GetIntFromEnvironmentVariable("WS_RATE_LIMIT_PERIOD_SECONDS", 1);

		public int RateLimitWebsocketTokensPerPeriod =>
			GetIntFromEnvironmentVariable("WS_RATE_LIMIT_TOKENS_PER_PERIOD", 1000);

		public int BeamInstanceCount => GetIntFromEnvironmentVariable("BEAM_INSTANCE_COUNT", 10);

		public int RateLimitWebsocketMaxQueueSize =>
			GetIntFromEnvironmentVariable("WS_RATE_LIMIT_MAX_QUEUE_SIZE", 100000);

		public double RateLimitCPUMultiplierLow => GetDoubleFromEnvironmentVariable("WS_RATE_LIMIT_CPU_MULT_LOW", -.2);
		public double RateLimitCPUMultiplierHigh => GetDoubleFromEnvironmentVariable("WS_RATE_LIMIT_CPU_MULT_HIGH", .1);
		public int RateLimitCPUOffset => GetIntFromEnvironmentVariable("WS_RATE_LIMIT_CPU_OFFSET", 0);
		public int ReceiveChunkSize => GetIntFromEnvironmentVariable("WS_RECEIVE_CHUNK_SIZE", 65536);
		public int SendChunkSize => GetIntFromEnvironmentVariable("WS_SEND_CHUNK_SIZE", 65536);
		public string SdkVersionBaseBuild => File.ReadAllText(".beamablesdkversion").Trim();
		public bool EnableDangerousDeflateOptions => IsEnvironmentVariableTrue("WS_ENABLE_DANGEROUS_DEFLATE_OPTIONS");
		public string MetadataUrl => Environment.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4");
	}
}
