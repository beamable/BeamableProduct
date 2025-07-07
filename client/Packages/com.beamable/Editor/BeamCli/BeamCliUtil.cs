using Beamable.Common;
using Beamable.Editor.Dotnet;
using Beamable.Editor.Modules.EditorConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.BeamCli
{
	public static class BeamCliUtil
	{

		public static string CLI_VERSION
		{
			get
			{
				return BeamableEnvironment.NugetPackageVersion;
			}
		}

		public static string OWNER => Path.GetFullPath("Library/BeamableEditor/BeamCL").ToLowerInvariant();


		[System.Diagnostics.Conditional("SPEW_ALL")]
		static void VerboseLog(string log)
		{
			BeamableLogger.Log($"<b>[{nameof(BeamCliUtil)}]</b> {log}");
		}

		/// <summary>
		/// Installs the Beam CLI into the /Library folder of the current project.
		/// </summary>
		public static void InitializeBeamCli()
		{
			try
			{
				// we need dotnet before we can initialize the CLI
				DotnetUtil.InitializeDotnet();

				// need to install the CLI
				var installResult = InstallTool();
				if (!installResult)
				{
					throw new Exception("Beamable could not install the Beam CLI");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to init beam cli");
				Debug.LogError(ex);
				throw;
			}
		}

		/// <summary>
		/// this exists for CI jobs. This function should be run before hand. 
		/// </summary>
		public static void InstallToolFromLocalPackageSource()
		{
			var path = "BeamableNugetSource";
			if (!Directory.Exists(path))
			{
				Debug.Log("------ No package source exists...");
				return;
			}

			var files = Directory.GetFiles(path);
			Debug.Log($"------ Found {files?.Length} files at path package source=[{Path.GetFullPath(path)}]");
			foreach (var file in files)
			{
				Debug.Log($"----- file=[{file}]");
			}
			
			
			Debug.Log("------ INSTALL TOOL FROM LOCAL PACKAGE SOURCE");
			var process = new Process();
			process.StartInfo.FileName = "dotnet";
			process.StartInfo.Arguments = $"nuget add source {Path.GetFullPath(path)} --name BeamableNugetSource";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.Start();
			Debug.Log("------ INSTALL LOGS TOOL FROM LOCAL PACKAGE SOURCE");
			Debug.Log(process.StandardOutput.ReadToEnd());
			Debug.LogError(process.StandardError.ReadToEnd());
			if (!process.WaitForExit(10 * 1000))
			{
				Debug.Log("------ INSTALLED TOOL FROM LOCAL PACKAGE SOURCE EXPIRED");
			}
			Debug.Log("------ INSTALLED TOOL FROM LOCAL PACKAGE SOURCE");
			
			process = new Process();
			process.StartInfo.FileName = "dotnet";
			process.StartInfo.Arguments = "nuget list source";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.Start();
			Debug.Log("------ LIST LOCAL SOURCES LOGS");
			Debug.Log(process.StandardOutput.ReadToEnd());
			Debug.LogError(process.StandardError.ReadToEnd());
			if (!process.WaitForExit(10 * 1000))
			{
				Debug.Log("------  LIST LOCAL SOURCES EXPIRED");
			}
		}
		
		static bool InstallTool()
		{
			
			var proc = new Process();
			var installCommand = $"tool install Beamable.Tools --create-manifest-if-needed";

			if (Application.isBatchMode)
			{
				installCommand += " --add-source BeamableNugetSource ";
			}
			
			if (!BeamableEnvironment.NugetPackageVersion.ToString().Equals("0.0.123"))
			{
				installCommand += $" --version {BeamableEnvironment.NugetPackageVersion}";
			}
			else
			{
				installCommand += $" --version {BeamableEnvironment.NugetPackageVersion}.*";
			}
			proc.StartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				WorkingDirectory = Path.GetFullPath("."),
				Arguments = installCommand,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			proc.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");
			// proc.StartInfo.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
			proc.Start();
			if (!proc.WaitForExit(10 * 1000))
			{
				Debug.LogError("dotnet tool install command did not finish fast enough; timed out.");
				return false;
			}
			var output = proc.StandardOutput.ReadToEnd();
			var error = proc.StandardError.ReadToEnd();
			if (!string.IsNullOrWhiteSpace(error))
			{
				Debug.LogError("Unable to install BeamCLI: " + error + " / " + output);
			}
			return proc.ExitCode == 0;
		}
	}
}
