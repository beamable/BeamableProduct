using Beamable.Common;
using Beamable.Editor.Modules.EditorConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Dotnet
{
	public static partial class DotnetUtil
	{
		private static readonly PackageVersion REQUIRED_INSTALL_VERSION = "8.0.302";

		private const string ENV_VAR_DOTNET_LOCATION = "BEAMABLE_DOTNET_PATH";

#if UNITY_EDITOR_WIN
		private const string DOTNET_LIBRARY_PATH = "Library\\BeamableEditor\\Dotnet";
		public static readonly string DOTNET_GLOBAL_PATH = "C:\\Program Files\\dotnet";
		public static readonly string DOTNET_EXEC = "dotnet.exe";
#else
		private const string DOTNET_LIBRARY_PATH = "Library/BeamableEditor/Dotnet";
		public static readonly string DOTNET_GLOBAL_PATH =
			Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".dotnet");

		public static readonly string DOTNET_EXEC = "dotnet";
#endif

		/// <summary>
		/// this list is in order precedence 
		/// </summary>
		static string[] _dotnetLocationCandidates = new string[]
		{
			System.Environment.GetEnvironmentVariable(ENV_VAR_DOTNET_LOCATION),
			System.Environment.GetEnvironmentVariable("DOTNET_ROOT"),
			DOTNET_LIBRARY_PATH,
			DOTNET_GLOBAL_PATH
		};

		public static string DotnetHome { get; private set; }
		public static string DotnetPath => Path.Combine(DotnetHome, DOTNET_EXEC);
		public static bool IsUsingGlobalDotnet => DotnetHome.Equals(DOTNET_GLOBAL_PATH);

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
			if (TryGetDotnetFilePath(out var path))
			{
				DotnetHome = path;
			}
			else
			{
				InstallDotnetToLibrary();
				if (TryGetDotnetFilePath(out path))
				{
					DotnetHome = path;
				}
				else
				{
					throw new Exception("Beamable unable to start because no Dotnet exists");
				}
			}
			SetDotnetEnvironmentPathVariable(DotnetHome);
		}

		static void InstallDotnetToLibrary()
		{
			EditorUtility.DisplayProgressBar("Downloading Dotnet", "Getting install script", .1f);
			DownloadInstallScript();

			EditorUtility.DisplayProgressBar("Downloading Dotnet", "installing dotnet in your Library folder", .2f);
			RunInstallScript(REQUIRED_INSTALL_VERSION.ToString());

			EditorUtility.ClearProgressBar();
		}

		static bool TryGetDotnetFilePath(out string filePath)
		{
			filePath = null;

			foreach (var path in _dotnetLocationCandidates)
			{
				if (path == null) continue;

				var dotnetPath = Path.Combine(path, DOTNET_EXEC);
				if (!CheckForDotnetAtPath(dotnetPath))
				{
					continue;
				}

				if (!CheckVersion(dotnetPath, out var version))
				{
					
					Debug.LogWarning(
						$"Ignoring version of dotnet at {path} due to incorrect version number. Found: {version}, required: {REQUIRED_INSTALL_VERSION}");
					continue;
				}

				filePath = path;
				return true;
			}

			return false;
		}

		static void SetDotnetEnvironmentPathVariable(string pathToDotnet)
		{
			var name = "PATH";
			var scope = EnvironmentVariableTarget.Machine;
			var oldValue = System.Environment.GetEnvironmentVariable(name, scope);
			var newValue  = oldValue + @";" + pathToDotnet;
			System.Environment.SetEnvironmentVariable(name, newValue, scope);
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
