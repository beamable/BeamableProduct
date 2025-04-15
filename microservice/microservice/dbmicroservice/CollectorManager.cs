using Beamable.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace microservice.dbmicroservice;

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

		throw new Exception("Collector executable was not found.");
	}

	public static async Task StartCollector(CancellationToken token)
	{
		//TODO Start a signed request to Beamo asking for credentials
		//TODO Set credentials as environment variables

		Promise connectedPromise = new Promise();

		Task.Run(() => CollectorDiscovery(token, connectedPromise));

		await connectedPromise; // the first time we wait for the collector to be up before continuing
	}

	private static async Task CollectorDiscovery(CancellationToken token, Promise connectedPromise)
	{
		// do some socket initialization
		var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
		var ed = new IPEndPoint(IPAddress.Any, 8686); //TODO figure a way to have this as config for both collector and microservice, environment variable / csproj property

		socket.ReceiveTimeout = 10;
		socket.ReceiveBufferSize = 4096;
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		socket.Bind(ed);

		int alarmCounter = 0;

		while (!token.IsCancellationRequested)
		{
			var isRunning = await IsCollectorRunning(socket, token);

			if (!isRunning)
			{
				if (alarmCounter > attemptsBeforeFailing)
				{
					throw new Exception("The collector couldn't start, terminating the microservice.");
				}

				await StartCollectorProcess(token);
				alarmCounter++;
			}
			else
			{
				if (!connectedPromise.IsCompleted)
				{
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
			if (socket.Available == 0)
			{
				await Task.Delay(delayBeforeNewAttempt);
				continue;
			}

			var byteCount = await socket.ReceiveAsync(buffer, token);

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

	public static async Task StartCollectorProcess(CancellationToken cts)
	{
		var collectorExecutablePath = GetCollectorExecutablePath();
		var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);

		using var process = new Process();

		process.StartInfo.FileName = collectorExecutablePath;
		process.StartInfo.Arguments = $"--config {configPath}";
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.UseShellExecute = false;
		process.EnableRaisingEvents = true;

		var result = "";
		var sublogs = "";

		var exitSignal = new Promise();

		process.ErrorDataReceived += (sender, args) =>
		{
			if (!string.IsNullOrEmpty(args.Data))
			{
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
				result += args.Data;
			}
		};

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		await exitSignal;
		await Task.Delay(1000);
	}
}
