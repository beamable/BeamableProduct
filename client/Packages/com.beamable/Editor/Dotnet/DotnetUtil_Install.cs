using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Dotnet
{
	public static partial class DotnetUtil
	{
		const string WIN_DOWNLOAD_URL =
			"https://dot.net/v1/dotnet-install.ps1";

		const string MAC_DOWNLOAD_URL =
			"https://dot.net/v1/dotnet-install.sh";

		const string WIN_SCRIPT_NAME =
			"dotnet-install.ps1";

		const string MAC_SCRIPT_NAME =
			"dotnet-install.sh";

		private const string DOTNET_LIBRARY_INSTALL_SCRIPT_HOME = "Library/BeamableEditor/DotnetInstall";
		private static string DotnetInstallScriptPath => Path.Combine(DOTNET_LIBRARY_INSTALL_SCRIPT_HOME, SCRIPT_NAME);

#if UNITY_EDITOR_WIN
		const string DOWNLOAD_URL = WIN_DOWNLOAD_URL;
		const string SCRIPT_NAME = WIN_SCRIPT_NAME;
#else
		const string DOWNLOAD_URL = MAC_DOWNLOAD_URL;
		const string SCRIPT_NAME = MAC_SCRIPT_NAME;
#endif


		static void DownloadInstallScript()
		{
			Debug.Log("Sending request to download script");

			var client = new HttpClient();

			var request = client.GetStringAsync(DOWNLOAD_URL);
			System.Threading.Tasks.Task.Run(async () =>
			{
				await request;
			});

			while (!request.IsCompleted)
			{
				// wait?
				Thread.Sleep(1);
			}

			var script = request.Result;

			Directory.CreateDirectory(DOTNET_LIBRARY_INSTALL_SCRIPT_HOME);
			var scriptPath = DotnetInstallScriptPath;

			File.WriteAllText(scriptPath, script);
			if (!Chmod(scriptPath, "+x"))
			{
				Debug.LogError("Could not make file writable");
			}
		}

		// Returns true if success and false otherwise
		// permissions can be an int or a string. For example it can also be +x, -x etc..
		static bool Chmod(string filePath, string permissions = "700", bool recursive = false)
		{
#if UNITY_EDITOR_WIN
			return true;
#else
			string cmd;
			if (recursive)
				cmd = $"chmod -R {permissions} {filePath}";
			else
				cmd = $"chmod {permissions} {filePath}";

			try
			{
				using (Process proc = Process.Start("/bin/bash", $"-c \"{cmd}\""))
				{
					proc.WaitForExit();
					return proc.ExitCode == 0;
				}
			}
			catch
			{
				return false;
			}
#endif
		}

		static bool RunInstallScript(string version)
		{
			Directory.CreateDirectory(DOTNET_LIBRARY_PATH);

			using (var process = new System.Diagnostics.Process())
			{
#if UNITY_EDITOR && !UNITY_EDITOR_WIN
				var command = $"{DotnetInstallScriptPath} --install-dir {DOTNET_LIBRARY_PATH} --no-path --version {version}";
				process.StartInfo.FileName = "sh";
				process.StartInfo.Arguments = $"-c '{command}'";
#else
				process.StartInfo.FileName = "powershell.exe";
				process.StartInfo.Arguments = "-ExecutionPolicy Bypass -File \"" + DotnetInstallScriptPath + $"\" -InstallDir \"{DOTNET_LIBRARY_PATH}\" -NoPath -Version {version}"; //  "/C " + command + " > " + commandoutputfile + "'"; // TODO: I haven't tested this since refactor.
#endif
				// Configure the process using the StartInfo properties.
				process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
				process.EnableRaisingEvents = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;

				process.ErrorDataReceived += (sender, data) =>
				{
					if (data == null || string.IsNullOrEmpty(data.Data)) return;
					Debug.Log("DOTNET ERROR INSTALL: " + data.Data);
				};


				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();
				return process.ExitCode == 0;
			}
		}
	}
}
