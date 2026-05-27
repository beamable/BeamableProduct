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

				// Only add the local folder feed for the local dev version (0.0.123.*). For a published
				// version, installing from a local folder feed extracts into the global packages folder
				// without copying the .nupkg, which then crashes finalization; let it resolve from nuget.org.
				if (Application.isBatchMode && BeamableEnvironment.NugetPackageVersion.ToString().StartsWith("0.0.123"))
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
				// Remove a corrupt global-packages entry (extracted, missing its .nupkg) so the install re-downloads cleanly instead of short-circuiting on it.
				HealCorruptGlobalPackagesEntry();
				Debug.Log($"[BeamCLI] Installing Beam CLI version [{BeamableEnvironment.NugetPackageVersion}]. " +
				          $"Command: [dotnet {installCommand}]. A first-time install with a cold NuGet cache can be slow while packages download.");
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
					Debug.Log("Failed Install Command: " + installCommand);
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
					var timeoutMs = 30 * 1000 * currentTry;
					Debug.Log($"[BeamCLI] Install attempt {currentTry} starting (timeout {timeoutMs / 1000}s)...");
					var stopwatch = Stopwatch.StartNew();
					proc.Start();
					if (proc.WaitForExit(timeoutMs))
					{
						Debug.Log($"[BeamCLI] Install attempt {currentTry} exited with code {proc.ExitCode} after {stopwatch.Elapsed.TotalSeconds:0.#}s.");
						return true;
					}

					// Kill the still-running process before retrying; a concurrent retry corrupts the NuGet cache (a version folder left without its .nupkg), which then fails every later restore.
					Debug.LogError(
						$"[BeamCLI] Install attempt {currentTry} did not finish within {timeoutMs / 1000}s. " +
						"Killing the dotnet process before retrying to avoid corrupting the NuGet cache.");
					try
					{
						if (!proc.HasExited)
						{
							proc.Kill();
						}
						proc.WaitForExit();
						Debug.Log($"[BeamCLI] Killed the timed-out dotnet process from attempt {currentTry}.");
					}
					catch (Exception killEx)
					{
						Debug.LogWarning($"[BeamCLI] Failed to kill the timed-out dotnet install process: {killEx.Message}");
					}

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

					Debug.Log($"[BeamCLI] Retrying install with a longer timeout (attempt {currentTry + 1}).");
					return TryRunWithTimeout(++currentTry);

				}

				void HealCorruptGlobalPackagesEntry()
				{
					try
					{
						var packageId = "beamable.tools";
						var version = BeamableEnvironment.NugetPackageVersion.ToString().ToLowerInvariant();
						var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
						if (string.IsNullOrEmpty(globalPackages))
						{
							var home = Environment.GetEnvironmentVariable("HOME");
							if (string.IsNullOrEmpty(home))
							{
								home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
							}
							globalPackages = Path.Combine(home, ".nuget", "packages");
						}

						var packageDir = Path.Combine(globalPackages, packageId, version);
						if (!Directory.Exists(packageDir))
						{
							Debug.Log($"[BeamCLI] No pre-existing global-packages entry at [{packageDir}].");
							return;
						}

						var nupkgCount = Directory.GetFiles(packageDir, "*.nupkg").Length;
						var totalFiles = Directory.GetFiles(packageDir, "*", SearchOption.AllDirectories).Length;
						Debug.Log($"[BeamCLI] Found global-packages entry [{packageDir}]: {nupkgCount} .nupkg, {totalFiles} total files.");
						if (nupkgCount == 0)
						{
							Debug.LogWarning($"[BeamCLI] Entry [{packageDir}] is missing its .nupkg (corrupt). Deleting it so the install can re-download a complete copy.");
							Directory.Delete(packageDir, true);
						}
					}
					catch (Exception healEx)
					{
						Debug.LogWarning($"[BeamCLI] Auto-heal of the global-packages cache failed: {healEx.Message}");
					}
				}
			}
		}
	}
}
