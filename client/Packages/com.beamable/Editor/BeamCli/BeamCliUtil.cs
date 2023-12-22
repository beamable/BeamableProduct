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
				VerboseLog("Check for built source");
				if (CheckForBuildedSource())
					return;

				BuildTool();
				VerboseLog("Check for built source ");
				if (CheckForBuildedSource())
					return;
			}

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

		private static bool CheckForBuildedSource()
		{
			if (!USE_SRC)
			{
				SessionState.EraseString(SRC_BEAM);
				return false;
			}

			if (!File.Exists(EditorConfiguration.Instance.AdvancedCli.Value.UseFromSource.Value))
			{
				SessionState.EraseString(SRC_BEAM);
				return false;
			}

			try
			{
				var dir = Path.Combine(Directory.GetCurrentDirectory(),Path.GetDirectoryName(EditorConfiguration.Instance.AdvancedCli.Value.UseFromSource.Value));
				List<string> exeFile = Directory
									   .EnumerateFiles(
										   dir,
										   EXEC.Replace("beam", "Beamable.Tools"), SearchOption.AllDirectories).ToList();

				if (exeFile.Count > 0)
				{
					var exe = exeFile.FirstOrDefault(s => s.Contains("publish")) ?? exeFile[0];
					SessionState.SetString(SRC_BEAM, Path.GetFullPath(exe));
					return true;
				}
			}
			catch (Exception e)
			{
				//da duck?!
				Debug.LogException(e);
			}
			SessionState.EraseString(SRC_BEAM);
			return false;
		}

		static void BuildTool()
		{
			var proc = new Process();
			var dir = Path.Combine(Directory.GetCurrentDirectory(),Path.GetDirectoryName(EditorConfiguration.Instance.AdvancedCli.Value.UseFromSource.Value));

			proc.StartInfo = new ProcessStartInfo
			{
				FileName = Path.GetFullPath(DotnetUtil.DotnetPath),
				WorkingDirectory = dir,
				Arguments = "publish -c Release --self-contained -p:PublishReadyToRun=true -f net6.0",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			proc.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");
			VerboseLog("Build tool from scratch");
			proc.Start();
			proc.WaitForExit();
			var output = proc.StandardOutput.ReadToEnd();
			var error = proc.StandardError.ReadToEnd();
			if (!string.IsNullOrWhiteSpace(error))
			{
				Debug.LogError("Unable to install BeamCLI: " + error + " / " + output);
			}
		}

		static bool InstallTool()
		{
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
