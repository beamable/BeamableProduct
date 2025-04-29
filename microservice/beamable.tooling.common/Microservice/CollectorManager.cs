using Beamable.Common;
using Beamable.Server.Common;
using microservice.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using ZLogger;

namespace cli.Services;

[Serializable]
public class CollectorDiscoveryEntry
{
	public int pid;
	public string status;
}

public class CollectorStatus
{
	public bool isRunning; // If this is true, it means the collector is running, but not necessarily ready to receive data
	public bool isReady;
	public int pid;

	public bool Equals(CollectorStatus otherStatus)
	{
		if (otherStatus.isReady != isReady)
		{
			return false;
		}

		if (otherStatus.isRunning != isRunning)
		{
			return false;
		}

		if (otherStatus.pid != pid)
		{
			return false;
		}

		return true;
	}
}

public class CollectorManager
{
	public const int ReceiveTimeout = 10;
	public const int ReceiveBufferSize = 4096;

	private const string configFileName = "clickhouse-config.yaml";
	private const int attemptsToConnect = 10;
	private const int attemptsBeforeFailing = 10;
	private const int delayBeforeNewAttempt = 500;

	public static string GetCollectorExecutablePath()
	{
		var osArchSuffix = "";
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			switch (RuntimeInformation.OSArchitecture)
			{
				case Architecture.X64:
					osArchSuffix = "windows-amd64.exe";
					break;
				case Architecture.Arm64:
					osArchSuffix = "windows-arm64.exe";
					break;
			}
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			switch (RuntimeInformation.OSArchitecture)
			{
				case Architecture.X64:
					osArchSuffix = "darwin-amd64";
					break;
				case Architecture.Arm64:
					osArchSuffix = "darwin-arm64";
					break;
			}
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			switch (RuntimeInformation.OSArchitecture)
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
		var collectorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, collectorFileName);

		if (File.Exists(collectorFilePath))
		{
			return collectorFilePath;
		}

		collectorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", collectorFileName);
		if (File.Exists(collectorFilePath))
		{
			return collectorFilePath;
		}

		throw new Exception("Collector executable was not found.");
	}

	public static Socket GetSocket(string host, int portNumber)
	{
		var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
		var ed = new IPEndPoint(IPAddress.Parse(host), portNumber);

		socket.ReceiveTimeout = CollectorManager.ReceiveTimeout;
		socket.ReceiveBufferSize = CollectorManager.ReceiveBufferSize;
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		socket.Bind(ed);

		return socket;
	}

	public static async Task<int> StartCollector(bool detach, CancellationTokenSource cts, ILogger logger)
	{
		//TODO Start a signed request to Beamo asking for credentials

		var connectedPromise = new Promise<int>();

		Task.Run(() => CollectorDiscovery(detach, cts, connectedPromise, logger));

		var collectorProcessId = await connectedPromise; // the first time we wait for the collector to be up before continuing

		return collectorProcessId;
	}

	private static async Task CollectorDiscovery(bool detach, CancellationTokenSource cts, Promise<int> connectedPromise, ILogger logger)
	{
		var port = Environment.GetEnvironmentVariable("BEAM_COLLECTOR_DISCOVERY_PORT");
		if(string.IsNullOrEmpty(port))
		{
			throw new Exception("There is no port configured for the collector discovery");
		}

		if (!Int32.TryParse(port, out int portNumber))
		{
			throw new Exception("Invalid value for port");
		}

		var host = Environment.GetEnvironmentVariable("BEAM_COLLECTOR_DISCOVERY_HOST");

		if(string.IsNullOrEmpty(host))
		{
			throw new Exception("There is no host configured for the collector discovery");
		}

		logger.ZLogInformation($"Starting listening to otel collector in port [{portNumber}]...");

		var socket = GetSocket(host, portNumber);

		int alarmCounter = 0;

		while (!cts.IsCancellationRequested) // Should we stop this after a number of attempts to find the collector?
		{
			var collectorStatus = await IsCollectorRunning(socket, cts.Token);

			if (!collectorStatus.isRunning)
			{
				if (alarmCounter > attemptsBeforeFailing)
				{
					throw new Exception("The collector couldn't start, terminating the microservice.");
				}
				logger.ZLogInformation($"Starting local process for collector, attempt number: {alarmCounter}");
				await StartCollectorProcess(detach, logger, cts);
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

	public static async Task StartCollectorProcess(bool detach, ILogger logger, CancellationTokenSource cts)
	{
		var collectorExecutablePath = GetCollectorExecutablePath();
		var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);

		if (!File.Exists(configPath))
		{
			configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources",configFileName);
			if (!File.Exists(configPath))
			{
				throw new Exception("Could not find the collector configuration file");
			}
		}

		using var process = new Process();

		var fileExe = collectorExecutablePath;
		var arguments = $"--config {configPath}";

		if (detach)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				arguments = "/C " + $"{fileExe.EnquotePath()} {arguments}".EnquotePath('(', ')');
				fileExe = "cmd.exe";
			}
			else
			{
				arguments = $"-a {fileExe} --args {arguments}";
				fileExe = "open";
			}
		}

		process.StartInfo.FileName = fileExe;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.UseShellExecute = false;
		process.EnableRaisingEvents = true;
		process.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");

		logger.ZLogInformation($"Executing: [{fileExe} {arguments}]");
		process.Start();

		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		await Task.Delay(100); // Not sure if this is necessary, is jut to take the time for the process to start before we listen to incoming data
	}
}
