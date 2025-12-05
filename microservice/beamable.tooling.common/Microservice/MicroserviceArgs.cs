using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Util;
using Beamable.Server.Common;
using System;
using System.IO;
using System.Linq;

namespace Beamable.Server
{
	
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
		public string OapiGenLogLevel { get; set; }
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
		public string RefreshToken { get; set; }
		public long AccountId { get; set; }
		public string AccountEmail { get; set; }
		public int RequireProcessId { get; set; }
		public string OtelExporterOtlpProtocol { get; set; }
		public string OtelExporterOtlpEndpoint { get; set; }
		public string OtelExporterOtlpHeaders { get; set; }
		public bool OtelExporterShouldRetry { get; set; }
		public bool UseLocalOtel { get; set; }
		public bool OtelExporterStandardEnabled { get; set; }
		public string OtelExporterRetryMaxSize { get; set; }
		public bool AllowStartupWithoutBeamableSettings { get; set; }
		public bool SkipLocalEnv { get; set; }
		public bool SkipAliasResolve { get; set; }

		public void SetResolvedCid(string resolvedCid)
		{
			throw new NotImplementedException();
		}
	}

	public static class MicroserviceArgsExtensions
	{
		public static MicroserviceArgs Copy(this IMicroserviceArgs args, Action<MicroserviceArgs> configurator = null)
		{
			var next = new MicroserviceArgs
			{
				RefreshToken = args.RefreshToken,
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
				OapiGenLogLevel = args.OapiGenLogLevel,
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
				MetadataUrl = args.MetadataUrl,
				AccountId = args.AccountId,
				AccountEmail = args.AccountEmail,
				RequireProcessId = args.RequireProcessId,
				OtelExporterOtlpEndpoint = args.OtelExporterOtlpEndpoint,
				OtelExporterOtlpHeaders = args.OtelExporterOtlpHeaders,
				OtelExporterOtlpProtocol = args.OtelExporterOtlpProtocol,
				OtelExporterShouldRetry = args.OtelExporterShouldRetry,
				UseLocalOtel = args.UseLocalOtel,
				OtelExporterStandardEnabled = args.OtelExporterStandardEnabled,
				OtelExporterRetryMaxSize = args.OtelExporterRetryMaxSize,
				SkipLocalEnv = args.SkipLocalEnv,
				SkipAliasResolve = args.SkipAliasResolve,
				AllowStartupWithoutBeamableSettings = args.AllowStartupWithoutBeamableSettings
			};
			configurator?.Invoke(next);
			return next;
		}
	}

	public class EnvironmentArgs : IMicroserviceArgs
	{
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
		
		static long GetLongFromEnvironmentVariable(string key, long defaultValue)
		{
			if (!long.TryParse(Environment.GetEnvironmentVariable(key), out long val))
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

		public string RefreshToken => Environment.GetEnvironmentVariable("REFRESH_TOKEN");
		public string CustomerID => Environment.GetEnvironmentVariable("CID");
		public string ProjectName => Environment.GetEnvironmentVariable("PID");
		public IDependencyProviderScope ServiceScope { get; }

		private static int? _freeHealthPort = null;
		public int HealthPort
		{
			get
			{
				var inDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
				var defaultPort = Constants.Features.Services.HEALTH_PORT;
				if (!inDocker)
				{
					// if we aren't in docker, then we can't use a constant default port, because
					//  it is very likely that there will be a port collision.
					//  So, get a fresh port number, and store it in a static variable, so 
					//  it doesn't get re-generated for later calls. 
					_freeHealthPort ??= PortUtil.FreeTcpPort();
					defaultPort = _freeHealthPort.Value;
				}
				return GetIntFromEnvironmentVariable("HEALTH_PORT", defaultPort);
			}
		}

		public long AccountId => GetLongFromEnvironmentVariable("USER_ACCOUNT_ID", 0);
		public string AccountEmail => Environment.GetEnvironmentVariable("USER_EMAIL");

		public int RequireProcessId =>
			GetIntFromEnvironmentVariable(Beamable.Common.Constants.EnvironmentVariables.BEAM_REQUIRE_PROCESS_ID, 0);


		/// <summary>
		/// Sets the protocol in which the Otlp exporter will try sending telemetry data
		/// </summary>
		public string OtelExporterOtlpProtocol => Environment.GetEnvironmentVariable("BEAM_OTEL_EXPORTER_OTLP_PROTOCOL");

		/// <summary>
		/// Sets the endpoint in which the Otlp exporter will try sending telemetry data.
		/// In case the protocol is Http, then it should look like this: http://127.0.0.1:4348
		/// In case the protocol is Grpc, then it should look like this: 127.0.0.1:4348
		/// </summary>
		public string OtelExporterOtlpEndpoint => Environment.GetEnvironmentVariable("BEAM_OTEL_EXPORTER_OTLP_ENDPOINT");

		public bool OtelExporterShouldRetry =>
			string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEAM_DISABLE_RETRY_OTEL"));

		public bool OtelExporterStandardEnabled => (this.InDocker() || UseLocalOtel) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEAM_DISABLE_STANDARD_OTEL"));

		public string OtelExporterRetryMaxSize => Environment.GetEnvironmentVariable("BEAM_OTEL_RETRY_MAX_SIZE");
		public bool AllowStartupWithoutBeamableSettings => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEAM_ALLOW_STARTUP_WITHOUT_ATTRIBUTES_RESOURCE"));

		public string OtelExporterOtlpHeaders => Environment.GetEnvironmentVariable("BEAM_OTEL_EXPORTER_OTLP_HEADERS");
		public void SetResolvedCid(string resolvedCid)
		{
			//CustomerID = resolvedCid;
		}

		public string Host => Environment.GetEnvironmentVariable("HOST");
		public string Secret => Environment.GetEnvironmentVariable("SECRET");
		public string NamePrefix => Environment.GetEnvironmentVariable("NAME_PREFIX") ?? "";
		public string SdkVersionExecution => Environment.GetEnvironmentVariable("BEAMABLE_SDK_VERSION_EXECUTION") ?? "";
		public bool WatchToken => IsEnvironmentVariableTrue("WATCH_TOKEN");

		public bool DisableCustomInitializationHooks =>
			IsEnvironmentVariableTrue("DISABLE_CUSTOM_INITIALIZATION_HOOKS");

		public int RequestCancellationTimeoutSeconds => GetIntFromEnvironmentVariable("REQUEST_TIMEOUT_SECONDS", 10);

		public LogOutputType LogOutputType
		{
			get
			{
				var hasFilePath = !string.IsNullOrEmpty(LogOutputPath);
				

				var arg = Environment.GetEnvironmentVariable("LOG_TYPE")?.ToLowerInvariant();
				if (arg == null && hasFilePath)
				{
					return LogOutputType.FILE;
				}
				if (hasFilePath && !arg.Contains("file"))
				{
					arg += "+file";
				}
				
				switch (arg)
				{
					case "structured":
						return LogOutputType.STRUCTURED;
					case "unstructured":
						return LogOutputType.UNSTRUCTURED;
					case "file":
						return LogOutputType.FILE;
					
					case "structured+file":
					case "file+structured":
						return LogOutputType.STRUCTURED_AND_FILE;
					default:
						return LogOutputType.DEFAULT;
				}
			}
		}

		public string LogOutputPath => Environment.GetEnvironmentVariable("LOG_PATH");
		public string LogLevel => Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "info";
		public string OapiGenLogLevel => Environment.GetEnvironmentVariable("BEAM_OAPI_LOG_LEVEL") ?? "info";
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
		public string SdkVersionBaseBuild => BeamAssemblyVersionUtil.GetVersion<MicroserviceArgs>();

		public bool EnableDangerousDeflateOptions => IsEnvironmentVariableTrue("WS_ENABLE_DANGEROUS_DEFLATE_OPTIONS");
		public bool UseLocalOtel => IsEnvironmentVariableTrue("BEAM_LOCAL_OTEL");
		public string MetadataUrl => Environment.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4");
		
		public bool SkipLocalEnv => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEAM_SKIP_LOCAL_ENV"));
		public bool SkipAliasResolve => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEAM_SKIP_ALIAS_RESOLVE"));
	}

}
