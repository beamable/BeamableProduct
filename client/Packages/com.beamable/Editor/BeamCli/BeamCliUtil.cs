using Beamable.Editor.Dotnet;
using Beamable.Editor.Modules.EditorConfig;
using System;
using System.Diagnostics;
using System.IO;
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
				if (USE_GLOBAL)
				{
					return Path.Combine(DotnetUtil.DOTNET_GLOBAL_PATH, "tools");
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

		public static string CLI_PATH => Path.Combine(CLI_VERSIONED_HOME, "beam");

		/// <summary>
		/// Installs the Beam CLI into the /Library folder of the current project.
		/// </summary>
		public static void InitializeBeamCli()
		{
			// we need dotnet before we can initialize the CLI
			DotnetUtil.InitializeDotnet();

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

		static bool InstallTool()
		{
			Directory.CreateDirectory(CLI_VERSIONED_HOME);
			var proc = new Process();
			var fullDirectory = Path.GetFullPath(CLI_VERSIONED_HOME);
			proc.StartInfo = new ProcessStartInfo
			{
				FileName = DotnetUtil.DotnetPath,
				WorkingDirectory = "Library",
				Arguments = $"tool install beamable.tools --tool-path {fullDirectory}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			proc.Start();
			proc.WaitForExit();
			var output = proc.StandardOutput.ReadToEnd();
			var error = proc.StandardError.ReadToEnd();
			if (!string.IsNullOrEmpty(error))
			{
				Debug.LogError("Unable to install BeamCLI: " + error + " / " + output);
			}
			return proc.ExitCode == 0;
		}
	}
}