using Beamable.Server;
using cli.Services.LocalStack;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackLogsCommandArgs : CommandArgs
{
	public string configPath;
	public string step;
	public bool follow;
	public int lines = 200;
}

public class LocalStackLogsCommandResult
{
	public string[] steps;
}

/// <summary>
/// Tails the per-step log files written by <c>beam local up</c>. With no step name, tails every recorded
/// step; <c>--follow</c> keeps streaming new lines until cancelled; <c>--lines</c> sets how much history to show.
/// </summary>
public class LocalStackLogsCommand
	: AtomicCommand<LocalStackLogsCommandArgs, LocalStackLogsCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public LocalStackLogsCommand() : base("logs", "Tail the log files of the running local stack")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("step", () => null, "The step name to tail (default: all recorded steps)"),
			(args, v) => args.step = v);
		AddOption(new Option<string>("--config", "Path to the manifest whose run-state to read (defaults to .beamable/local-stack.json)"),
			(args, v) => args.configPath = v);
		var follow = new Option<bool>("--follow", "Keep streaming new log lines until cancelled");
		follow.AddAlias("-f");
		AddOption(follow, (args, v) => args.follow = v);
		var lines = new Option<int>("--lines", () => 200, "How many trailing lines of history to show first");
		lines.AddAlias("-n");
		AddOption(lines, (args, v) => args.lines = v);
	}

	public override async Task<LocalStackLogsCommandResult> GetResult(LocalStackLogsCommandArgs args)
	{
		var runStatePath = LocalStackCommand.ResolveRunStatePath(args.ConfigService, args.configPath);
		var state = LocalStackRunStateIO.Load(runStatePath);
		if (state == null || state.steps.Count == 0)
			throw new CliException($"No running local stack recorded at {runStatePath}. Run `beam local up` first.");

		var selected = string.IsNullOrWhiteSpace(args.step)
			? state.steps
			: state.steps.Where(s => string.Equals(s.name, args.step, StringComparison.OrdinalIgnoreCase)).ToList();

		if (selected.Count == 0)
			throw new CliException($"No recorded step named '{args.step}'. Known steps: {string.Join(", ", state.steps.Select(s => s.name))}");

		var token = args.Lifecycle.CancellationToken;

		// Each selected step contributes its stdout and stderr files.
		var files = selected
			.SelectMany(s => new[] { (s.name, path: s.stdoutLog, isErr: false), (s.name, path: s.stderrLog, isErr: true) })
			.Where(f => !string.IsNullOrEmpty(f.path) && File.Exists(f.path))
			.ToList();

		if (!args.follow)
		{
			foreach (var (name, path, isErr) in files)
				DumpTail(name, path, isErr, args.lines);
			return new LocalStackLogsCommandResult { steps = selected.Select(s => s.name).ToArray() };
		}

		var tasks = files.Select(f => FollowFile(f.name, f.path, f.isErr, args.lines, token)).ToList();
		try { await Task.WhenAll(tasks); }
		catch (OperationCanceledException) { /* expected on Ctrl+C */ }

		return new LocalStackLogsCommandResult { steps = selected.Select(s => s.name).ToArray() };
	}

	private static void DumpTail(string name, string path, bool isErr, int lines)
	{
		try
		{
			using var tailer = new LineTailer(path, lines);
			foreach (var line in tailer.ReadAvailableLines())
				Emit(name, line, isErr);
		}
		catch (Exception e)
		{
			Log.Warning($"[{name}] could not read {path}: {e.Message}");
		}
	}

	private static async Task FollowFile(string name, string path, bool isErr, int lines, CancellationToken token)
	{
		try
		{
			using var tailer = new LineTailer(path, lines);
			while (!token.IsCancellationRequested)
			{
				foreach (var line in tailer.ReadAvailableLines())
					Emit(name, line, isErr);
				await Task.Delay(400, token);
			}
		}
		catch (OperationCanceledException) { /* expected */ }
		catch (Exception e) { Log.Verbose($"[{name}] tail stopped: {e.Message}"); }
	}

	private static void Emit(string name, string line, bool isErr)
	{
		// A tail command's primary output IS the log content, so write it straight to stdout rather than through
		// the structured Log pipeline (which routes to the log file / logs channel and wouldn't be shown here).
		var marker = isErr ? "!" : " ";
		Console.Out.WriteLine($"[{name}]{marker}{line}");
	}
}
