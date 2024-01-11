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
		static bool USE_GLOBAL
		{
			get
			{
				var config = EditorConfiguration.Instance;
				if (config == null || !config.AdvancedCli.HasValue) return false;

				return config.AdvancedCli.Value.UseGlobalCLI;
			}
		}

		static bool USE_SRC
		{
			get
			{
				var config = EditorConfiguration.Instance;
				if (config == null || !config.AdvancedCli.HasValue) return false;

				return config.AdvancedCli.Value.UseFromSource.HasValue;
			}
		}

		private static string DEFAULT_VERSION
		{
			get
			{
				var config = EditorConfiguration.Instance;
				if (config == null || !config.AdvancedCli.HasValue) return "0.0.0";

				return config.AdvancedCli.Value.DefaultCLIVersion;
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

				if (USE_GLOBAL)
				{
#if UNITY_EDITOR_WIN
                return System.Environment.ExpandEnvironmentVariables("%USERPROFILE%\\.dotnet\\tools");
#else
					return Path.Combine(DotnetUtil.DOTNET_GLOBAL_PATH, "tools");
#endif
				}
				const string CLI_LIBRARY = "Library/BeamableEditor/BeamCLI";
				var requiredVersion = BeamableEnvironment.SdkVersion;

				var requiredVersionStr = requiredVersion.ToString();
				// if the required version is 0.0.0, then we want the latest CLI
				if (requiredVersionStr == "0.0.0")
				{
					requiredVersionStr = DEFAULT_VERSION;
				}
				return Path.Combine(CLI_LIBRARY, requiredVersionStr);
			}
		}

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

		const string SRC_BEAM = "BUILDED_BEAM";
#if UNITY_EDITOR_WIN
       private const string EXEC = "beam.exe";
#else
		private const string EXEC = "beam";
#endif

		// [System.Diagnostics.Conditional("SPEW_ALL")]
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
			Debug.Log("WHYISDEVBROKEN: done with dotnet.");

			Debug.Log($"WHYISDEVBROKEN: useSrc={USE_SRC} useGlobal={USE_GLOBAL} cliPath={ File.Exists(CLI_PATH)}");
			if (USE_SRC)
			{
				if (CheckForBuildedSource())
				{
					Debug.Log("WHYISDEVBROKEN: built source already exists");
					return;
				}

				BuildTool();
				Debug.Log("WHYISDEVBROKEN: built tool");

				if (CheckForBuildedSource(outputNotFoundError: true))
				{
					Debug.Log("WHYISDEVBROKEN: built source okay dokes");
					return;
				}
				
				Debug.Log("WHYISDEVBROKEN: hmm, uh oh");

			}

			Debug.Log($"WHYISDEVBROKEN: take 2. useSrc={USE_SRC} useGlobal={USE_GLOBAL} cliPath={ File.Exists(CLI_PATH)}");

			if (USE_GLOBAL || File.Exists(CLI_PATH))
			{
				// if using global, we make no promises about anything. 
				// or, if the cli exists, we are good 
				return;
			}

			// need to install the CLI
			var installResult = InstallTool();

			if (!installResult || !File.Exists(CLI_PATH))
			{
				// if the CLI still doesn't exist at the path, something went wrong.
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
			BeamableLogger.LogError("I am a known error");

			var configPath = EditorConfiguration.Instance.AdvancedCli.Value.UseFromSource.Value;
			var cliRelativePath = Path.GetDirectoryName(configPath);
			var cliAbsolutePath = Path.GetFullPath(cliRelativePath!);

			BeamableLogger.Log($"WHYISDEVBROKEN: {configPath} -- {cliRelativePath} -- {cliAbsolutePath} -- {DotnetUtil.DotnetPath}");
			if (!Directory.Exists(cliAbsolutePath))
			{
				BeamableLogger.LogError($"Failed to build CLI from source. Working directory '{cliAbsolutePath}' does not exist.");
				return;
			}

			var proc = new Process();
			proc.StartInfo = new ProcessStartInfo
			{
				FileName = Path.GetFullPath(DotnetUtil.DotnetPath),
				WorkingDirectory = cliAbsolutePath,
				Arguments = "build -c Release -f net6.0",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");
			proc.OutputDataReceived += (sender, args) => Debug.Log("WHYISDEVBROKEN: (cli stdout) " + args);
			proc.ErrorDataReceived += (sender, args) => Debug.Log("WHYISDEVBROKEN: (cli stderr) " + args);

			proc.Start();
			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			BeamableLogger.Log("WHYISDEVBROKEN: running the install, yo");

			proc.WaitForExit();
			BeamableLogger.Log("WHYISDEVBROKEN: ran the install, yo");

			var stdout = proc.StandardOutput.ReadToEnd();
			var stderr = proc.StandardError.ReadToEnd();
			if (!string.IsNullOrWhiteSpace(stderr) || proc.ExitCode > 0)
			{
				var output = "";
				output += string.IsNullOrEmpty(stderr) ? "" : $"stderr: {stderr}\n";
				output += string.IsNullOrEmpty(stdout) ? "" : $"stdout: {stdout}";
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

			Directory.CreateDirectory(CLI_VERSIONED_HOME);
			var proc = new Process();
			var fullDirectory = Path.GetFullPath(CLI_VERSIONED_HOME);
			var installCommand = $"tool install beamable.tools --tool-path \"{fullDirectory}\"";
			if (!BeamableEnvironment.SdkVersion.ToString().Equals("0.0.0"))
			{
				installCommand += $" --version {BeamableEnvironment.SdkVersion}";
			}
			proc.StartInfo = new ProcessStartInfo
			{
				FileName = Path.GetFullPath(DotnetUtil.DotnetPath),
				WorkingDirectory = Path.GetFullPath("Library"),
				Arguments = installCommand,
				UseShellExecute = false,
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
