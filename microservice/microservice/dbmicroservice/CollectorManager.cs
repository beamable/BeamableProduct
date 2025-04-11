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

	public static async Task StartCollector()
	{
		//TODO Start a signed request to Beamo asking for credentials
		//TODO Set credentials as environment variables

		//First lets check if the collector is not already running
		var cts = new CancellationTokenSource();
		await StartCollectorDiscoveryTask(cts.Token, async (e) =>
		{
			//Collector is not running yet, so we will start it
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
				//_logger.ZLogTrace($"Generate env process (error): [{args.Data}]");
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
				//_logger.ZLogTrace($"Generate env process (log): [{args.Data}]");
				if (!string.IsNullOrEmpty(args.Data))
				{
					result += args.Data;
				}
			};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await exitSignal;
			await Task.Delay(3000); //Give it a few seconds to be up and running

			//This time if it can't connect to the collector, then we throw an exception and exit the C#MS
			await StartCollectorDiscoveryTask(cts.Token, (e) => throw e);
		});

	}

	public static Task StartCollectorDiscoveryTask(CancellationToken token, Action<Exception> errorCaseHandler)
	{
		return Task.Run(async () =>
		{
			try
			{
				var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
				var ed = new IPEndPoint(IPAddress.Any, 8686); //TODO figure a way to have this as config for both collector and microservice

				socket.ReceiveTimeout = 10;
				socket.ReceiveBufferSize = 4096;
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				socket.Bind(ed);

				var buffer = new ArraySegment<byte>(new byte[socket.ReceiveBufferSize]);
				int attemptsCounter = 0;

				while (!token.IsCancellationRequested)
				{
					{
						// the only reason to delay at all is to avoid task exhaustion on lower end systems.
						//  this should happen at the start of the loop so that it cannot be accidentally skipped
						//  by `continue` statements
						await Task.Delay(Beamable.Common.Constants.Features.Services.DISCOVERY_BROADCAST_PERIOD_MS, token);
					}

					if (socket.Available == 0)
					{
						attemptsCounter += 1;
						if (attemptsCounter >= 10) //TODO for now this is arbitrary
						{
							throw new Exception("Couldn't connect to collector, shutting down.");
						}
						continue;
					}

					// Block and wait for a message from the collector.
					var byteCount = await socket.ReceiveAsync(buffer, token);

					if (byteCount == 0)
					{
						// This means we didn't get anything from the collector, so it's probably turned off
						throw new Exception("Didn't get any message from the collector after the timeout");
					}

					var collectorMessage = Encoding.UTF8.GetString(buffer.Array!, 0, byteCount);
				}
			}
			catch (TaskCanceledException)
			{
				// this exception is "fine"
				//  we can exit the task as though everything is "fine"
			}
			catch (OperationCanceledException)
			{
				// this comes from the ReceiveAsync being cancelled mid
			}
			catch (Exception e)
			{
				errorCaseHandler?.Invoke(e);
			}
		}, token);
	}
}
