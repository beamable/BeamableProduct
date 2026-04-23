using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Services;
using Newtonsoft.Json;

namespace cli.Mcp;

public class McpToolExecutor
{
	private readonly string _cid;
	private readonly string _pid;

	// Serialize all in-process beam calls so Console.Out redirection and MSBuildLocator don't race.
	private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

	public McpToolExecutor(string cid, string pid)
	{
		_cid = cid;
		_pid = pid;
	}

	public Task<string> ExecuteHelpAsync(string commandPath)
	{
		var helpCommand = string.IsNullOrWhiteSpace(commandPath)
			? "--help"
			: $"{commandPath.Trim()} --help";
		return ExecuteAsync(helpCommand);
	}

	public async Task<string> ExecuteAsync(string commandLine)
	{
		await _lock.WaitAsync();
		try
		{
			return await RunInProcessAsync(commandLine);
		}
		finally
		{
			_lock.Release();
		}
	}

	private async Task<string> RunInProcessAsync(string commandLine)
	{
		var fullCommand = AppendRealmContext(commandLine);

		var sw = new StringWriter();
		var capturer = new CapturingReporterService(sw);

		var previousOut = Console.Out;
		Console.SetOut(sw);
		try
		{
			var app = new App();
			app.Configure(
				builder =>
				{
					builder.Remove<IDataReporterService>();
					builder.AddSingleton<IDataReporterService>(capturer);
				},
				overwriteLogger: false);
			app.Build();

			await app.RunWithSingleString(fullCommand, useCustomSplitter: false);
		}
		catch (Exception ex)
		{
			sw.WriteLine($"{{\"error\":\"{ex.Message.Replace("\"", "\\\"")}\"}}");
		}
		finally
		{
			Console.SetOut(previousOut);
		}

		return sw.ToString();
	}

	private string AppendRealmContext(string commandLine)
	{
		if (string.IsNullOrWhiteSpace(_cid) && string.IsNullOrWhiteSpace(_pid))
			return commandLine;

		// Don't double-append if the caller already specified them.
		var lower = commandLine.ToLowerInvariant();
		var extra = string.Empty;
		if (!string.IsNullOrWhiteSpace(_cid) && !lower.Contains("--cid"))
			extra += $" --cid {_cid}";
		if (!string.IsNullOrWhiteSpace(_pid) && !lower.Contains("--pid"))
			extra += $" --pid {_pid}";

		return commandLine + extra;
	}

	private sealed class CapturingReporterService : IDataReporterService
	{
		private readonly TextWriter _writer;

		public CapturingReporterService(TextWriter writer)
		{
			_writer = writer;
		}

		public void Report<T>(string type, T data)
		{
			var pt = new ReportDataPoint<T>
			{
				data = data,
				type = type,
				ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			};
			_writer.WriteLine(JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance));
		}
	}
}
