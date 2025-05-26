using System.Collections;
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
using ZLogger;
using Otel = Beamable.Common.Constants.Features.Otel;

namespace cli.Services;

[Serializable]
public class CollectorZapcoreEntry
{
	public string Level { get; set; }
	public DateTime Time { get; set; }
	public string LoggerName { get; set; }
	public string Message { get; set; }
	public CollectorCaller Caller { get; set; }
	public string Stack { get; set; }
}

[Serializable]
public class CollectorCaller
{
	public bool Defined { get; set; }
	public ulong PC { get; set; }
	public string File { get; set; }
	public int Line { get; set; }
}

[Serializable]
public class CollectorDiscoveryEntry
{
	public int pid;
	public string status;
	public CollectorZapcoreEntry[] logs;
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
	
	public static string CollectorDownloadUrl =
		"https://collectors.beamable.com/version/BEAM_VERSION/BEAM_FILE_NAME";

	
	
	
	public static string configFileName = "clickhouse-config.yaml";
	private const int attemptsToConnect = 3;
	private const int attemptsBeforeFailing = 3;
	private const int delayBeforeNewAttempt = 500;

	public static string GetCollectorExecutablePath()
	{
		var collectorFileName = GetCollectorName();
		var collectorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, collectorFileName);

		return collectorFilePath;
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

	public static string GetCollectorName()
	{
		var platform = GetCurrentPlatform();
		var arch = RuntimeInformation.OSArchitecture;
		return GetCollectorName(platform, arch);
	}

	public static string GetCollectorBasePathForCli(string version=null)
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

		if (string.IsNullOrEmpty(version))
		{
			version = BeamAssemblyVersionUtil.GetVersion<CollectorManager>();
		}
		if (version.StartsWith("0.0.123") || version == "1.0.0")
		{
			// if the version looks like a local dev version, then 
			//  force the version to be THE local dev version
			version = "0.0.123";
		}
		
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var root = Path.Combine(localAppData, "beam", "collectors", version);
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
		Architecture arch, 
		string version=null)
	{
		if (string.IsNullOrEmpty(version))
		{
			version = BeamAssemblyVersionUtil.GetVersion<CollectorManager>();
		}
		
		var collectorName = GetCollectorName(platform, arch);
		var collectorPath = Path.Combine(absBasePath, collectorName);
		var configPath = Path.Combine(absBasePath, configFileName);

		var itemsToDownload = new List<(string url, string filePath, bool makeExecutable)>();
		
		if (!File.Exists(collectorPath) && allowDownload)
		{
			var collectorUrl = COLLECTOR_DOWNLOAD_URL_TEMPLATE
				.Replace(KEY_VERSION, version)
				.Replace(KEY_FILE, collectorName + ".gz");
			
			itemsToDownload.Add(new (collectorUrl, collectorPath, true));
		}

		if (!File.Exists(configPath) && allowDownload)
		{
			var configUrl = COLLECTOR_DOWNLOAD_URL_TEMPLATE
				.Replace(KEY_VERSION, version)
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
	
	public static Socket GetSocket(string host, int portNumber)
	{
		var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
		var ed = new IPEndPoint(IPAddress.Parse(host), portNumber);

		Log.Verbose($"collector discovery acquiring socket address=[{ed}]");
		socket.ReceiveTimeout = CollectorManager.ReceiveTimeout;
		socket.ReceiveBufferSize = CollectorManager.ReceiveBufferSize;
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		socket.Bind(ed);

		return socket;
	}

	public static async Task<int> StartCollector(
		string absBasePath, 
		bool allowDownload,
		bool detach, 
		CancellationTokenSource cts, 
		ILogger logger)
	{
		//TODO Start a signed request to Beamo asking for credentials

		var collectorInfo = await ResolveCollector(absBasePath, allowDownload);
		
		
		var connectedPromise = new Promise<int>();

		_ = Task.Run(async () =>
		{
			try
			{
				await CollectorDiscovery(collectorInfo.filePath, detach, cts, connectedPromise, logger);
			}
			catch (Exception ex)
			{
				logger.LogError($"Collector discovery failed! ({ex.GetType().Name}) {ex.Message}\n{ex.StackTrace}");
				throw;
			}
		});

		var collectorProcessId = await connectedPromise; // the first time we wait for the collector to be up before continuing

		return collectorProcessId;
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

	private static async Task CollectorDiscovery(string collectorExecutablePath, bool detach, CancellationTokenSource cts, Promise<int> connectedPromise, ILogger logger)
	{
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

		var socket = GetSocket(host, portNumber);

		int alarmCounter = 0;

		var collectorStatus = await IsCollectorRunning(socket, cts.Token);
		if (!collectorStatus.isRunning)
		{
			logger.ZLogInformation($"Starting local process for collector");
			await StartCollectorProcess(collectorExecutablePath, detach, logger, cts);
		}
		
		while (!cts.IsCancellationRequested) // Should we stop this after a number of attempts to find the collector?
		{
			collectorStatus = await IsCollectorRunning(socket, cts.Token);

			if (!collectorStatus.isRunning)
			{
				if (alarmCounter > attemptsBeforeFailing)
				{
					throw new Exception("The collector couldn't start, terminating the microservice.");
				}
				alarmCounter++;
			}
			else if (collectorStatus.isReady)
			{
				if (!connectedPromise.IsCompleted)
				{
					logger.ZLogInformation($"Found collector! Events can now be sent to collector");
					connectedPromise.CompleteSuccess(collectorStatus.pid);
				}
				alarmCounter = 0;
			}

			await Task.Delay(delayBeforeNewAttempt);
		}
	}

	public static async Task<CollectorStatus> IsCollectorRunning(Socket socket, CancellationToken token)
	{
		// var socket = GetSocket("127.0.0.1", 8688);
		var buffer = new ArraySegment<byte>(new byte[socket.ReceiveBufferSize]);

		for (int i = 0; i < attemptsToConnect; i++)
		{
			if (token.IsCancellationRequested)
			{
				return new CollectorStatus()
				{
					isRunning = false,
					isReady = false
				};
			}

			if (socket.Available == 0)
			{
				await Task.Delay(delayBeforeNewAttempt);
				continue;
			}

			var byteCount = await socket.ReceiveAsync(buffer, SocketFlags.None);
			Log.Information($"Socket has bytes=[{byteCount}]");

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
					continue;

			}
			return new CollectorStatus()
			{
				isRunning = true,
				isReady = collector.status == "READY",
				pid = collector.pid
			};
		}

		return new CollectorStatus()
		{
			isRunning = false,
			isReady = false
		};
	}

	public static async Task StartCollectorProcess(string collectorExecutablePath, bool detach, ILogger logger, CancellationTokenSource cts)
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

		var workingDir = Path.GetDirectoryName(fileExe);
		process.StartInfo.FileName = fileExe;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.WorkingDirectory = Path.GetDirectoryName(fileExe);
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.UseShellExecute = false;
		process.EnableRaisingEvents = true;
		process.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");

		logger.ZLogInformation($"Executing: [{fileExe} {arguments}]");

		process.OutputDataReceived += (_, args) =>
		{
			Log.Verbose($"(collector) {args.Data}");
		};
		process.ErrorDataReceived += (_, args) =>
		{
			Log.Verbose($"(collector err) {args.Data}");
		};
		var started = process.Start();
		if (!started)
		{
			throw new Exception("Failed to start collector");
		}

		Log.Information($"Started collector with process-id=[{process.Id}]");

		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		await Task.Delay(100); // Not sure if this is necessary, is jut to take the time for the process to start before we listen to incoming data
		

	}
}
