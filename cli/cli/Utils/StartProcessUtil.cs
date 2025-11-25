using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace cli.Utils;

public class StartProcessResult
{
	public int exit;
	public string stdout;
	public string stderr;
}

public static class StartProcessUtil
{
	public static StartProcessResult Run(string fileName, string args, string workingDirectoryPath = null)
	{
		var psi = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = args,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		if (!string.IsNullOrEmpty(workingDirectoryPath))
		{
			psi.WorkingDirectory = workingDirectoryPath;
		}

		using var p = Process.Start(psi)!;
		var stdout = new StringBuilder();
		var stderr = new StringBuilder();
		p.OutputDataReceived += (_, e) =>
		{
			if (e.Data != null) stdout.AppendLine(e.Data);
		};
		p.ErrorDataReceived += (_, e) =>
		{
			if (e.Data != null) stderr.AppendLine(e.Data);
		};
		p.BeginOutputReadLine();
		p.BeginErrorReadLine();
		p.WaitForExit();

		return new StartProcessResult() { exit = p.ExitCode, stdout = stdout.ToString(), stderr = stderr.ToString() };
	}
}
