using Beamable.Common;
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Dotnet
{
	public static partial class DotnetUtil
	{
		private const int REQUIRED_MAJOR_VERSION = 6;

		private const string ENV_VAR_DOTNET_LOCATION = "BEAMABLE_DOTNET_PATH";
		private const string DOTNET_LIBRARY_PATH = "Library/BeamableEditor/Dotnet";

#if UNITY_EDITOR_WIN
		public static readonly string DOTNET_GLOBAL_PATH = "C:\\Program Files\\dotnet";
#else
		public static readonly string DOTNET_GLOBAL_PATH = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".dotnet");
#endif

		/// <summary>
		/// this list is in order precedence 
		/// </summary>
		static string[] _dotnetLocationCandidates = new string[]
		{
			System.Environment.GetEnvironmentVariable(ENV_VAR_DOTNET_LOCATION),
			DOTNET_LIBRARY_PATH,
			DOTNET_GLOBAL_PATH
		};

		public static string DotnetHome { get; private set; }
		public static string DotnetPath => Path.Combine(DotnetHome, "dotnet");

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
		}

		static void InstallDotnetToLibrary()
		{

			EditorUtility.DisplayProgressBar("Downloading Dotnet", "Getting install script", .1f);
			DownloadInstallScript();

			EditorUtility.DisplayProgressBar("Downloading Dotnet", "installing dotnet in your Library folder", .2f);
			RunInstallScript();

			EditorUtility.ClearProgressBar();
		}

		static bool TryGetDotnetFilePath(out string filePath)
		{
			filePath = null;

			foreach (var path in _dotnetLocationCandidates)
			{
				if (path == null) continue;

				var dotnetPath = Path.Combine(path, "dotnet");
				if (!CheckForDotnetAtPath(dotnetPath))
				{
					continue;
				}

				if (!CheckVersion(dotnetPath))
				{
					Debug.LogWarning($"Ignoring version of dotnet at {path} due to incorrect version number.");
					continue;
				}

				filePath = path;
				return true;
			}

			return false;
		}


		static bool CheckVersion(string dotnetPath)
		{
			var proc = new Process();
			proc.StartInfo = new ProcessStartInfo
			{
				FileName = Path.GetFullPath(dotnetPath),
				Arguments = "--version",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			};

			proc.Start();
			proc.WaitForExit();
			var output = proc.StandardOutput.ReadToEnd();

			if (!PackageVersion.TryFromSemanticVersionString(output, out var version))
			{
				return false;
			}

			return version.Major == REQUIRED_MAJOR_VERSION;
		}

		static bool CheckForDotnetAtPath(string dotnetPath)
		{
			return File.Exists(dotnetPath);
		}
	}
}