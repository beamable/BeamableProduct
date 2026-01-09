using Beamable.Common;
using Beamable.Editor.Modules.EditorConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Dotnet
{
	public static partial class DotnetUtil
	{
		private static readonly PackageVersion DOTNET_8_VERSION = "8.0.302";
		private static readonly PackageVersion DOTNET_10_VERSION = "10.0.100";
		private static readonly PackageVersion[] ALLOWED_DOTNET_VERSIONS = new PackageVersion[]
		{
			// version 8 will work with CLI 7, because CLI 7 is built for both net versions.
			DOTNET_8_VERSION,
			
			// version 10 is the new default, starting with CLI 7 
			DOTNET_10_VERSION
		};
		public static readonly string DOTNET_EXEC = "dotnet.dll";
		public static readonly string DOTNET_GLOBAL_CONFIG_PATH = "global.json";

		public const string DOTNET_TEMPLATE_ARG_VERSION = "__DOTNET_VERSION__";
		public const string DOTNET_TEMPLATE_GLOBAL_CONFIG = "{\n  \"sdk\": {\n    \"version\": \"" + DOTNET_TEMPLATE_ARG_VERSION +  "\"\n} \n}";

		static bool DotnetHandled
		{
			get => SessionState.GetBool(nameof(DotnetHandled), false);
			set => SessionState.SetBool(nameof(DotnetHandled), value);
		}

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
			if (DotnetHandled) return;
			if (!TryGetDotnetFilePath())
			{
				InstallDotnetToLibrary();
				if (!TryGetDotnetFilePath())
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
				var info = CheckDotnetInfo();
				var globalContent = DOTNET_TEMPLATE_GLOBAL_CONFIG.Replace(DOTNET_TEMPLATE_ARG_VERSION, info.PreferredDotnetVersion);
				File.WriteAllText(DOTNET_GLOBAL_CONFIG_PATH, globalContent);
			}

			DotnetHandled = true;
		}

		

		static void InstallDotnetToLibrary()
		{
			var installed = string.Empty;
			var info = CheckDotnetInfo();
			if (!info.HasAnyDotnet)
			{
				installed = $" Currently installed: {string.Join(", ", info.versionToPath.Keys)}.";
			}
			
			var message = $"Beamable Unity SDK requires Dotnet SDK {string.Join(" or ", ALLOWED_DOTNET_VERSIONS.Select(x => x.ToString()))} to function properly. {installed} Please download the SDK Installer and proceed with the installation before continuing.";

			if (Application.isBatchMode)
			{
				throw new Exception($"Cannot find dotnet, and cannot install in batch-mode. message=[{message}]");
			}
			if (EditorUtility.DisplayDialog("Dotnet Installation Required", message,"Download", "Close"))
        	{
				Application.OpenURL(GetDotnetDownloadLink_10());
				if (EditorUtility.DisplayDialog("Dotnet Installation Required", "Waiting for dotnet installation before proceeding", "Ok"))
				{
					// We don't need to do anything here, just continue the flow and the next thing will be checking if dotnet was successfuly installed
				}
			}
		}

		public class DotnetInfoResult
		{
			public string PreferredDotnetVersion
			{
				get
				{
					// if the user has old dotnet, use that.
					if (HasNet8 && !HasNet10)
					{
						return DOTNET_8_VERSION.ToString();
					}

					// otherwise, always use the latest.
					return DOTNET_10_VERSION.ToString();
				}
			}
			public bool HasAnyDotnet => HasNet8 || HasNet10;

			public bool HasNet8
			{
				get
				{
					foreach (var version in versionToPath.Keys)
					{
						// ignore non 8.x versions
						if (version.Major != 8) continue;
						
#if BEAM_RESTRICT_DOTNET_VERSION
						return version == DOTNET_8_VERSION;
#endif

						// the version needs to match our min required version
						if (version >= DOTNET_8_VERSION)
						{
							return true;
						}

					}

					return false;
				}
			}
			public bool HasNet10 {
				get
				{
					foreach (var version in versionToPath.Keys)
					{
						// ignore non 10.x versions
						if (version.Major != 10) continue;
						
#if BEAM_RESTRICT_DOTNET_VERSION
						return version == DOTNET_10_VERSION;
#endif

						// the version needs to match our min required version
						if (version >= DOTNET_10_VERSION)
						{
							return true;
						}
					}

					return false;
				}
			}

			public Dictionary<PackageVersion, string> versionToPath = new Dictionary<PackageVersion, string>();
		}
		
		public static DotnetInfoResult CheckDotnetInfo()
		{
			var info = new DotnetInfoResult();
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
			proc.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");
			proc.StartInfo.Environment.Add("MSBUILDTERMINALLOGGER", "off");
			try
			{
				proc.Start();
				proc.WaitForExit(10 * 1000);

				var output = proc.StandardOutput.ReadToEnd();

				string sdkSection =
					output.Split(new string[] { ".NET runtimes installed:" }, StringSplitOptions.None)[0];
				Regex regex = new Regex(@"(\d+\.\d+\.\d+)\s+\[(.+)\]");

				foreach (Match match in regex.Matches(sdkSection))
				{
					if (!info.versionToPath.ContainsKey(match.Groups[1].Value))
					{
						info.versionToPath.Add(match.Groups[1].Value,
							Path.Combine(match.Groups[2].Value, match.Groups[1].Value));
					}
				}
			}
			catch
			{
				// let it gooo!
			}

			return info;
		}

		public static string GetDotnetDownloadLink_10()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X86:
						return "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.100-windows-x86-installer";
					case Architecture.X64:
						return "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.100-windows-x64-installer";
					case Architecture.Arm64:
						return "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.100-windows-arm64-installer";
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						return "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.100-macos-x64-installer";
					case Architecture.Arm64:
						return "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.100-macos-arm64-installer";
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return "https://learn.microsoft.com/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website";
			}

			throw new NotImplementedException("unsupported os");
		}
		
		public static string GetDotnetDownloadLink_8()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						return "https://download.visualstudio.microsoft.com/download/pr/b6f19ef3-52ca-40b1-b78b-0712d3c8bf4d/426bd0d376479d551ce4d5ac0ecf63a5/dotnet-sdk-8.0.302-win-x64.exe";
					case Architecture.Arm64:
						return "hhttps://download.visualstudio.microsoft.com/download/pr/a98d10f0-ae96-4d7f-be23-613fe9fc22cc/cd29f30a839a98d39d3df639a810f658/dotnet-sdk-8.0.302-win-arm64.exe";
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						return "https://download.visualstudio.microsoft.com/download/pr/5b488f80-2155-4b14-9865-50f328574283/e9126ea28af0375173a18e1d8a6a3086/dotnet-sdk-8.0.302-osx-x64.pkg";
					case Architecture.Arm64:
						return "https://download.visualstudio.microsoft.com/download/pr/348456db-b1d0-49ce-930d-4e905ed17efd/a39c5b23c669ed9b270e0169eea2b83b/dotnet-sdk-8.0.302-osx-arm64.pkg";
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return "https://github.com/dotnet/core/blob/main/release-notes/8.0/install-linux.md";
			}

			throw new NotImplementedException("unsupported os");
		}

		public static bool TryGetDotnetFilePath()
		{
			var info = CheckDotnetInfo();
			if (!info.HasAnyDotnet)
			{
				return false;
			}

			return true;
		}

	}
}
