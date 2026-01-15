using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Beamable.Editor.Utility
{
	public class UnityOtelManager : IBeamableDisposable
	{
		private const double DEFAULT_PUSH_TIME_INTERVAL = 120; // 2 minutes 
		private const double SECONDS_TO_AUTO_FLUSH = 60; // 5 minutes
		private const string ATTRIBUTES_EXTRA_TIMESTAMPS_KEY = "x-beam-extra-timestamps";
		private const string LAST_CRASH_PARSE_TIMESTAMP_FILE_NAME = "last-crash-parse-timestamps.txt";
		public const string SKIP_OTEL_LOG_KEY = "[SKIP_OTEL]";
		private const string SESSION_CRASH_CHECK_KEY = "Beamable_Otel_CrashLogsChecked";

		private double _lastTimePublished;
		private double _lastTimeFlush;
		OtelFileStatus _otelFileStatus;
		
		private ConcurrentDictionary<string, CliOtelLogRecord> _cachedLogs = new();
		private ConcurrentDictionary<string, List<string>> _cachedTimestamps = new();
		private readonly BeamCommands _cli;
		
		private List<string> BeamableNamespaces = new()
		{
			"Beamable.Editor",
			"Beamable.Commands",
			"Beamable.Config",
			"Beamable.Runtime",
			"com.Beamable"
		};
		
		private List<string> CrashMarkers = new()
		{
			"Crash!!!",
			"Fatal Error",
			"Segmentation fault",
			"Unhandled exception"
		};

		public bool TelemetryEnabled => _telemetryEnabled;

		private bool _telemetryEnabled;
		private long _telemetryMaxSize;
		private OtelLogLevel _telemetryLogLevel;
		private BeamCollectorStatusResult _collectorStatus;
		private readonly TelemetryCollectorPsWrapper _collectorCommandWatcher;

		private CoreConfiguration CoreConfig => CoreConfiguration.Instance;

		public OtelFileStatus OtelFileStatus => _otelFileStatus;

		public BeamCollectorStatusResult CollectorStatus => _collectorStatus;

		public static string UnityOtelLogsFolder => Path.Join(Application.temporaryCachePath, "OtelLogs");
		
		public UnityOtelManager(BeamCommands cli)
		{
			_cli = cli;
			EditorApplication.update += HandleUpdate;
			Application.logMessageReceived += OnUnityLogReceived;
			Application.wantsToQuit += OnUnityQuitting;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			CoreConfig.OnValidateCallback += OnCoreConfigChanged;
			
			_ = FetchOtelConfig();

			// As UnityOtelManager is instantiated for each domain reload
			// We only need to get UnparsedCrashLogs once per Unity session, doing it we prevent some performance issues.
			if (!SessionState.GetBool(SESSION_CRASH_CHECK_KEY, false))
			{
				_ = GetUnparsedCrashLogs();
				SessionState.SetBool(SESSION_CRASH_CHECK_KEY, true);
			}
			
			
			// Make sure to Publish on Init so we make sure that all missing logs that weren't published on last session
			if (CoreConfig.EnableOtelAutoPublish)
			{
				_ = PublishLogs();
			}

			_collectorCommandWatcher = _cli.TelemetryCollectorPs(new TelemetryCollectorPsArgs() {watch = true});
			_collectorCommandWatcher.OnStreamCollectorStatusResult(report =>
			{
				_collectorStatus = report.data;
			});
			_collectorCommandWatcher.OnExtraStreamOtelFileStatus(report =>
			{
				_otelFileStatus = report.data;
			});
			_collectorCommandWatcher.Run();
			
		}

		public Promise OnDispose()
		{
			EditorApplication.update -= HandleUpdate;
			Application.logMessageReceived -= OnUnityLogReceived;
			Application.wantsToQuit -= OnUnityQuitting;
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
			_collectorCommandWatcher?.Cancel();
			//await PublishLogs();
			return Promise.Success;
		}
		
		public void AddLog(string message, string stacktrace, string logLevel, Exception exception = null, Dictionary<string, string> extraAttributes = null)
		{
			// Log as Key to skip log addition
			if(message.Contains(SKIP_OTEL_LOG_KEY))
				return;
			string hashCode = GetLogKey(message, stacktrace, logLevel, exception, extraAttributes);
			string timestamp = DateTime.UtcNow.ToString("O");
			_cachedLogs.TryAdd(
				hashCode,
				new CliOtelLogRecord
				{
					Timestamp = timestamp,
					LogLevel = logLevel,
					Body = $"LogMessage: {message}{System.Environment.NewLine}Stacktrace: {stacktrace}",
					ExceptionMessage = exception?.Message,
					ExceptionStackTrace = exception?.StackTrace,
					Attributes = extraAttributes
				});
			if(!_cachedTimestamps.TryGetValue(hashCode, out List<string> timestamps))
			{
				_cachedTimestamps[hashCode] = timestamps = new List<string>();
			}

			timestamps.Add(timestamp);
			TryToFlushUnityLogs();
		}

		public async Promise PublishLogs(int maxLogFileCount = 10)
		{
			_lastTimePublished = EditorApplication.timeSinceStartup;
			try
			{
				await FlushUnityLogsAsync();
				if (Directory.Exists(UnityOtelLogsFolder))
				{
					string[] allFiles = Directory.GetFiles(UnityOtelLogsFolder).Take(maxLogFileCount).ToArray();
					if (allFiles.Length > 0)
					{
						List<TelemetryReportStatus> reportStatusList = new();
						var commandWrapper = _cli.TelemetryReport(new TelemetryReportArgs() {paths = allFiles});
						commandWrapper.OnStreamReportTelemetryResult(report => reportStatusList = report.data.AllStatus);
						await commandWrapper.Run();

						await Task.Run(() =>
						{
							foreach (var reportItem in reportStatusList.Where(item => item.Success))
							{
								File.Delete(reportItem.FilePath);
							}
						});
					}
				}

				await _cli.TelemetryPush(new TelemetryPushArgs(){ processId = Process.GetCurrentProcess().Id.ToString()}).Run();
			}
			catch (Exception e)
			{
				try { AddLog(e.Message, e.StackTrace, nameof(OtelLogLevel.Critical), e); }
				catch { Debug.LogError($"[UnityOtelManager] Failed to log exception: {e}"); }
			}
		}

		public async Promise PruneLogs()
		{
			_lastTimeFlush = _lastTimePublished = EditorApplication.timeSinceStartup;
			
			try
			{
				await _cli.TelemetryPrune(new TelemetryPruneArgs()
				{
					deleteAll = !CoreConfig.PruneRetainingDays.HasValue,
					retainingDays = CoreConfig.PruneRetainingDays.Value,
					processId = Process.GetCurrentProcess().Id.ToString()
				}).Run();
				string[] allFiles = Directory.GetFiles(UnityOtelLogsFolder);
				await Task.Run(() =>
				{
					foreach (string filePath in allFiles)
					{
						File.Delete(filePath);
					}
				});
			}
			catch (Exception e)
			{
				AddLog(e.Message, e.StackTrace, nameof(OtelLogLevel.Critical), e);
			}
		}

		

		private async Promise FetchOtelConfig()
		{
			await _cli.TelemetryConfig().OnStreamGetBeamOtelConfigCommandResult(report =>
			{
				_telemetryMaxSize = report.data.BeamCliTelemetryMaxSize;
				_telemetryLogLevel = Enum.TryParse(report.data.BeamCliTelemetryLogLevel, out OtelLogLevel logLevel) ? logLevel : _telemetryLogLevel;
				_telemetryEnabled = report.data.BeamCliAllowTelemetry;
			}).Run();
		}
		
		private void HandleUpdate()
		{
			TryToFlushUnityLogs();
			double now = EditorApplication.timeSinceStartup;
			if(!CoreConfiguration.Instance.EnableOtelAutoPublish)
				return;

			double timeSinceLastPublish = now - _lastTimePublished;
			double timeIntervalToPublish = CoreConfig.TimeInterval.HasValue ? CoreConfig.TimeInterval.Value : DEFAULT_PUSH_TIME_INTERVAL;
			if (timeSinceLastPublish >= timeIntervalToPublish)
			{
				_ = PublishLogs();
			}
		}

		private void OnUnityLogReceived(string message, string stackTrace, LogType type)
		{
			OtelLogLevel otelLogLevel = ConvertLogTypeToOtelLogType(type);
			OtelLogLevel minimumLogLevel = CoreConfig.TelemetryMinLogLevel.HasValue ? CoreConfig.TelemetryMinLogLevel.Value : OtelLogLevel.Warning;
			
			if (minimumLogLevel > otelLogLevel)
				return;
			
			if (!IsBeamableRelated(stackTrace))
			{
				return;
			}

			AddLog(message, stackTrace, otelLogLevel.ToString());
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is not Exception ex)
				return;

			if(!IsBeamableRelated(ex.StackTrace))
				return;
				
			AddLog(ex.Message, ex.StackTrace, nameof(OtelLogLevel.Critical), ex, null);
			if (e.IsTerminating)
			{
				_ = FlushUnityLogsAsync();
			}
		}
		
		private void OnCoreConfigChanged()
		{
			if (CoreConfig.TelemetryMaxSize.Value == _telemetryMaxSize && CoreConfig.TelemetryMinLogLevel.Value == _telemetryLogLevel && CoreConfig.EnableOtel.Value == _telemetryEnabled)
			{
				return;
			}
			_telemetryMaxSize = CoreConfig.TelemetryMaxSize.HasValue ? CoreConfig.TelemetryMaxSize.Value : _telemetryMaxSize;
			_telemetryLogLevel = CoreConfig.TelemetryMinLogLevel.HasValue ? CoreConfig.TelemetryMinLogLevel.Value : _telemetryLogLevel;
			_telemetryEnabled = CoreConfig.EnableOtel.HasValue ? CoreConfig.EnableOtel.Value : _telemetryEnabled;
			var commandWrapper = _cli.TelemetrySetConfig(new TelemetrySetConfigArgs()
			{
				cliLogLevel = _telemetryLogLevel.ToString(),
				cliTelemetryMaxSize = _telemetryMaxSize.ToString(),
				cliAllowTelemetry = _telemetryEnabled
			});
			commandWrapper.Run();
		}
		
		private bool IsBeamableRelated(string stackTrace)
		{
			bool hasBeamableReference = stackTrace.Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
			                                      .Any(line => BeamableNamespaces.Any(line.Contains));
			return !string.IsNullOrEmpty(stackTrace) && hasBeamableReference;
		}
		
		private bool OnUnityQuitting()
		{
			FlushUnityLogs();
			return true;
		}
		
		
		private async Task GetUnparsedCrashLogs()
		{
			string lastEditorSessionsLogs = GetEditorPrevLogPath();
			if (File.Exists(lastEditorSessionsLogs))
			{
				bool containsBeamableReference = false;
				bool containsCrashMarker = false;
				using var reader = new StreamReader(lastEditorSessionsLogs);
				while (await reader.ReadLineAsync() is { } line)
				{
					if (BeamableNamespaces.Any(line.Contains))
					{
						containsBeamableReference = true;
					}

					if (CrashMarkers.Any(line.Contains))
					{
						containsCrashMarker = true;
					}

					if (containsCrashMarker && containsBeamableReference)
					{
						reader.Close();
						string allText = await File.ReadAllTextAsync(lastEditorSessionsLogs);
						AddLog($"Unity Editor Crash - Project: {Application.companyName}.{Application.productName}", allText, nameof(OtelLogLevel.Critical));
						break;
					}
				}
			}
#if UNITY_EDITOR_WIN
			await TryGetWindowsCrashes();
#endif
		}
		
		private static string GetEditorPrevLogPath()
		{
#if UNITY_EDITOR_WIN
			string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			return Path.Combine(appData, "Unity", "Editor", "Editor-prev.log");
#elif UNITY_EDITOR_OSX
        string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        return Path.Combine(home, "Library", "Logs", "Unity", "Editor-prev.log");
#else
        string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        return Path.Combine(home, ".config", "unity3d", "Editor-prev.log");
#endif
		}
		
		#if UNITY_EDITOR_WIN
		private async Task TryGetWindowsCrashes()
		{
			string localAppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			string crashFolder = Path.Combine(localAppData, "Unity", "Editor", "Crashes");
			if (!Directory.Exists(crashFolder))
			{
				crashFolder = Path.Combine(localAppData, "Temp", "Unity", "Crashes");
			}
			if (!Directory.Exists(crashFolder))
			{
				crashFolder = Path.Combine(Path.GetTempPath(), "Unity", "Editor", "Crashes");
			}

			if (!Directory.Exists(crashFolder))
			{
				throw new Exception($"Could not find Crash Folders");
			}
			
			string lastParseFullPath = Path.Join(Application.temporaryCachePath, LAST_CRASH_PARSE_TIMESTAMP_FILE_NAME);
			if (!File.Exists(lastParseFullPath))
			{
				await File.WriteAllTextAsync(lastParseFullPath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
				return;
			}
			string fileContent = await File.ReadAllTextAsync(lastParseFullPath);
			if (!long.TryParse(fileContent, out long timestamp))
			{
				await File.WriteAllTextAsync(lastParseFullPath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
				return;
			}
			DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;

			string[] crashLogExtensions = {".log", ".txt", ".crash"};
			var allCrashes = Directory.GetFiles(crashFolder, "*", SearchOption.AllDirectories).Where(path => crashLogExtensions.Any(path.EndsWith));
			var unparsedCrashLogs = allCrashes.Where(filePath => File.GetLastAccessTime(filePath).ToUniversalTime() > dateTime).ToList();
			if (unparsedCrashLogs.Count > 0)
			{
				await ParseCrashLogs(unparsedCrashLogs);
			}
			await File.WriteAllTextAsync(lastParseFullPath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
		}

		private async Task ParseCrashLogs(List<string> crashLogsPath)
		{
			foreach (string crashPath in crashLogsPath)
			{
				try
				{
					bool containsBeamableReference = false;
					using var reader = new StreamReader(crashPath);
					while (await reader.ReadLineAsync() is { } line)
					{
						if (BeamableNamespaces.Any(line.Contains))
						{
							containsBeamableReference = true;
							break;
						}
					}

					if (containsBeamableReference)
					{
						string allText = await File.ReadAllTextAsync(crashPath);
						AddLog($"Unity Editor Crash - Project: {Application.companyName}.{Application.productName}", allText, nameof(OtelLogLevel.Critical));
					}
				}
				catch (Exception e)
				{
					// Error while reading the crash log, let's log it and skip it
					AddLog(e.Message, e.StackTrace, nameof(OtelLogLevel.Critical), e);
				}
			}
		}
		
		#endif
		
		private void TryToFlushUnityLogs()
		{
			
			double timeSinceLastFlush = EditorApplication.timeSinceStartup - _lastTimeFlush;
			if (timeSinceLastFlush < SECONDS_TO_AUTO_FLUSH)
				return;
			_ = FlushUnityLogsAsync();
		}

		private void FlushUnityLogs()
		{
			_lastTimeFlush = EditorApplication.timeSinceStartup;
			(string fileName, string jsonData) data = ParseUnityLogs();
			if (string.IsNullOrEmpty(data.fileName) || string.IsNullOrEmpty(data.jsonData))
			{
				return;
			}
			File.WriteAllText(Path.Join(UnityOtelLogsFolder, data.fileName), data.jsonData);
		}
		
		private async Task FlushUnityLogsAsync()
		{
			_lastTimeFlush = EditorApplication.timeSinceStartup;
			(string fileName, string jsonData) data = ParseUnityLogs();
			if (string.IsNullOrEmpty(data.fileName) || string.IsNullOrEmpty(data.jsonData))
			{
				return;
			}
			await File.WriteAllTextAsync(Path.Join(UnityOtelLogsFolder, data.fileName), data.jsonData);
		}
		
		private (string fileName, string jsonData) ParseUnityLogs()
		{
			string fileName = string.Empty;
			string jsonData = string.Empty;
			if(_cachedLogs.Count == 0)
			{
				return (fileName, jsonData);
			}
			Directory.CreateDirectory(UnityOtelLogsFolder);
			var groupedLogs = _cachedLogs.Select(kvp =>
			{
				var logData = kvp.Value;
				logData.Attributes ??= new Dictionary<string, string>();
				logData.Attributes[ATTRIBUTES_EXTRA_TIMESTAMPS_KEY] = $"[{string.Join(", ", _cachedTimestamps[kvp.Key])}]";
				return logData;
			}).ToList();
			_cachedLogs.Clear();
			_cachedTimestamps.Clear();
			
			CliOtelMessage otelMessage = new CliOtelMessage {allLogs = groupedLogs};
			jsonData = Json.Serialize(otelMessage, new StringBuilder());
			fileName = $"unityOtelLog-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.json";
			return (fileName, jsonData);
		}
		
		private static OtelLogLevel ConvertLogTypeToOtelLogType(LogType logType)
		{
			return logType switch
			{
				LogType.Error => OtelLogLevel.Error,
				LogType.Assert => OtelLogLevel.Information,
				LogType.Warning => OtelLogLevel.Warning,
				LogType.Log => OtelLogLevel.Information,
				LogType.Exception => OtelLogLevel.Critical,
				_ => OtelLogLevel.None
			};
		}

		private string GetLogKey(string message, string stacktrace, string logLevel, Exception exception = null, Dictionary<string, string> attributes = null)
        {
            string combined = $"{message}|{stacktrace}|{logLevel}";
			
            if (attributes != null && attributes.Count > 0)
            {
                combined += $"|{string.Join("|", attributes.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))}";
            }

            if (exception != null)
            {
	            combined += $"|{exception}";
            }
            
            return GenerateHash(combined);
        }
		
		private string GenerateHash(string input)
		{
			if(string.IsNullOrEmpty(input))
			{
				return string.Empty;
			}

			using (var md5 = MD5.Create())
			{
				byte[] inputBytes = Encoding.UTF8.GetBytes(input);
				byte[] hashBytes = md5.ComputeHash(inputBytes);
				return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
			}
		}
	}
}
