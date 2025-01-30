using Beamable.Common;
using Beamable.Editor.Modules.EditorConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Dotnet
{
	public static partial class DotnetUtil
	{
		private static readonly PackageVersion REQUIRED_INSTALL_VERSION = "8.0.302";
		public static readonly string DOTNET_EXEC = "dotnet.dll";
		public static readonly string DOTNET_GLOBAL_CONFIG_PATH = "global.json";
		public static readonly string DOTNET_GLOBAL_CONFIG = "{\n  \"sdk\": {\n    \"version\": \"8.0.302\"\n} \n}";

		/// <summary>
		/// Beamable 2.0+ requires Dotnet.
		/// This method will ensure Dotnet exists for use with the Unity SDK.
		/// Dotnet is searched for in the following directories
		/// <list type="bullet">
		/// <item> the value of a local ENV variable, BEAMABLE_DOTNET_PATH </item>
		/// <item> in the /Library folder of the current Unity project </item>
		/// <item> in the default install directory for Dotnet </item>
		/// </list>
		///
		/// The first location where Dotnet is found will be used. However,
		/// Dotnet must be the correct version, major version 6. If that version is
		/// not installed, then this method assumes Dotnet is not available.
		///
		/// If Dotnet is not available, this method will install the correct version of Dotnet
		/// into the /Library folder of the current Unity project. 
		/// </summary>
		public static void InitializeDotnet()
		{
			if (!TryGetDotnetFilePath(out var path))
			{
				InstallDotnetToLibrary();
				if (!TryGetDotnetFilePath(out path))
				{
					throw new Exception("Beamable unable to start because no Dotnet exists");
				}
			}

			WriteGlobalDotnet();
		}

		private static void WriteGlobalDotnet()
		{
			if (!File.Exists(DOTNET_GLOBAL_CONFIG_PATH))
			{
				File.WriteAllText(DOTNET_GLOBAL_CONFIG_PATH, DOTNET_GLOBAL_CONFIG);
			}
		}

		public static bool InstallLocalManifest()
		{
			var proc = new Process();

			var installCommand = $"new tool-manifest --force";

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
				Debug.LogError("Unable to create local manifest: " + error + " / " + output);
			}
			return proc.ExitCode == 0;
		}

		static void InstallDotnetToLibrary()
		{
			EditorUtility.DisplayProgressBar("Downloading Dotnet", "Beamable is fetching dotnet installer", .1f);
			DownloadInstallScript();

			EditorUtility.DisplayProgressBar("Downloading Dotnet", "Beamable is installing dotnet in your Library folder", .2f);
			RunInstallScript(REQUIRED_INSTALL_VERSION.ToString());

			EditorUtility.ClearProgressBar();
		}

		public static bool CheckDotnetInfo(out Dictionary<string, string> pathByVersion)
		{
			var proc = new Process();

			var infoCommand = $" --info";

			proc.StartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				WorkingDirectory = Path.GetFullPath("."),
				Arguments = infoCommand,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			proc.Start();
			proc.WaitForExit();

			var output = proc.StandardOutput.ReadToEnd();
			var error = proc.StandardError.ReadToEnd();

			string sdkSection = output.Split(new string[] {".NET runtimes installed:"}, StringSplitOptions.None)[0];
			Regex regex = new Regex(@"(\d+\.\d+\.\d+)\s+\[(.+)\]");
			pathByVersion = new Dictionary<string, string>();

			foreach (Match match in regex.Matches(sdkSection))
			{
				if (!pathByVersion.ContainsKey(match.Groups[1].Value))
				{
					pathByVersion.Add(match.Groups[1].Value,
					                  Path.Combine(match.Groups[2].Value, match.Groups[1].Value));
				}
			}

			return proc.ExitCode == 0;
		}

		public static bool TryGetDotnetFilePath(out string filePath)
		{
			filePath = null;

			if (!CheckDotnetInfo(out Dictionary<string, string> pathByVersion))
			{
				return false;
			}

			foreach (var path in pathByVersion)
			{
				var dotnetPath = Path.Combine(path.Value, DOTNET_EXEC);
				if (!CheckForDotnetAtPath(dotnetPath))
				{
					continue;
				}

				if (!(path.Key == REQUIRED_INSTALL_VERSION))
				{

					Debug.LogWarning(
						$"Ignoring version of dotnet at {path} due to incorrect version number. Found: {path.Key}, required: {REQUIRED_INSTALL_VERSION}");
					continue;
				}

				filePath = path.Value;
				return true;
			}

			return false;
		}

		static bool CheckVersion(string dotnetPath, out PackageVersion version)
		{
			version = "0.0.0";
			var dir = Path.GetDirectoryName(dotnetPath)!;
			var proc = new Process();
			proc.StartInfo = new ProcessStartInfo
			{
				FileName = Path.GetFullPath(dotnetPath),
				WorkingDirectory = Path.GetFullPath(dir),
				Arguments = "--version",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true
			};

			proc.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");


			proc.Start();
			proc.WaitForExit();
			var output = proc.StandardOutput.ReadToEnd().Replace("\r\n", string.Empty);
			if (string.IsNullOrWhiteSpace(output))
			{
				return false;
			}

			if (!PackageVersion.TryFromSemanticVersionString(output, out version))
			{
				return false;
			}

			return version == REQUIRED_INSTALL_VERSION ;
		}

		static bool CheckForDotnetAtPath(string dotnetPath)
		{
			return File.Exists(dotnetPath);
		}
	}
}
