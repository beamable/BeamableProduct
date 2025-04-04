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
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.BeamCli
{
	public static class BeamCliUtil
	{
		static bool USE_SRC
		{
			get
			{
				var config = EditorConfiguration.Instance;
				if (config == null || !config.AdvancedCli.HasValue) return false;

				return config.AdvancedCli.Value.UseFromSource.HasValue;
			}
		}

		public static string CLI_VERSION
		{
			get
			{
				if (USE_SRC) return "0.0.123"; 
				return BeamableEnvironment.NugetPackageVersion;
			}
		}

		private static string CLI_VERSIONED_HOME
		{
			get
			{
				if (USE_SRC)
				{
					return SessionState.GetString(SRC_BEAM, string.Empty);
				}

				const string CLI_LIBRARY = "Library/BeamableEditor/BeamCLI";
				return Path.Combine(CLI_LIBRARY, CLI_VERSION);
			}
		}

		public static string CLI => USE_SRC ? Path.GetFullPath(CLI_VERSIONED_HOME) : EXEC;

		public static string CLI_PATH
		{
			get
			{
				if (USE_SRC)
				{
					return CLI_VERSIONED_HOME;
				}

				return Path.Combine(CLI_VERSIONED_HOME, EXEC);
			}
		}
		public static string OWNER => Path.GetFullPath(CLI_PATH).ToLowerInvariant();

		const string SRC_BEAM = "BUILDED_BEAM";

		private const string EXEC = "beam";


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

			if (USE_SRC)
			{
				if (CheckForBuildedSource())
				{
					return;
				}

				BuildTool();
				if (CheckForBuildedSource(outputNotFoundError: true))
				{
					return;
				}
			}

			// need to install the CLI
			var installResult = InstallTool();

			if (!installResult)
			{
				throw new Exception("Beamable could not install the Beam CLI");
			}
		}

		private static bool CheckForBuildedSource(bool outputNotFoundError = false)
		{
			if (!USE_SRC)
			{
				SessionState.EraseString(SRC_BEAM);
				return false;
			}

			var configPath = EditorConfiguration.Instance.AdvancedCli.Value.UseFromSource!.Value;
			VerboseLog("Check for built source");

			if (!File.Exists(configPath))
			{
				VerboseLog($"CLI project file specified in config({configPath} ) does not exist, returning false");
				SessionState.EraseString(SRC_BEAM);
				return false;
			}

			try
			{
				var cliRelativePath = Path.GetDirectoryName(configPath);
				var cliAbsolutePath = Path.GetFullPath(cliRelativePath!);
				var cliBuildPath = Path.Combine(cliAbsolutePath, "bin");

				if (Directory.Exists(cliBuildPath))
				{
					var cliBuildArtifacts = Directory.EnumerateFiles(
						cliBuildPath,
						"Beamable.Tools.dll",
						SearchOption.AllDirectories
					);

					var exeFile = cliBuildArtifacts.FirstOrDefault();
					if (!string.IsNullOrWhiteSpace(exeFile))
					{
						VerboseLog($"Found Beamable.Tools.dll at {exeFile[0]}");
						SessionState.SetString(SRC_BEAM, Path.GetFullPath(exeFile));
						return true;
					}
				}

				if (outputNotFoundError)
				{
					BeamableLogger.LogError($"Beamable.Tools.dll (CLI artifact) not found in '{cliBuildPath}'. Please build dll from CLI solution, or change EditorConfiguration to use global installed cli binary.");
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			SessionState.EraseString(SRC_BEAM);
			return false;
		}

		static void BuildTool()
		{
			VerboseLog("Building CLI from source...");

			var configPath = EditorConfiguration.Instance.AdvancedCli.Value.UseFromSource.Value;
			var cliRelativePath = Path.GetDirectoryName(configPath);
			var cliAbsolutePath = Path.GetFullPath(cliRelativePath!);

			if (!Directory.Exists(cliAbsolutePath))
			{
				BeamableLogger.LogError($"Failed to build CLI from source. Working directory '{cliAbsolutePath}' does not exist.");
				return;
			}

			var proc = new Process();
			proc.StartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				WorkingDirectory = cliAbsolutePath,
				Arguments = "build -c Release -f net6.0",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");
			var stdErr = "";
			var stdOut = "";
			proc.OutputDataReceived += (sender, args) =>
			{
				if (!string.IsNullOrEmpty(args.Data))
				{
					stdOut += args.Data;
				}
			};
			proc.ErrorDataReceived += (sender, args) =>
			{
				if (!string.IsNullOrEmpty(args.Data))
				{
					stdErr += args.Data;
				}
			};

			proc.Start();
			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			proc.WaitForExit();


			if (!string.IsNullOrWhiteSpace(stdErr) || proc.ExitCode > 0)
			{
				var output = "";
				output += string.IsNullOrEmpty(stdErr) ? "" : $"stderr: {stdErr}\n";
				BeamableLogger.LogError($"Failed to build CLI from source.\n{output}");
			}
			VerboseLog($"Building CLI completed with exit code '{proc.ExitCode}'.");
		}

		static bool InstallTool()
		{
			if (string.IsNullOrEmpty(CLI_VERSIONED_HOME))
			{
				BeamableLogger.LogError("Unable to install BeamCLI from package: the provided cli home directory is blank.");
				return false;
			}

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
