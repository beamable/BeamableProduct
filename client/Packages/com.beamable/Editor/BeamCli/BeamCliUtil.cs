using Beamable.Common;
using Beamable.Editor.Dotnet;
using Beamable.Editor.Modules.EditorConfig;
using Beamable.Editor.UI.OptionDialogWindow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
		static string InstalledVersion
		{
			get => SessionState.GetString(nameof(InstalledVersion), string.Empty);
			set => SessionState.SetString(nameof(InstalledVersion), value);
		}

		/// <summary>
		/// Installs the Beam CLI into the /Library folder of the current project.
		/// </summary>
		public static void InitializeBeamCli()
		{
			try
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
			catch (Exception ex)
			{
				Debug.LogError("Failed to init beam cli");
				Debug.LogError(ex);
				throw;
			}
		}

		/// <summary>
		/// this exists for CI jobs. This function should be run before hand. 
		/// </summary>
		public static void InstallToolFromLocalPackageSource()
		{
			var path = "BeamableNugetSource";
			if (!Directory.Exists(path))
			{
				Debug.Log("------ No package source exists...");
				return;
			}

			var files = Directory.GetFiles(path);
			Debug.Log($"------ Found {files?.Length} files at path package source=[{Path.GetFullPath(path)}]");
			foreach (var file in files)
			{
				Debug.Log($"----- file=[{file}]");
			}
			
			
			Debug.Log("------ INSTALL TOOL FROM LOCAL PACKAGE SOURCE");
			var process = new Process();
			process.StartInfo.FileName = "dotnet";
			process.StartInfo.Arguments = $"nuget add source {Path.GetFullPath(path)} --name BeamableNugetSource";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.Start();
			Debug.Log("------ INSTALL LOGS TOOL FROM LOCAL PACKAGE SOURCE");
			Debug.Log(process.StandardOutput.ReadToEnd());
			Debug.LogError(process.StandardError.ReadToEnd());
			if (!process.WaitForExit(10 * 1000))
			{
				Debug.Log("------ INSTALLED TOOL FROM LOCAL PACKAGE SOURCE EXPIRED");
			}
			Debug.Log("------ INSTALLED TOOL FROM LOCAL PACKAGE SOURCE");
			
			process = new Process();
			process.StartInfo.FileName = "dotnet";
			process.StartInfo.Arguments = "nuget list source";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.Start();
			Debug.Log("------ LIST LOCAL SOURCES LOGS");
			Debug.Log(process.StandardOutput.ReadToEnd());
			Debug.LogError(process.StandardError.ReadToEnd());
			if (!process.WaitForExit(10 * 1000))
			{
				Debug.Log("------  LIST LOCAL SOURCES EXPIRED");
			}
			
			BeamCliUtil.InitializeBeamCli();
			Debug.Log("------  BEAM CLI FINISHED");
			
			System.Environment.SetEnvironmentVariable("BEAM_UNITY_TEST_CI", "true");
		}

		public static object _installLock = new object();
		
		static bool InstallTool()
		{
			lock (_installLock)
			{
				if (EditorConfiguration.Instance.IgnoreCliVersionRequirement)
				{
					// the developer has opted out of the cli version requirement. 
					return true;
				}

				if (InstalledVersion.Equals(BeamableEnvironment.NugetPackageVersion))
				{
					return true;
				}

				var proc = new Process();
				var installCommand = $"tool install Beamable.Tools --create-manifest-if-needed --allow-downgrade";

				if (Application.isBatchMode)
				{
					installCommand += " --add-source BeamableNugetSource ";
				}

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
				TryRunWithTimeout(1);

				const string errorGuide =
					"Please try installing manually by https://help.beamable.com/CLI-Latest/cli/guides/getting-started/#installing or contact Beamable for further support.";

				var output = proc.StandardOutput.ReadToEnd();
				var error = proc.StandardError.ReadToEnd();
				if (!string.IsNullOrWhiteSpace(error) || proc.ExitCode != 0)
				{
					StringBuilder message = new StringBuilder("Unable to install BeamCLI");
					if (!string.IsNullOrEmpty(output))
					{
						message.AppendLine($"Output: {output}");
					}

					message.AppendLine($"Error: {error}");
					message.Append(errorGuide);
					Debug.LogError(message.ToString());
					var tryAgainButtonInfo = new OptionDialogWindow.ButtonInfo()
					{
						Name = "Try Again",
						OnClick = () => true,
						Color = new Color(0.08f, 0.44f, 0.82f)
					};
					var closeUnityButtonInfo = new OptionDialogWindow.ButtonInfo()
					{
						Name = "Close Unity",
						OnClick = () => false,
						Color = Color.gray,
					};
					var openDocsButtonInfo = new OptionDialogWindow.ButtonInfo()
					{
						Name = "Close Unity & Open Docs",
						OnClick = () =>
						{
							Application.OpenURL(
								"https://help.beamable.com/CLI-Latest/cli/guides/getting-started/#installing");
							return false;
						},
						Color = Color.gray,
					};
					if (!OptionDialogWindow.ShowModal("Error when Installing BeamCLI", message.ToString(),
					                                  tryAgainButtonInfo, closeUnityButtonInfo, openDocsButtonInfo))
					{
						EditorApplication.Exit(0);
					}
				}

				if (proc.ExitCode == 0)
				{
					InstalledVersion = BeamableEnvironment.NugetPackageVersion;
				}

				return proc.ExitCode == 0;

				bool TryRunWithTimeout(int currentTry)
				{
					proc.Start();
					if (proc.WaitForExit(10 * 1000 * currentTry))
					{
						return true;
					}

					Debug.LogError(
						"dotnet tool install command did not finish fast enough; timed out. Trying again with longer timeout");
					const int maxRetries = 5;
					if (currentTry > maxRetries)
					{
						string message =
							$"The BeamCLI installation could not be completed because the command did not finish in time. It timed out after {maxRetries} retries. {errorGuide}";
						Debug.LogError(message);
						bool result =
							EditorUtility.DisplayDialog("Error when Installing BeamCLI", message, "Try Again",
							                            "Close Unity");
						if (!result)
						{
							EditorApplication.Exit(0);
						}

						return false;
					}

					return TryRunWithTimeout(++currentTry);

				}
			}
		}
	}
}
