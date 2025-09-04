using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Utility
{
	public class UnityOtelManager : IBeamableDisposable
	{
		private const double DEFAULT_TIME_INTERVAL = 30; 
		private const double SECONDS_TO_AUTO_FLUSH = 300; // 5 minutes
		private const double OTEL_CHECK_DELAY = 60;
		private const string ATTRIBUTES_EXTRA_TIMESTAMPS_KEY = "x-beam-extra-timestamps";
		private const string LAST_CRASH_PARSE_TIMESTAMP_FILE_NAME = "last-crash-parse-timestamps.txt";

		private double _lastTimePublished;
		private double _lastTimeFlush;
		private double _lastOtelStatusRefresh;
		BeamOtelStatusResult _otelStatus;
		private OtelManagerStatus _otelManagerStatus = OtelManagerStatus.Normal;
		
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

		private long _telemetryMaxSize;
		private OtelLogLevel _telemetryLogLevel;

		private CoreConfiguration CoreConfig => CoreConfiguration.Instance;

		public BeamOtelStatusResult OtelStatus => _otelStatus;

		public OtelManagerStatus OtelManagerStatus => _otelManagerStatus;

		public static string UnityOtelLogsFolder => Path.Join(Application.temporaryCachePath, "OtelLogs");
		
		public UnityOtelManager(BeamCommands cli)
		{
			_cli = cli;
			EditorApplication.update += HandleUpdate;
			Application.logMessageReceived += OnUnityLogReceived;
			Application.wantsToQuit += OnUnityQuitting;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			CoreConfig.OnValidateCallback += OnCoreConfigChanged;

			_ = FetchOtelData();
			
			// On Windows we can try to get the crash logs
#if UNITY_EDITOR_WIN
			_ = GetUnparsedCrashLogs();
#endif
			
			// Make sure to Publish on Init so we make sure that all missing logs that weren't published on last session
			if (CoreConfig.EnableOtelAutoPublish)
			{
				_ = PublishLogs();
			}
		}

		private void OnCoreConfigChanged()
		{
			if (CoreConfig.TelemetryMaxSize.HasValue && CoreConfig.TelemetryMaxSize.Value == _telemetryMaxSize &&
			    CoreConfig.TelemetryMinLogLevel.HasValue && CoreConfig.TelemetryMinLogLevel.Value == _telemetryLogLevel)
			{
				return;
			}
			_telemetryMaxSize = CoreConfig.TelemetryMaxSize;
			_telemetryLogLevel = CoreConfig.TelemetryMinLogLevel;
			var commandWrapper = _cli.OtelSetConfig(new OtelSetConfigArgs() {logLevel = _telemetryLogLevel.ToString(), maxSize = _telemetryMaxSize.ToString()});
			commandWrapper.Run();

		}

		public async Promise OnDispose()
		{
			EditorApplication.update -= HandleUpdate;
			Application.logMessageReceived -= OnUnityLogReceived;
			Application.wantsToQuit -= OnUnityQuitting;
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
			await PublishLogs();
		}
		
		public void AddLog(string message, string stacktrace, string logLevel, Exception exception = null, Dictionary<string, string> extraAttributes = null)
		{
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

		public async Promise PublishLogs()
		{
			_lastTimePublished = EditorApplication.timeSinceStartup;
			if (_otelManagerStatus is not OtelManagerStatus.Normal)
			{
				return;
			}

			_otelManagerStatus = OtelManagerStatus.Publishing;
			try
			{
				await FlushUnityLogsAsync();
				if (!Directory.Exists(UnityOtelLogsFolder))
					return;
				var allFiles = Directory.GetFiles(UnityOtelLogsFolder);
				if (allFiles.Length > 0)
				{
					var commandWrapper = _cli.OtelReport(new OtelReportArgs() {paths = allFiles});
					// TODO: Change delete to use the OtelReport return status
					await commandWrapper.Run();
					await Task.Run(() =>
					{
						foreach (string filePath in allFiles)
						{
							File.Delete(filePath);
						}
					});
				}

				await _cli.OtelPush(new OtelPushArgs()).Run();

				await FetchOtelData();
			}
			catch (Exception e)
			{
				try { AddLog(e.Message, e.StackTrace, nameof(OtelLogLevel.Critical), e); }
				catch { Debug.LogError($"[UnityOtelManager] Failed to log exception: {e}"); }
			}
			finally
			{
				_otelManagerStatus = OtelManagerStatus.Normal;
			}
		}

		public async Promise PruneLogs()
		{
			_lastTimeFlush = _lastTimePublished = EditorApplication.timeSinceStartup;
			if (_otelManagerStatus is not OtelManagerStatus.Normal)
			{
				return;
			}

			_otelManagerStatus = OtelManagerStatus.Pruning;
			try
			{
				await _cli.OtelPrune(new OtelPruneArgs() {deleteAll = true}).Run();
				var allFiles = Directory.GetFiles(UnityOtelLogsFolder);
				// TODO: Move to Task.run
				foreach (string filePath in allFiles)
				{
					File.Delete(filePath);
				}

				await FetchOtelData();
			}
			catch (Exception e)
			{
				AddLog(e.Message, e.StackTrace, nameof(OtelLogLevel.Critical), e);
			}
			finally
			{
				_otelManagerStatus = OtelManagerStatus.Normal;
			}
		}
		
		public async Promise FetchOtelData()
		{
			await FetchOtelConfig();
			await FetchOtelStatus();
		}

		private async Promise FetchOtelStatus()
		{
			_lastOtelStatusRefresh = EditorApplication.timeSinceStartup;
			OtelStatusWrapper wrapper = _cli.OtelStatus();
			wrapper.OnStreamOtelStatusResult(rb => _otelStatus = rb.data);
			await wrapper.Run();
			if (_otelStatus.FolderSize > _telemetryMaxSize)
			{
				await PublishLogs();
			}
		}

		private async Promise FetchOtelConfig()
		{
			await Task.Delay(1);
		}
		
		private void HandleUpdate()
		{
			if(_otelManagerStatus is not OtelManagerStatus.Normal) 
			{
				return; 
			}
			
			TryToFlushUnityLogs();

			double now = EditorApplication.timeSinceStartup;
			
			double timeSinceLastRefresh = now - _lastOtelStatusRefresh;
			if (timeSinceLastRefresh > OTEL_CHECK_DELAY)
			{
				_ = FetchOtelData();
			}

			if(!CoreConfiguration.Instance.EnableOtelAutoPublish)
				return;

			double timeSinceLastPublish = now - _lastTimePublished;
			double timeIntervalToPublish = CoreConfig.TimeInterval.HasValue ? CoreConfig.TimeInterval.Value : DEFAULT_TIME_INTERVAL;
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
			string crashFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Temp", "Unity", "Editor", "Crashes");
			var lastParseFullPath = Path.Join(Application.temporaryCachePath, LAST_CRASH_PARSE_TIMESTAMP_FILE_NAME);
			if (!File.Exists(lastParseFullPath))
			{
				await File.WriteAllTextAsync(lastParseFullPath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
				return;
			}
			var fileContent = await File.ReadAllTextAsync(lastParseFullPath);
			if (!long.TryParse(fileContent, out long timestamp))
			{
				await File.WriteAllTextAsync(lastParseFullPath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
				return;
			}
			var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;

			var allCrashes = Directory.GetFiles(crashFolder, "*.log", SearchOption.AllDirectories);
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
						AddLog($"Unity Editor Crash Project: {Application.companyName}.{Application.productName}", allText, nameof(OtelLogLevel.Critical));
					}
				}
				catch (Exception e)
				{
					// Error while reading the crash log, let's log it and skip it
					AddLog(e.Message, e.StackTrace, nameof(OtelLogLevel.Critical), e);
				}
			}
		}

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

	public enum OtelManagerStatus
	{
		Normal,
		Publishing,
		Pruning
	}
}
