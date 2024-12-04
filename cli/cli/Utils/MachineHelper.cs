using Beamable.Common;
using System.Diagnostics;
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
		if (Environment.OSVersion.Platform != PlatformID.Win32NT)
		{
			BeamableLogger.LogWarning("Auto generation of Unreal project files is not yet supported on this platform. Remember to generate the Unreal project files manually.");
			return;
		}

		// Get the name of the uproject
		var cmd = @"$uproject = Get-ChildItem ""*.uproject"" -Name;";
		// Get the path to the UnrealEngine version for this project (this is stored here as-per UE -- https://forums.unrealengine.com/t/generate-vs-project-files-by-command-line/277707/18).
		cmd += @"$bin = & { (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\Unreal.ProjectFile\shell\rungenproj' -Name 'Icon' ).'Icon' };";
		// Build the actual command to run at this path and pipe it into the cmd.exe.
		cmd += @"$bin + ' -projectfiles %cd%\' + $uproject | cmd.exe";

		var result = ExecutePowershellCommand(cmd, unrealRoot);
		if (result.ExitCode != 0)
		{
			BeamableLogger.LogWarning("Auto generation of Unreal project files failed but it is still possible to generate the Unreal project files manually.");
			throw new CliException($"Failed to generate project files. Command output:\n{result.Error}\n{result.StandardOut}");
		}
	}

	public static PowershellOutput ExecutePowershellCommand(string command, string directory)
	{
		var processStartInfo = new ProcessStartInfo();
		processStartInfo.FileName = "powershell.exe";
		processStartInfo.Arguments = $"-Command \"{command}\"";
		processStartInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
		processStartInfo.WorkingDirectory = directory;
		processStartInfo.UseShellExecute = false;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.RedirectStandardError = true;

		using var process = new Process();
		process.StartInfo = processStartInfo;
		process.Start();
		process.WaitForExit();
		return new PowershellOutput
		{
			ExitCode = process.ExitCode,
			StandardOut = process.StandardOutput.ReadToEnd(),
			Error = process.StandardError.ReadToEnd()
		};
	}

	[Serializable]
	public struct PowershellOutput
	{
		public string StandardOut;
		public string Error;
		public int ExitCode;
	}
}
