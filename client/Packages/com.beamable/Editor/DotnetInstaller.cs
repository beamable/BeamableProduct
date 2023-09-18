using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Editor.Environment;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor
{
	public class DotnetUtil
	{
		const string WIN_DOWNLOAD_URL =
			"https://dot.net/v1/dotnet-install.ps1";

		const string MAC_DOWNLOAD_URL =
			"https://dot.net/v1/dotnet-install.sh";
		
		
		const string WIN_SCRIPT_NAME =
			"dotnet-install.ps1";

		const string MAC_SCRIPT_NAME =
			"dotnet-install.sh";

#if UNITY_EDITOR_WIN
		public const string DOWNLOAD_URL = WIN_DOWNLOAD_URL;
		public const string SCRIPT_NAME = WIN_SCRIPT_NAME;
#elif  UNITY_EDITOR_OSX
		public const string DOWNLOAD_URL = MAC_DOWNLOAD_URL;
		public const string SCRIPT_NAME = MAC_SCRIPT_NAME;
#endif

		[MenuItem("Dotnet Test/Download Script")]
		public static async void DownloadInstaller()
		{
			var ctx = BeamEditorContext.Default;
			await ctx.InitializePromise;

			var service = ctx.ServiceScope.GetService<DotnetContext>();
			await service.DownloadInstallScript();
			
			service.RunInstallScript();
		}

		[MenuItem("Dotnet Test/Download CLI")]
		public static async void InstallCLI()
		{
			var ctx = BeamEditorContext.Default;
			await ctx.InitializePromise;

			var service = ctx.ServiceScope.GetService<BeamCliInstaller>();
			service.InstallCli();
		}
		
		
		[MenuItem("Dotnet Test/Check CLI")]
		public static async void RunCLIVersion()
		{
			var ctx = BeamEditorContext.Default;
			await ctx.InitializePromise;

			var service = ctx.ServiceScope.GetService<BeamCliInstaller>();
			service.InstallCli();
		}

	}

	public class BeamCliInstaller
	{
		private readonly DotnetContext _dotnet;
		private readonly IBeamableFilesystemAccessor _fs;
		private readonly EnvironmentData _env;

		public BeamCliInstaller(DotnetContext dotnet, IBeamableFilesystemAccessor fs, EnvironmentData env)
		{
			_dotnet = dotnet;
			_fs = fs;
			_env = env;
		}
		
		public string InstallLocation => Path.Combine("beamCli", _env.SdkVersion.ToString());

		public void InstallCli()
		{
			if (!_dotnet.Run($"tool install beamable.tools --tool-path {InstallLocation}"))
			{
				Debug.LogError("Failed to install CLI");
			}
		}
	}
	
	public class DotnetContext
	{
		private readonly IEditorHttpRequester _requester;
		private readonly IBeamableFilesystemAccessor _fs;

		public DotnetContext(IEditorHttpRequester requester, IBeamableFilesystemAccessor fs)
		{
			_requester = requester;
			_fs = fs;
		}
		
		public string InstallScriptPath =>
			Path.Combine(_fs.GetPersistentDataPathWithoutTrailingSlash(), "DotnetTools", DotnetUtil.SCRIPT_NAME);

		public string InstallLocation => Path.Combine(_fs.GetPersistentDataPathWithoutTrailingSlash(), "DotnetTools", "install");

		public string DotnetPath => Path.Combine(InstallLocation, "dotnet");
		
		public bool Run(string dotnetCommand)
		{
			// var relativePath = Path.GetRelativePath(_fs.GetPersistentDataPathWithoutTrailingSlash(), DotnetPath);
			var relativePath = Path.Combine("DotnetTools", "install", "dotnet");
			var command = $"{relativePath} {dotnetCommand}";
			using (var process = new System.Diagnostics.Process())
			{
#if UNITY_EDITOR && !UNITY_EDITOR_WIN
				process.StartInfo.WorkingDirectory = _fs.GetPersistentDataPathWithoutTrailingSlash();
				process.StartInfo.FileName = "sh";
				process.StartInfo.Arguments = $"-c '{command}'";
#else
					_process.StartInfo.FileName = "cmd.exe";
					_process.StartInfo.Arguments = $"/C {command}";
																																																																																																									                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           $"/C {command}"; //  "/C " + command + " > " + commandoutputfile + "'"; // TODO: I haven't tested this since refactor.
#endif
				// Configure the process using the StartInfo properties.
				process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
				process.EnableRaisingEvents = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;

				process.OutputDataReceived += (sender, data) =>
				{
					Debug.Log("DOTNET LOG: " + data.Data);
				};
				process.ErrorDataReceived += (sender, data) =>
				{
					Debug.Log("DOTNET ERROR: " + data.Data);
				};

				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				process.WaitForExit();
				return process.ExitCode == 0;
			}
		}
		
		
		public async Promise DownloadInstallScript()
		{
			var url = DotnetUtil.DOWNLOAD_URL;
			var script = await _requester.ManualRequest(Method.GET, url, parser: s => s);

			var directory = Directory.GetParent(InstallScriptPath);
			directory.Create();
			
			await File.WriteAllTextAsync(InstallScriptPath, script);
			
			if (!Chmod(InstallScriptPath, "+x"))
			{
				Debug.LogError("Could not make file writable");
			}
		}
		
		// Returns true if success and false otherwise
		// permissions can be an int or a string. For example it can also be +x, -x etc..
		bool Chmod(string filePath, string permissions = "700", bool recursive = false)
		{
#if UNITY_EDITOR_WIN
			return true;
#endif
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
		}

		public bool RunInstallScript()
		{

			Directory.CreateDirectory(InstallLocation);
			// --install-dir ./here 
			var command = $"{InstallScriptPath} --install-dir {InstallLocation} --no-path";
			using (var process = new System.Diagnostics.Process())
			{
#if UNITY_EDITOR && !UNITY_EDITOR_WIN
				process.StartInfo.FileName = "sh";
				process.StartInfo.Arguments = $"-c '{command}'";
#else
					_process.StartInfo.FileName = "cmd.exe";
					_process.StartInfo.Arguments = $"/C {command}";
																																																																																																									                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           $"/C {command}"; //  "/C " + command + " > " + commandoutputfile + "'"; // TODO: I haven't tested this since refactor.
#endif
				// Configure the process using the StartInfo properties.
				process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
				process.EnableRaisingEvents = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;

				process.OutputDataReceived += (sender, data) =>
				{
					Debug.Log("DOTNET LOG INSTALL: " + data.Data);
				};
				process.ErrorDataReceived += (sender, data) =>
				{
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
