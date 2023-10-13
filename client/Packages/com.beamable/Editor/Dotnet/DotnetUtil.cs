using Beamable.Common;
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
		
		private static readonly string DOTNET_GLOBAL_PATH = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".dotnet");
		
		
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

		public static void InitializeDotnet()
		{
			if (TryGetDotnetFilePath(out var path))
			{
				DotnetHome = path;
				Debug.Log("--- DOTNET AT " + DotnetHome);
			}
			else
			{
				InstallDotnetToLibrary();
				if (TryGetDotnetFilePath(out path))
				{
					DotnetHome = path;
					Debug.Log("--- DOTNET INSTALLED AT " + path);
				}
				else
				{
					Debug.LogError("----- NO DOTNET FOUND");
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
				
				var dotnetPath = Path.Join(path, "dotnet");
				if (!CheckForDotnetAtPath(dotnetPath))
				{
					continue;
				}

				if (!CheckVersion(dotnetPath))
				{
					Debug.Log("Wrong version number");
					continue;
				}
				
				// ah, dotnet exists at this path
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
				FileName = dotnetPath, 
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
