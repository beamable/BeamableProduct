﻿using Beamable.Server;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Beamable.standalone_microservice
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="StandaloneMicroservice"/> service.
		/// </summary>
		public static async Task Main()
		{
			Environment.SetEnvironmentVariable("DOTNET_DiagnosticPorts", null);
			await Prepare<StandaloneMicroservice>();
			
			// load environment variables from local file
			LoadEnvironmentVariables();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<StandaloneMicroservice>();
		}
		
		static void LoadEnvironmentVariables(string filePath=".env")
		{
			if (!File.Exists(filePath))
				throw new Exception($"No environment file found at path=[{filePath}]");

			foreach (var line in File.ReadAllLines(filePath))
			{
				var parts = line.Split(
					'=',
					StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length != 2)
					continue;

				Environment.SetEnvironmentVariable(parts[0], parts[1]);
			}
		}
		
		static async Task Prepare<TMicroservice>() where TMicroservice : Microservice
		{
			
			var inDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
			if (inDocker) return;
			
			MicroserviceAttribute attribute = typeof(TMicroservice).GetCustomAttribute<MicroserviceAttribute>();
			var serviceName = attribute.MicroserviceName.ToLower();
			
			using var process = new Process();

			process.StartInfo.FileName = "beam";
			process.StartInfo.Arguments = $"--log fatal project generate-env {serviceName} . --auto-deploy --include-prefix=false --instance-count=10";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;

			process.Start();
			await process.WaitForExitAsync();
			
			var result = await process.StandardOutput.ReadToEndAsync();
			// Console.WriteLine(result);
			if (process.ExitCode != 0)
			{
				throw new Exception($"Failed to generate-env message=[{result}]");
			}
		}
	}
}
