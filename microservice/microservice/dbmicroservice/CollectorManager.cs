using Beamable.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
		await Task.Delay(100);
	}
}
