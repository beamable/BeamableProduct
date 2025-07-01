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
			// we need dotnet before we can initialize the CLI
			DotnetUtil.InitializeDotnet();
			
			// need to install the CLI
			var installResult = InstallTool();

			if (!installResult)
			{
				throw new Exception("Beamable could not install the Beam CLI");
			}
		}

		static bool InstallTool()
		{
			if (!DotnetUtil.InstallLocalManifest(out var manifestPath))
			{
				BeamableLogger.LogError("Unable to install BeamCLI from package: couldn't create a local manifest for the project.");
				return false;
			}
			
			var proc = new Process();
			var installCommand = $"tool install beamable.tools --tool-manifest \"{manifestPath}\"";
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
			proc.Start();
			proc.WaitForExit();
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
