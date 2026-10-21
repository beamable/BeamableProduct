using Beamable.Server;
using cli.Services.LocalStack;
using Spectre.Console;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Commands.LocalStack;

public class LocalStackPsCommandArgs : CommandArgs
{
	public string configPath;
	public bool watch;
}

public class LocalStackPsEntry
{
	public string name;
	public string group;
	public int pid;
	public string kind;
	public bool running;
	public string logPath;

	/// <summary>The step's readable name, as the board shows it.</summary>
	public string label;

	/// <summary>Its row on the board (see <see cref="LocalStackStepState"/>); null for a stack started before the board.</summary>
	public string state;

	public int elapsedSeconds;
	public int port;
	public string detail;
}

public class LocalStackPsCommandResult
{
	/// <summary>The stack that is running, when <c>beam local run</c> recorded its board.</summary>
	public string stack;

	/// <summary>True when the stack belongs to an attached <c>beam local run</c> that is still in its terminal.</summary>
	public bool attached;

	public string host;
	public string portalUrl;
	public bool backendHealthy;
	public List<LocalStackPsEntry> steps = new List<LocalStackPsEntry>();
}

/// <summary>
/// Reports the status of the local stack started by <c>beam local run</c>, as the same board the run shows: each
/// step with its state, time and port. A detached step that was up and whose process is gone shows as stopped.
/// Also probes the backend's <c>${host}/metadata</c> (PR#632). <c>--watch</c> re-renders on an interval until
/// cancelled. A stack started by a CLI from before the board gets the older table of pids.
/// </summary>
public class LocalStackPsCommand
	: AtomicCommand<LocalStackPsCommandArgs, LocalStackPsCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public LocalStackPsCommand() : base("ps", "Show the status of the running local stack")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--config", "Path to the manifest whose run-state to read (defaults to .beamable/local/stack-settings.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<bool>("--watch", "Continuously re-render the status until cancelled"),
			(args, v) => args.watch = v);
	}

	public override async Task<LocalStackPsCommandResult> GetResult(LocalStackPsCommandArgs args)
	{
		var runStatePath = LocalStackCommand.ResolveRunStatePath(args.ConfigService, args.configPath);
		var token = args.Lifecycle.CancellationToken;

		LocalStackPsCommandResult result;
		do
		{
			result = await Snapshot(runStatePath);
			Render(result, runStatePath, args.watch);

			if (!args.watch) break;
			try { await Task.Delay(2000, token); }
			catch (OperationCanceledException) { break; }
		} while (!token.IsCancellationRequested);

		return result;
	}

	private static async Task<LocalStackPsCommandResult> Snapshot(string runStatePath)
	{
		var result = new LocalStackPsCommandResult();
		var state = LocalStackRunStateIO.Load(runStatePath);
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		if (board != null && !LocalStackBoardIO.IsStale(board))
		{
			Merge(result, board, state);
			if (!string.IsNullOrWhiteSpace(result.host))
				result.backendHealthy = await ProbeMetadata(result.host);
			return result;
		}

		if (state == null) return result;

		result.host = state.host;
		result.portalUrl = state.portalUrl;

		foreach (var s in state.steps)
		{
			// The recorded pid works once `up` has resolved it to the service leaf, but it must be identity-checked:
			// a recycled pid otherwise reports a service as running when it is long gone (see LocalStackLiveness).
			// For stacks brought up by an older CLI (which recorded the Windows wrapper pid that has since died),
			// fall back to matching the service on a live JVM's command line so it isn't misreported as "stopped".
			var tokens = LocalStackStopCommand.BuildKillTokens(s);
			var running = LocalStackLiveness.IsEntryRunning(s, tokens);
			if (!running && !s.waitForExit)
				running = LocalStackProcess.FindByCommandLine(tokens, LocalStackProcess.ServiceImages).Count > 0;

			result.steps.Add(new LocalStackPsEntry
			{
				name = s.name,
				group = s.group,
				pid = s.pid,
				kind = s.kind,
				running = running,
				logPath = s.stdoutLog,
			});
		}

		if (!string.IsNullOrWhiteSpace(state.host))
			result.backendHealthy = await ProbeMetadata(state.host);

		return result;
	}


	/// <summary>
	/// The board's rows, with what the run-state knows on top: a detached step the board shows as up whose process
	/// is gone is <see cref="LocalStackStepState.Stopped"/>, since nobody is left to update the board for it.
	/// </summary>
	public static void Merge(LocalStackPsCommandResult result, LocalStackBoardSnapshot board, LocalStackRunState state,
		Func<LocalStackRunEntry, bool> isRunning = null)
	{
		isRunning ??= entry =>
		{
			var tokens = LocalStackStopCommand.BuildKillTokens(entry);
			return LocalStackLiveness.IsEntryRunning(entry, tokens)
			       || (!entry.waitForExit && LocalStackProcess.FindByCommandLine(tokens, LocalStackProcess.ServiceImages).Count > 0);
		};

		result.stack = board.stack;
		result.attached = board.attached;
		result.host = board.host;
		result.portalUrl = board.portalUrl;
		var now = DateTime.UtcNow;
		foreach (var row in board.rows)
		{
			var entry = state?.steps.FirstOrDefault(e => string.Equals(e.name, row.name, StringComparison.OrdinalIgnoreCase));
			var up = row.state is LocalStackStepState.Ready or LocalStackStepState.Running or LocalStackStepState.Degraded;
			var running = entry != null ? isRunning(entry) : up;
			if (!board.attached && up && entry != null && !entry.adopted && !running)
			{
				row.state = LocalStackStepState.Stopped;
				row.detail = "its process is gone";
				row.changedUtc = now;
			}

			result.steps.Add(new LocalStackPsEntry
			{
				name = row.name, label = row.label, state = row.state.ToString(), elapsedSeconds = row.ElapsedSeconds(now),
				port = row.port, detail = row.detail, running = running,
				group = entry?.group, pid = entry?.pid ?? 0, kind = entry?.kind, logPath = entry?.stdoutLog
			});
		}
	}

	private static async Task<bool> ProbeMetadata(string host)
	{
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
			using var res = await http.GetAsync($"{host.TrimEnd('/')}/metadata", HttpCompletionOption.ResponseHeadersRead);
			return (int)res.StatusCode == 200;
		}
		catch
		{
			return false;
		}
	}

	private static void Render(LocalStackPsCommandResult result, string runStatePath, bool watch)
	{
		if (watch)
		{
			try { AnsiConsole.Clear(); } catch { /* non-interactive */ }
		}

		if (result.steps.Count == 0)
		{
			Log.Information($"No running local stack recorded at {runStatePath}. Start one with `beam local run <stack>`.");
			return;
		}

		var backendLine = $"backend={Markup.Escape(result.host ?? "?")} ({(result.backendHealthy ? "[green]healthy[/]" : "[yellow]no /metadata response[/]")})  portal={Markup.Escape(result.portalUrl ?? "?")}";
		if (result.stack != null)
		{
			// The same board `beam local run` shows, as a snapshot: every row, however tall.
			var snapshot = new LocalStackBoardSnapshot
			{
				stack = result.stack,
				rows = result.steps.Select(s => new LocalStackBoardRow
				{
					name = s.name, label = s.label, port = s.port, detail = s.detail,
					state = Enum.TryParse<LocalStackStepState>(s.state, out var parsed) ? parsed : LocalStackStepState.Waiting,
					startedUtc = DateTime.UtcNow.AddSeconds(-s.elapsedSeconds), changedUtc = DateTime.UtcNow
				}).ToList()
			};
			AnsiConsole.Write(LocalStackBoard.Render(snapshot, AnsiConsole.Profile.Width, DateTime.UtcNow));
			AnsiConsole.MarkupLine(backendLine + (result.attached ? "  [grey](attached to its terminal)[/]" : string.Empty));
			return;
		}

		var table = new Table();
		table.Border(TableBorder.Simple);
		table.AddColumn("[bold]step[/]");
		table.AddColumn("[bold]group[/]");
		table.AddColumn("[bold]kind[/]");
		table.AddColumn("[bold]pid[/]");
		table.AddColumn("[bold]status[/]");

		foreach (var s in result.steps)
		{
			var status = s.kind == "docker"
				? "[grey]docker (see docker ps)[/]"
				: s.running ? "[green]running[/]" : "[red]stopped[/]";
			table.AddRow(
				new Markup(Markup.Escape(s.name)),
				new Markup(Markup.Escape(s.group ?? "")),
				new Markup(Markup.Escape(s.kind ?? "")),
				new Markup(s.pid > 0 ? s.pid.ToString() : "-"),
				new Markup(status));
		}

		AnsiConsole.Write(table);
		AnsiConsole.MarkupLine(backendLine);
	}
}
