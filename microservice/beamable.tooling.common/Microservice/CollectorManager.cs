using Beamable.Common;
using microservice.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using ZLogger;

namespace cli.Services;

public class CollectorManager
{
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

	public static async Task StartCollector(CancellationTokenSource cts, ILogger logger)
	{
		//TODO Start a signed request to Beamo asking for credentials
		//TODO Set credentials as environment variables

		Promise connectedPromise = new Promise();

		Task.Run(() => CollectorDiscovery(cts, connectedPromise, logger));

		await connectedPromise; // the first time we wait for the collector to be up before continuing
	}

	private static async Task CollectorDiscovery(CancellationTokenSource cts, Promise connectedPromise, ILogger logger)
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

		logger.ZLogInformation($"Starting listening to otel collector in port [{portNumber}]...");

		var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
		var ed = new IPEndPoint(IPAddress.Any, portNumber); //TODO change this to use the IP received through env var

		socket.ReceiveTimeout = 10;
		socket.ReceiveBufferSize = 4096;
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		socket.Bind(ed);

		int alarmCounter = 0;

		while (!cts.IsCancellationRequested)
		{
			var isRunning = await IsCollectorRunning(socket, cts.Token);

			if (!isRunning)
			{
				if (alarmCounter > attemptsBeforeFailing)
				{
					throw new Exception("The collector couldn't start, terminating the microservice.");
				}
				logger.ZLogInformation($"Starting local process for collector, attempt number: {alarmCounter}");
				await StartCollectorProcess(false, logger, cts);
				alarmCounter++;
			}
			else
			{
				if (!connectedPromise.IsCompleted)
				{
					logger.ZLogInformation($"Found collector! Events can now be sent to collector");
					connectedPromise.CompleteSuccess();
				}
				alarmCounter = 0;
			}

			await Task.Delay(delayBeforeNewAttempt);
		}
	}

	private static async Task<bool> IsCollectorRunning(Socket socket, CancellationToken token)
	{
		var buffer = new ArraySegment<byte>(new byte[socket.ReceiveBufferSize]);

		for (int i = 0; i < attemptsToConnect; i++)
		{
			if (token.IsCancellationRequested)
			{
				return false;
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

			return true;
		}

		return false;
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
			// it varies based on the os, but in general, when we are detaching, then
			//  when THIS process exits, we don't want the child-process to exit.
			//  The C# ProcessSDK makes that sort of difficult, but we can invoke programs
			//  that themselves create separate process trees. Or, at least I think we can.

			// in windows, this doesn't actually really _work_. It is put onto a background process,
			//  and the main window may close for the parent, but the process is kept open
			//  if you look in task-manager.
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				arguments = "/C " + $"{fileExe.EnquotePath()} {arguments}".EnquotePath('(', ')');
				fileExe = "cmd.exe";
			}
			else
			{
				arguments = $"--stdout --stderr -a {fileExe} --args {arguments}";
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


		var result = "";
		var sublogs = "";

		var exitSignal = new Promise();

		process.ErrorDataReceived += (sender, args) =>
		{
			if (!string.IsNullOrEmpty(args.Data))
			{
				//logger.ZLogInformation($"[Collector] {args.Data}");
				sublogs += args.Data;
				if (args.Data.Contains("Everything is ready. Begin running and processing data."))
				{
					exitSignal.CompleteSuccess();
				}
			}
		};

		process.OutputDataReceived += (sender, args) =>
		{
			if (!string.IsNullOrEmpty(args.Data))
			{
				//logger.ZLogInformation($"[Collector] {args.Data}");
				result += args.Data;
			}
		};

		logger.ZLogInformation($"Starting process...");
		process.Start();

		logger.ZLogInformation($"Collector PID: [{process.Id}]");
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		await exitSignal;
	}
}
