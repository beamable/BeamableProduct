using Beamable.Common;
using Beamable.Common.Api;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace cli.Utils;

public class MachineHelper
{
	public static void OpenBrowser(string url)
	{
		try
		{
			Process.Start(url);
		}
		catch
		{
			// hack because of this: https://github.com/dotnet/corefx/issues/10361
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				url = url.Replace("&", "^&");
				Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process.Start("xdg-open", url);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				Process.Start("open", url);
			}
			else
			{
				throw;
			}
		}
	}

	/// <summary>
	/// Call to regenerate project files. Currently only works on windows.
	/// TODO: Add support for MacOS/Linux
	/// </summary>
	/// <param name="unrealRoot"></param>
	public static void RunUnrealGenerateProjectFiles(string unrealRoot)
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			// go into the unreal root
			var cmd = $"cd {unrealRoot};";
			// Get the name of the uproject
			cmd += @"$uproject = Get-ChildItem ""*.uproject"" -Name;";
			// Get the path to the UnrealEngine version for this project (this is stored here as-per UE -- https://forums.unrealengine.com/t/generate-vs-project-files-by-command-line/277707/18).
			cmd += @"$bin = & { (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\Unreal.ProjectFile\shell\rungenproj' -Name 'Icon' ).'Icon' };";
			// Build the actual command to run at this path and pipe it into the cmd.exe.
			cmd += @"$bin + ' -projectfiles ""' + $pwd + '\' + $uproject + '""' | cmd.exe";

			// Run the command and print the result
			var _ = ExecutePowershellCommand(cmd);
		}
	}

	public static string ExecutePowershellCommand(string command)
	{
		var processStartInfo = new ProcessStartInfo();
		processStartInfo.FileName = "powershell.exe";
		processStartInfo.Arguments = $"-Command \"{command}\"";
		processStartInfo.UseShellExecute = false;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.RedirectStandardError = true;

		using var process = new Process();
		process.StartInfo = processStartInfo;
		process.Start();
		string output = process.StandardOutput.ReadToEnd();
		string error = process.StandardError.ReadToEnd(); 
		process.WaitForExit();
		if (string.IsNullOrEmpty(error))
		{
			BeamableLogger.LogError(error);
		}
		return output;
	}
}
