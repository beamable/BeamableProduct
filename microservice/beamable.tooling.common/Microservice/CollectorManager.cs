using Beamable.Common;
using Beamable.Server.Common;
using microservice.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Util;
using Beamable.Server;
using System.Reflection;
using ZLogger;
using Otel = Beamable.Common.Constants.Features.Otel;

[Serializable]
public class CollectorVersion
{
	public string collectorVersion;
}

[Serializable]
public class CollectorDiscoveryEntry
{
	public string version;
	public string status;
	public int pid;
	public string otlpEndpoint;
}

public class CollectorInfo
{
	public string filePath;
	public string configFilePath;
}

public class CollectorManager
{
	public const int ReceiveTimeout = 10;
	public const int ReceiveBufferSize = 4096;

	private const string KEY_VERSION = "BEAM_VERSION";
	private const string KEY_FILE = "BEAM_FILE_NAME";

	private const string COLLECTOR_DOWNLOAD_URL_TEMPLATE =
		"https://collectors.beamable.com/version/" + KEY_VERSION + "/" + KEY_FILE;

	
	
	
	public static string configFileName = "clickhouse-config.yaml";
	private const int attemptsToConnect = 50;
	private const int attemptsBeforeFailing = 10;
	private const int delayBeforeNewAttempt = 500;
	private const int delayBeforeNewMessage = 100;

	public static CollectorStatus CollectorStatus;

	private static string _cachedVersion;

	public static string Version
	{
		get
		{
			if (_cachedVersion == null)
			{
				_cachedVersion = GetCollectorVersion();
			}
			return _cachedVersion;
		}
	}

	public static OSPlatform GetCurrentPlatform()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return OSPlatform.Windows;
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return OSPlatform.OSX;
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return OSPlatform.Linux;
		}
		else
		{
			throw new NotImplementedException("Unknown OS Platform");
		}
	}
	
	public static string GetCollectorName(OSPlatform platform, Architecture arch)
	{
		var osArchSuffix = "";
		if (platform == OSPlatform.Windows)
		{
			switch (arch)
			{
				case Architecture.X64:
					osArchSuffix = "windows-amd64.exe";
					break;
				case Architecture.Arm64:
					osArchSuffix = "windows-arm64.exe";
					break;
			}
		}else if (platform == OSPlatform.OSX)
		{
			switch (arch)
			{
				case Architecture.X64:
					osArchSuffix = "darwin-amd64";
					break;
				case Architecture.Arm64:
					osArchSuffix = "darwin-arm64";
					break;
			}
		} else if (platform == OSPlatform.Linux)
		{
			switch (arch)
			{
				case Architecture.X64:
					osArchSuffix = "linux-amd64";
					break;
				case Architecture.Arm64:
					osArchSuffix = "linux-arm64";
					break;
			}
		}

		if (string.IsNullOrEmpty(osArchSuffix))
		{
			throw new NotImplementedException("unsupported os");
		}

		var collectorFileName = $"collector-{osArchSuffix}";
		return collectorFileName;
	}

	public static string GetCollectorVersion()
	{
		var assembly = Assembly.GetExecutingAssembly();
		var resourceName = "beamable.tooling.common.Microservice.VersionManagement.collector-version.json";

		var version = BeamAssemblyVersionUtil.GetVersion<CollectorManager>();

		if (version.StartsWith("0.0.123") || version == "1.0.0")
		{
			// if the version looks like a local dev version, then
			//  force the version to be THE local dev version
			return "0.0.123";
		}

		Stream stream = assembly.GetManifestResourceStream(resourceName);

		if (stream == null)
		{
			throw new FileNotFoundException($"Collector version file not found: '{resourceName}'.\nAvailable resources:\n" +
			                                string.Join("\n", assembly.GetManifestResourceNames()));
		}

		string versionJson;

		using(stream)
		using (var reader = new StreamReader(stream))
		{
			versionJson = reader.ReadToEnd();
		}

		return JsonConvert.DeserializeObject<CollectorVersion>(versionJson).collectorVersion;
	}

	public static string GetCollectorBasePathForCli()
	{
		/*
		 * there are several common cases,
		 * 1. this is the production CLI, like 5.0.0,
		 * 2. this is the local CLI, like 0.0.123.x
		 * 3. this is a test CLI, like 0.0.0 (when running from Rider, or Unit Test)
		 * 4. this is a local dev Microservice , like 0.0.123.x
		 * 5. this is a local prod Microservice, like 5.0.0
		 * 6. this is a deployed dev Microservice, like 0.0.123.x
		 * 7. this is a deployed prod Microservice, like 5.0.0
		 *
		 * ( the cases where the C#MS is local, but running in Docker, are treated as though they are deployed )
		 * 
		 * the dimensions are,
		 * - CLI / Microservice
		 * - VERSION (0.0.0, 0.0.123.x, 1+.0.0)
		 * - DOCKER (yes, no)
		 *
		 * Here are some true facts, 
		 * - When we are in the CLI, we can freely download a collector if it does not exist.
		 * - When we are 0.0.0 or 0.0.123, we can expect that the collectors have been built as part of the dev cycle.
		 * - When we are in 1+.0.0, we must use the collectors from the CDN
		 * - We should always use a local file first instead of downloading it.
		 * 
		 * With that said, I think the guiding light is that,
		 * - 
		 * 
		 */
		
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var root = Path.Combine(localAppData, "beam", "collectors", Version);
		return root;
	}

	public static async Task<CollectorInfo> ResolveCollector(
		string absBasePath,
		bool allowDownload)
	{
		return await ResolveCollector(absBasePath, allowDownload, GetCurrentPlatform(), RuntimeInformation.OSArchitecture);
	}
	
	public static async Task<CollectorInfo> ResolveCollector(
		string absBasePath, 
		bool allowDownload,
		OSPlatform platform, 
		Architecture arch)
	{
		
		var collectorName = GetCollectorName(platform, arch);
		var collectorPath = Path.Combine(absBasePath, collectorName);
		var configPath = Path.Combine(absBasePath, configFileName);

		var itemsToDownload = new List<(string url, string filePath, bool makeExecutable)>();
		
		if (!File.Exists(collectorPath) && allowDownload)
		{
			var collectorUrl = COLLECTOR_DOWNLOAD_URL_TEMPLATE
				.Replace(KEY_VERSION, Version)
				.Replace(KEY_FILE, collectorName + ".gz");
			
			itemsToDownload.Add(new (collectorUrl, collectorPath, true));
		}

		if (!File.Exists(configPath) && allowDownload)
		{
			var configUrl = COLLECTOR_DOWNLOAD_URL_TEMPLATE
				.Replace(KEY_VERSION, Version)
				.Replace(KEY_FILE, configFileName + ".gz");
			itemsToDownload.Add(new (configUrl, configPath, false));
		}

		if (itemsToDownload.Count > 0)
		{
			var httpClient = new HttpClient();
			var tasks = itemsToDownload
				.Select(d => DownloadAndDecompressGzip(httpClient, d.url, d.filePath, d.makeExecutable))
				.ToList();
			foreach (var t in tasks)
			{
				await t;
			}
		}

		return new CollectorInfo
		{
			configFilePath = File.Exists(configPath) ? configPath : null,
			filePath = File.Exists(collectorPath) ? collectorPath : null
		};
	}
	
	private static async Task DownloadAndDecompressGzip(HttpClient httpClient, string url, string outputPath, bool makeExecutable)
	{
		Log.Information($"Downloading {url} to {outputPath}");
		var folder = Path.GetDirectoryName(outputPath);
		Directory.CreateDirectory(folder);
		using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		using var responseStream = await response.Content.ReadAsStreamAsync();
		using var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress);
		using var outputFileStream = File.Create(outputPath);

		await gzipStream.CopyToAsync(outputFileStream);

		if (makeExecutable)
		{
			if (!TryMakeExecutable(outputPath, out var error))
			{
				Log.Error($"Unable to mark {outputPath} as executable. Error=[{error}]");
			}
		}
	}
	
	private static bool TryMakeExecutable(string filePath, out string error)
    {
        error = null;

        if (!File.Exists(filePath))
        {
            error = $"File does not exist: {filePath}";
            return false;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, executability is based on file extension (.exe, .bat, etc.).
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".exe" || ext == ".bat" || ext == ".cmd" || ext == ".com")
                {
                    return true;
                }
                else
                {
                    error = $"Windows does not consider '{ext}' files executable. Use a proper executable extension.";
                    return false;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{filePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    error = $"chmod failed with exit code {process.ExitCode}: {stderr.Trim()}";
                    return false;
                }

                return true;
            }
            else
            {
                error = "Unsupported platform.";
                return false;
            }
        }
        catch (Exception ex)
        {
            error = $"Unexpected error: {ex.Message}";
            return false;
        }
    }
	
	public static Socket GetSocket(int portNumber, ILogger logger)
	{
		var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		var ed = new IPEndPoint(IPAddress.Any, portNumber);

		logger.LogInformation($"collector discovery acquiring socket address=[{ed}]");
		socket.ReceiveTimeout = CollectorManager.ReceiveTimeout;
		socket.ReceiveBufferSize = CollectorManager.ReceiveBufferSize;
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
		socket.Bind(ed);

		return socket;
	}

	public static async Task<CollectorStatus> StartCollector(
		string absBasePath, 
		bool allowDownload,
		bool detach, 
		CancellationTokenSource cts, 
		ILogger logger)
	{
		//TODO Start a signed request to Beamo asking for credentials

		AddDefaultCollectorHostAndPortFallback();

		var port = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_PORT);
		if(string.IsNullOrEmpty(port))
		{
			throw new Exception("There is no port configured for the collector discovery");
		}

		if (!Int32.TryParse(port, out int portNumber))
		{
			throw new Exception("Invalid value for port");
		}

		var host = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_HOST);

		if(string.IsNullOrEmpty(host))
		{
			throw new Exception("There is no host configured for the collector discovery");
		}

		logger.ZLogInformation($"Starting listening to otel collector in port [{portNumber}]...");

		var collectorInfo = await ResolveCollector(absBasePath, allowDownload);
		
		var connectedPromise = new Promise<CollectorStatus>();

		Socket socket = GetSocket(portNumber, logger);

		var backgroundTask = Task.Run(async () =>
		{
			try
			{
				await CollectorDiscovery(collectorInfo.filePath, socket, detach, cts, connectedPromise, logger);
			}
			catch (Exception ex)
			{
				logger.LogError($"Collector discovery failed! ({ex.GetType().Name}) {ex.Message}\n{ex.StackTrace}");

				Environment.Exit(1);
				throw;
			}
		});

		var collectorStatus = await connectedPromise; // the first time we wait for the collector to be up before continuing

		return collectorStatus;
	}

	public static void AddDefaultCollectorHostAndPortFallback()
	{
		var port = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_PORT);
		if(string.IsNullOrEmpty(port))
		{
			Environment.SetEnvironmentVariable(Otel.ENV_COLLECTOR_PORT, Otel.ENV_COLLECTOR_PORT_DEFAULT_VALUE);
		}

		var host = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_HOST);
		if(string.IsNullOrEmpty(host))
		{
			Environment.SetEnvironmentVariable(Otel.ENV_COLLECTOR_HOST, Otel.ENV_COLLECTOR_HOST_DEFAULT_VALUE);
		}
	}

	private static async Task CollectorDiscovery(string collectorExecutablePath, Socket socket, bool detach, CancellationTokenSource cts, Promise<CollectorStatus> connectedPromise, ILogger logger)
	{
		int alarmCounter = 0;

		CollectorStatus = await IsCollectorRunning(socket, cts.Token, logger);
		if (!CollectorStatus.isRunning)
		{
			logger.ZLogInformation($"Starting local process for collector");
			StartCollectorProcess(collectorExecutablePath, detach, logger, cts);
		}
		
		while (!cts.IsCancellationRequested) // Should we stop this after a number of attempts to find the collector?
		{
			CollectorStatus = await IsCollectorRunning(socket, cts.Token, logger);

			if (!CollectorStatus.isRunning)
			{
				if (alarmCounter > attemptsBeforeFailing)
				{
					throw new Exception("The collector couldn't start, terminating the microservice.");
				}
				alarmCounter++;
			}
			else if (CollectorStatus.isReady)
			{
				if (!connectedPromise.IsCompleted)
				{
					logger.ZLogInformation($"Found collector! Events can now be sent to collector");
					connectedPromise.CompleteSuccess(CollectorStatus);
				}
				alarmCounter = 0;
			}

			await Task.Delay(delayBeforeNewAttempt);
		}
	}

	public static async Task<List<CollectorStatus>> CheckAllRunningCollectors(Socket socket, CancellationToken token, ILogger logger)
	{
		var results = new List<CollectorStatus>();

		for (int i = 0; i < attemptsToConnect; i++)
		{
			var runningResult = await GetRunningCollectorMessage(socket, token);

			if (!runningResult.foundCollector)
			{
				await Task.Delay(delayBeforeNewMessage);
				continue;
			}

			var collector = runningResult.message;

			var alreadyAdded = results.FirstOrDefault(r => r.version == collector.version);

			if (alreadyAdded == null)
			{
				results.Add(new CollectorStatus()
				{
					isRunning = true,
					isReady = collector.status == "READY",
					pid = collector.pid,
					otlpEndpoint = collector.otlpEndpoint,
					version = collector.version
				});
			}
			await Task.Delay(delayBeforeNewMessage);
		}

		//TODO: This does not guarantee that will have all collectors running in it, since they are fighting
		// for the broadcasting channel. Is there a better way to handle this?
		return results;
	}

	public static async Task<(bool foundCollector, CollectorDiscoveryEntry message)> GetRunningCollectorMessage(Socket socket, CancellationToken token)
	{
		var buffer = new ArraySegment<byte>(new byte[socket.ReceiveBufferSize]);

		if (token.IsCancellationRequested)
		{
			return (false, null);
		}

		if (socket.Available == 0)
		{
			return (false, null);
		}

		var byteCount = await socket.ReceiveAsync(buffer, SocketFlags.None);

		if (byteCount == 0)
		{
			// This means we didn't get anything from the collector, so something wrong happened
			throw new Exception("Didn't get any message from the collector after the timeout");
		}

		var collectorMessage = Encoding.UTF8.GetString(buffer.Array!, 0, byteCount);

		var collector = JsonConvert.DeserializeObject<CollectorDiscoveryEntry>(collectorMessage, UnitySerializationSettings.Instance);

		{
			// it is POSSIBLE that a dead collector's message is in the socket receive queue,
			//  so before promising that this service exists, do a quick process-check to
			//  make sure it is actually alive.
			var isProcessNotWorking = false;
			try
			{
				Process.GetProcessById(collector.pid);
			}
			catch
			{
				isProcessNotWorking = true;
			}

			if (isProcessNotWorking)
			{
				return (false, null);
			}
		}

		return (true, collector);
	}

	public static async Task<CollectorStatus> IsCollectorRunning(Socket socket, CancellationToken token, ILogger logger)
	{
		for (int i = 0; i < attemptsToConnect; i++)
		{
			var runningResult = await GetRunningCollectorMessage(socket, token);

			if (!runningResult.foundCollector)
			{
				await Task.Delay(delayBeforeNewMessage);
				continue;
			}

			var collector = runningResult.message;

			// We check if the version of the found collector matches the version supported
			if (collector.version != Version)
			{
				await Task.Delay(delayBeforeNewMessage);
				continue;
			}

			return new CollectorStatus()
			{
				isRunning = true,
				isReady = collector.status == "READY",
				pid = collector.pid,
				otlpEndpoint = collector.otlpEndpoint
			};
		}

		return new CollectorStatus()
		{
			isRunning = false,
			isReady = false,
			pid = 0,
		};
	}

	public static void StartCollectorProcess(string collectorExecutablePath, bool detach, ILogger logger, CancellationTokenSource cts)
	{
		logger.ZLogInformation($"Using Collector Executable Path: [{collectorExecutablePath}]");

		if (!File.Exists(collectorExecutablePath))
		{
			throw new Exception($"Collector binary not found at: {collectorExecutablePath}");
		}

		var configPath = Path.Combine(Path.GetDirectoryName(collectorExecutablePath), configFileName);
		if (!File.Exists(configPath))
		{
			throw new Exception("Could not find the collector configuration file");
		}

		var process = new Process(); // DO NOT use using- because we don't want it to go away!

		var fileExe = collectorExecutablePath;
		var arguments = $"--config {configPath.EnquotePath()}";

		if (detach)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				arguments = "/C " + $"{fileExe} {arguments}".EnquotePath('(', ')');
				fileExe = "cmd.exe";
			}
			else
			{
				arguments = $"-a {fileExe.EnquotePath()} --args {arguments}";
				fileExe = "open";
			}
		}

		//TODO: We should make it possible to pass a OTLP PORT that is free for the collector, it being the same one we are using here in the first place

		process.StartInfo.FileName = fileExe;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.WorkingDirectory = Path.GetDirectoryName(fileExe);
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.UseShellExecute = false;
		process.EnableRaisingEvents = true;
		process.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");

		//TODO: Should we check if that env var was already set and use it?
		process.StartInfo.Environment.Add("BEAM_COLLECTOR_PROMETHEUS_PORT", PortUtil.FreeTcpPort().ToString());

		var localEndpoint = PortUtil.FreeEndpoint().Replace("http://", "");
		process.StartInfo.Environment.Add("BEAM_OTLP_HTTP_ENDPOINT", localEndpoint);

		logger.ZLogInformation($"Executing: [{fileExe} {arguments}]");

		process.OutputDataReceived += (_, args) =>
		{
			logger.ZLogInformation($"(collector) {args.Data}");
		};
		process.ErrorDataReceived += (_, args) =>
		{
			logger.ZLogInformation($"(collector err) {args.Data}");
		};
		var started = process.Start();
		if (!started)
		{
			throw new Exception("Failed to start collector");
		}

		logger.ZLogInformation($"Started collector with process-id=[{process.Id}]");

		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
	}
}
