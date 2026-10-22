using Beamable.Server;
using cli.Services;
using Newtonsoft.Json;
using cli.Services.LocalStack;
using Spectre.Console;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Commands.LocalStack;

public class LocalStackPsCommandArgs : CommandArgs
{
	public string configPath;
	public bool watch;
	public bool json;

	/// <summary>Steps to restart or start, by name, label or tag, comma separated; empty when the option came without
	/// a value (then what can be named is listed). Null when the option wasn't given.</summary>
	public string restart;
	public string start;

	/// <summary>With <see cref="restart"/> or <see cref="start"/>: every service.</summary>
	public bool all;
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

	/// <summary>True for a step that runs once (a build, <c>docker compose up</c>): it isn't restarted on its own.</summary>
	public bool once;
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

	/// <summary>True once the stack has come up.</summary>
	public bool up;

	/// <summary>With <c>--restart</c> or <c>--start</c>: the steps acted on, and what was said about it.</summary>
	public string[] acted;
	public string message;

	/// <summary>With <c>--restart</c> or <c>--start</c> and nothing named: what can be named, by tag.</summary>
	public List<LocalStackRestartChoice> choices;
	public List<LocalStackPsEntry> steps = new List<LocalStackPsEntry>();
}

/// <summary>
/// Reports the status of the local stack started by <c>beam local run</c>, as the same board the run shows: each
/// step with its state, time and port. A detached step that was up and whose process is gone shows as stopped.
/// Also probes the backend's <c>${host}/metadata</c> (PR#632). <c>--watch</c> keeps the board up in a terminal,
/// where the same keys as an attached run's board restart, stop or start the selected service; without a terminal
/// it re-renders on an interval. <c>--json</c> prints the status as JSON instead.
///
/// <c>--restart &lt;steps&gt;</c> and <c>--start &lt;steps&gt;</c> act on the running stack without the board, for scripts
/// and agents: steps by name, label or tag, or <c>--all</c> for every service. A restart runs the step's build check
/// first, so restarting a C# host after a code change rebuilds it. An attached stack's services live in its
/// terminal's process, so that process is asked to do it (see <see cref="LocalStackControl"/>); a detached stack's
/// steps are stopped here and the stack is run again detached, which starts what isn't running. With nothing named,
/// they list what can be named, grouped by tag (a picker in a terminal).
/// </summary>
public class LocalStackPsCommand
	: AtomicCommand<LocalStackPsCommandArgs, LocalStackPsCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	// It prints its own readable output; the result object still reaches tools through the data channel.
	public override bool AutoLogOutput => false;

	public LocalStackPsCommand() : base("ps", "Show the status of the running local stack")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--config", "Path to the manifest whose run-state to read (defaults to .beamable/local/stack-settings.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<bool>("--watch", "Keep the board up until cancelled; in a terminal its keys restart, stop or start the selected service"),
			(args, v) => args.watch = v);
		AddOption(new Option<bool>("--json", "Print the status as JSON instead of the board"),
			(args, v) => args.json = v);

		var restart = new Option<string>("--restart", "Restart steps of the running stack, by name, label or tag, comma separated; without a value, list them")
			{ Arity = ArgumentArity.ZeroOrOne };
		AddOption(restart, (args, ctx, v) =>
		{
			if (ctx.ParseResult.FindResultFor(restart) is { IsImplicit: false }) args.restart = v ?? string.Empty;
		});
		var start = new Option<string>("--start", "Start steps of the running stack that aren't running, by name, label or tag; without a value, list them")
			{ Arity = ArgumentArity.ZeroOrOne };
		AddOption(start, (args, ctx, v) =>
		{
			if (ctx.ParseResult.FindResultFor(start) is { IsImplicit: false }) args.start = v ?? string.Empty;
		});
		AddOption(new Option<bool>("--all", "With --restart or --start: every service of the running stack (docker containers stay up)"),
			(args, v) => args.all = v);
	}

	public override async Task<LocalStackPsCommandResult> GetResult(LocalStackPsCommandArgs args)
	{
		var runStatePath = LocalStackCommand.ResolveRunStatePath(args.ConfigService, args.configPath);
		var token = args.Lifecycle.CancellationToken;

		if (args.restart != null || args.start != null)
			return await RestartOrStart(args, runStatePath);

		// In a terminal the board stays up with its keys, as an attached run's does; piped or with --json, one snapshot.
		var capabilities = AnsiConsole.Profile.Capabilities;
		if (!args.json && !args.Quiet && capabilities.Interactive && capabilities.Ansi && capabilities.AlternateBuffer)
			return await WatchLive(args, runStatePath, token);

		LocalStackPsCommandResult result;
		do
		{
			result = await Snapshot(runStatePath);
			if (args.json)
				Console.Out.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
			else
				Render(result, runStatePath, args.watch);

			if (!args.watch) break;
			try { await Task.Delay(2000, token); }
			catch (OperationCanceledException) { break; }
		} while (!token.IsCancellationRequested);

		return result;
	}

	/// <summary><c>--restart</c> or <c>--start</c>: what they name, every service with <c>--all</c>, or the list.</summary>
	private static async Task<LocalStackPsCommandResult> RestartOrStart(LocalStackPsCommandArgs args, string runStatePath)
	{
		if (args.restart != null && args.start != null)
			throw new CliException("Pass --restart or --start, not both.");

		var action = args.restart != null ? "restart" : "start";
		var items = Items(args.restart ?? args.start);
		var all = args.all;
		if (items.Count == 0 && !all)
		{
			var running = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
			if (running == null || LocalStackBoardIO.IsStale(running))
				throw new CliException("No stack is running here. Start one with `beam local run <stack>`.");

			var (steps, tagsOf) = RunningDefinition(args, running);
			if (args.Quiet || !AnsiConsole.Profile.Capabilities.Interactive)
			{
				var choices = Choices(running.rows, steps, tagsOf, action == "start", firstTagOnly: false);
				PrintChoices(action, choices);
				return new LocalStackPsCommandResult { acted = Array.Empty<string>(), choices = choices };
			}

			if (!Pick(action, Choices(running.rows, steps, tagsOf, action == "start", firstTagOnly: true), out items, out all))
				return new LocalStackPsCommandResult { acted = Array.Empty<string>(), message = $"Nothing to {action}." };
		}

		var (acted, message) = await Act(args, runStatePath, action, items, all, args.Quiet);
		return new LocalStackPsCommandResult { acted = acted, message = message };
	}

	/// <summary>
	/// The live board in a terminal: the board file read again every half second, and the keys
	/// acting on the stack as <c>beam local restart</c>, <c>start</c> and <c>stop</c> would, whether it runs attached to
	/// another terminal or detached. Ends on Ctrl+C, which leaves the stack running, or when the stack is gone.
	/// </summary>
	private static async Task<LocalStackPsCommandResult> WatchLive(LocalStackPsCommandArgs args, string runStatePath, CancellationToken token)
	{
		var result = await Snapshot(runStatePath, probe: false);
		if (result.stack == null)
		{
			Render(result, runStatePath, watch: false);
			return result;
		}

		var board = new LocalStackBoard(ToBoard(result), LocalStackBoardMode.Live, file: null);
		var live = board.ShowLive(token, (action, step) => Act(args, runStatePath, action, step),
			"Up/Down select, Space acts on it, PgUp/PgDn/Home/End, F follows, Ctrl+C leaves (the stack keeps running)");
		while (!live.IsCompleted)
		{
			try { await Task.Delay(500, token); }
			catch (OperationCanceledException) { break; }

			result = await Snapshot(runStatePath, probe: false);
			if (result.stack == null)
			{
				board.Complete();
				break;
			}

			board.Load(ToBoard(result));
		}

		await live;
		if (result.stack == null)
			Log.Information("The stack isn't running any more.");
		return result;
	}

	/// <summary>A key on the watched board: what <c>beam local restart</c>, <c>start</c> or <c>stop</c> of that step does.</summary>
	private static async Task<string> Act(CommandArgs args, string runStatePath, string action, string step)
	{
		var items = new List<string> { step };
		if (action != "stop")
			return (await Act(args, runStatePath, action, items, all: false, quiet: true)).message;

		var answer = await AskAttached(runStatePath, "stop", items, args.Lifecycle.CancellationToken);
		if (answer != null) return answer.message;

		var state = LocalStackRunStateIO.Load(runStatePath);
		if (state == null) return "No stack is running here.";
		var (stopped, _) = LocalStackStopCommand.Stop(runStatePath, state, e => string.Equals(e.name, step, StringComparison.OrdinalIgnoreCase), purge: false);
		return stopped.Count == 0 ? $"{step} wasn't running." : $"Stopped {string.Join(", ", stopped)}.";
	}

	/// <summary>The status as the board shows it.</summary>
	private static LocalStackBoardSnapshot ToBoard(LocalStackPsCommandResult result) => new LocalStackBoardSnapshot
	{
		stack = result.stack, attached = result.attached, host = result.host, portalUrl = result.portalUrl, up = result.up,
		rows = result.steps.Select(s => new LocalStackBoardRow
		{
			name = s.name, label = s.label, port = s.port, detail = s.detail, once = s.once,
			state = Enum.TryParse<LocalStackStepState>(s.state, out var parsed) ? parsed : LocalStackStepState.Waiting,
			startedUtc = DateTime.UtcNow.AddSeconds(-s.elapsedSeconds), changedUtc = DateTime.UtcNow
		}).ToList()
	};

	private static async Task<LocalStackPsCommandResult> Snapshot(string runStatePath, bool probe = true)
	{
		var result = new LocalStackPsCommandResult();
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		if (board == null || LocalStackBoardIO.IsStale(board)) return result;

		Merge(result, board, LocalStackRunStateIO.Load(runStatePath));
		if (probe && !string.IsNullOrWhiteSpace(result.host))
			result.backendHealthy = await ProbeMetadata(result.host);
		return result;
	}

	/// <summary>
	/// The board's rows, with what the run-state knows on top: a detached step the board shows as up whose process
	/// is gone is <see cref="LocalStackStepState.Stopped"/>, since nobody is left to update the board for it.
	/// </summary>
	public static void Merge(LocalStackPsCommandResult result, LocalStackBoardSnapshot board, LocalStackRunState state,
		Func<LocalStackRunEntry, bool> isRunning = null)
	{
		isRunning ??= entry => LocalStackLiveness.IsEntryRunning(entry, LocalStackStopCommand.BuildKillTokens(entry));

		result.stack = board.stack;
		result.attached = board.attached;
		result.up = board.up;
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
				port = row.port, detail = row.detail, running = running, once = row.once,
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
		// The same board `beam local run` shows, as a snapshot: every row, however tall.
		AnsiConsole.Write(LocalStackBoard.Render(ToBoard(result), AnsiConsole.Profile.Width, DateTime.UtcNow));
		AnsiConsole.MarkupLine(backendLine + (result.attached ? "  [grey](attached to its terminal)[/]" : string.Empty));
	}

	/// <summary>
	/// Does <paramref name="action"/> (<c>restart</c> or <c>start</c>) to the steps <paramref name="items"/> name, or
	/// to every service with <paramref name="all"/>, in the running stack; the board's keys use it too. Returns the steps acted on and what to say about it.
	/// </summary>
	public static async Task<(string[] steps, string message)> Act(CommandArgs args, string runStatePath, string action, List<string> items,
		bool all, bool quiet)
	{
		var answer = await AskAttached(runStatePath, action, all ? new List<string>() : items, args.Lifecycle.CancellationToken);
		if (answer != null)
			return (answer.steps.ToArray(), answer.message);

		var state = LocalStackRunStateIO.Load(runStatePath);
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		if (state == null || board == null || LocalStackBoardIO.IsStale(board))
			throw new CliException("No stack is running here. Start one with `beam local run <stack>`.");

		// Everything: every service the run-state records (docker steps run once, and their containers stay up).
		var names = all
			? state.steps.Where(e => !e.waitForExit).Select(e => e.name).ToHashSet(StringComparer.OrdinalIgnoreCase)
			: MatchRecorded(args, runStatePath, state, items).Select(s => s.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		if (action == "restart")
			LocalStackStopCommand.Stop(runStatePath, state, e => names.Contains(e.name), purge: false);

		// Run the same stack again, detached: it starts what isn't running (the steps just stopped, with their build
		// check) and leaves the rest alone.
		var run = args.Create<LocalStackRunCommandArgs>();
		run.stack = board.stack;
		run.runDetached = true;
		run.Quiet = quiet;
		var runCommand = new LocalStackRunCommand();
		((IResultProvider)runCommand).Reporter = args.Provider.GetService<IDataReporterService>();
		await runCommand.Handle(run);

		var message = $"{(action == "restart" ? "Restarted" : "Started")} {(all && action == "start" ? "what wasn't running" : string.Join(", ", names))}.";
		Log.Information(message);
		return (names.ToArray(), message);
	}

	/// <summary>The comma separated items of a steps argument (step names contain spaces, so only commas split).</summary>
	public static List<string> Items(string value) =>
		string.IsNullOrWhiteSpace(value)
			? new List<string>()
			: value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

	/// <summary>
	/// When a stack attached to another terminal is running, asks it to do <paramref name="action"/> and returns its
	/// answer (printing what it says); null when there is no such stack. Fails when it refuses or doesn't answer.
	/// </summary>
	public static async Task<LocalStackControlResponse> AskAttached(string runStatePath, string action, List<string> items,
		CancellationToken token)
	{
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		if (board == null || !board.attached || LocalStackBoardIO.IsStale(board) || board.runnerPid == Environment.ProcessId)
			return null;

		Log.Information($"Asking the stack running in another terminal (pid {board.runnerPid}) to {action} {(items.Count == 0 ? "everything" : string.Join(", ", items))}...");
		var answer = await LocalStackControl.Ask(runStatePath, new LocalStackControlRequest { action = action, items = items },
			TimeSpan.FromMinutes(30), token);
		if (answer == null)
			throw new CliException($"The stack in the other terminal (pid {board.runnerPid}) didn't answer. Is it still starting? Try again once it's up.");
		if (!answer.ok)
			throw new CliException(answer.message);

		Log.Information(answer.message);
		return answer;
	}

	/// <summary>
	/// What can be named, by tag, in <see cref="LocalStackStacks.KnownTags"/> order, then the stack's own tags, then
	/// <c>other</c> for steps without one: the services on the board (build and run-once steps aren't restarted on
	/// their own), and for <c>start</c> only those that aren't up. With <paramref name="firstTagOnly"/> each step is
	/// listed under its first tag only, as a picker needs; otherwise under every tag it has, as the names it answers to.
	/// </summary>
	public static List<LocalStackRestartChoice> Choices(IReadOnlyList<LocalStackBoardRow> rows, IReadOnlyList<LocalStackStep> steps,
		Func<LocalStackStep, IEnumerable<string>> tagsOf, bool starting, bool firstTagOnly)
	{
		var up = new[]
		{
			LocalStackStepState.Ready, LocalStackStepState.Running, LocalStackStepState.Degraded, LocalStackStepState.Starting,
			LocalStackStepState.Restarting, LocalStackStepState.Building
		};
		var candidates = rows.Where(r => !r.once && (!starting || !up.Contains(r.state)))
			.Select(r => (row: r, step: steps.FirstOrDefault(s => string.Equals(s.name, r.name, StringComparison.OrdinalIgnoreCase))
			                           ?? new LocalStackStep { name = r.name }))
			.ToList();
		List<string> TagsOf(LocalStackStep step) => tagsOf(step).Where(t => !string.Equals(t, "build", StringComparison.OrdinalIgnoreCase)).ToList();

		var order = LocalStackStacks.KnownTags.Where(t => t != "build").ToList();
		foreach (var tag in candidates.SelectMany(c => TagsOf(c.step)))
		{
			if (!order.Contains(tag, StringComparer.OrdinalIgnoreCase)) order.Add(tag);
		}
		order.Add("other");

		var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var choices = new List<LocalStackRestartChoice>();
		foreach (var tag in order)
		{
			var members = candidates.Where(c =>
				{
					var tags = TagsOf(c.step);
					return tag == "other" ? tags.Count == 0 : tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
				})
				.Where(c => !firstTagOnly || !listed.Contains(c.row.name))
				.ToList();
			if (members.Count == 0) continue;

			listed.UnionWith(members.Select(m => m.row.name));
			choices.Add(new LocalStackRestartChoice
			{
				tag = tag,
				description = LocalStackStacks.TagDescriptions.TryGetValue(tag, out var description) ? description : null,
				steps = members.Select(m => new LocalStackRestartChoiceStep
				{
					name = m.row.name,
					label = string.IsNullOrWhiteSpace(m.row.label) ? LocalStackConfigIO.InferLabel(m.row.name) : m.row.label,
					state = LocalStackBoard.Text(m.row.state)
				}).ToList()
			});
		}

		return choices;
	}

	/// <summary>The tree of what can be named for <paramref name="action"/>, with examples.</summary>
	public static void PrintChoices(string action, List<LocalStackRestartChoice> choices)
	{
		if (choices.Count == 0)
		{
			AnsiConsole.MarkupLine(action == "start" ? "Everything is running." : "Nothing in the running stack can be restarted on its own.");
			return;
		}

		LocalStackStyle.H1($"What can be {(action == "start" ? "started" : "restarted")}", $"a tag {action}s all of its processes, a name just that one");
		var tagWidth = choices.Max(c => c.tag.Length);
		var stepWidth = choices.SelectMany(c => c.steps).Max(s => s.label.Length + s.name.Length + 3);
		foreach (var choice in choices)
		{
			AnsiConsole.MarkupLine($"  {LocalStackStyle.H3(choice.tag.PadRight(tagWidth))}   [grey]{Markup.Escape(choice.description ?? string.Empty)}[/]");
			foreach (var step in choice.steps)
				AnsiConsole.MarkupLine($"    {Markup.Escape($"{step.label} ({step.name})".PadRight(stepWidth))}   [grey]{Markup.Escape(step.state)}[/]");
		}

		var stepExample = choices.SelectMany(c => c.steps).First().name;
		AnsiConsole.MarkupLine(Markup.Escape(
			$"Examples: beam local ps --{action} {choices[0].tag} | beam local ps --{action} \"{stepExample}\" | beam local ps --{action} --all"));
	}

	/// <summary>The picker: everything, a whole tag, or single processes. False when there was nothing to pick.</summary>
	private static bool Pick(string action, List<LocalStackRestartChoice> choices, out List<string> items, out bool all)
	{
		items = new List<string>();
		all = false;
		if (choices.Count == 0)
		{
			PrintChoices(action, choices);
			return false;
		}

		const string everything = "all:", tagPrefix = "tag:", stepPrefix = "step:";
		var steps = choices.SelectMany(c => c.steps).GroupBy(s => s.name, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
		var prompt = new MultiSelectionPrompt<string>()
			.Title($"What do you want to {action}?")
			.InstructionsText($"[grey](Space picks a process or a whole tag, Enter {action}s what's picked)[/]")
			.PageSize(20)
			.UseConverter(value =>
			{
				if (value == everything) return "Everything [grey](--all)[/]";
				if (value.StartsWith(tagPrefix))
				{
					var choice = choices.First(c => c.tag == value.Substring(tagPrefix.Length));
					return $"[bold]{Markup.Escape(choice.tag)}[/] [grey]{Markup.Escape(choice.description ?? string.Empty)}[/]";
				}

				var step = steps[value.Substring(stepPrefix.Length)];
				return $"{Markup.Escape(step.label)} [grey]({Markup.Escape(step.name)}, {Markup.Escape(step.state)})[/]";
			});
		prompt.AddChoice(everything);
		foreach (var choice in choices)
			prompt.AddChoiceGroup(tagPrefix + choice.tag, choice.steps.Select(s => stepPrefix + s.name));

		var picked = AnsiConsole.Prompt(prompt);
		all = picked.Contains(everything);
		items = picked.Where(p => p.StartsWith(stepPrefix)).Select(p => p.Substring(stepPrefix.Length)).ToList();
		return all || items.Count > 0;
	}

	/// <summary>
	/// The running stack's definition, for labels and tags: its resolved steps and how to tag them. When it can't be
	/// resolved any more, the board's rows by name, without tags.
	/// </summary>
	public static (List<LocalStackStep> steps, Func<LocalStackStep, IEnumerable<string>> tagsOf) RunningDefinition(CommandArgs args,
		LocalStackBoardSnapshot board)
	{
		try
		{
			var settings = LocalStackCommand.ResolveManifestPath(args.ConfigService, null);
			var config = LocalStackConfigIO.Load(settings);
			var stacks = LocalStackStacks.Load(LocalStackConfigIO.FindWorkspaceRoot(settings));
			if (board?.stack != null)
				return (stacks.Resolve(board.stack, config).steps, stacks.TagsOf);
		}
		catch (LocalStackStackException e)
		{
			Log.Verbose($"Listing by name only: {e.Message}");
		}

		return (board?.rows.Select(r => new LocalStackStep { name = r.name }).ToList() ?? new List<LocalStackStep>(),
			_ => Enumerable.Empty<string>());
	}

	/// <summary>
	/// The steps <paramref name="items"/> name among those the running stack runs, by name, label or tag. Uses the
	/// stack's definition for labels and tags; when it can't be resolved any more, names only.
	/// </summary>
	public static List<LocalStackStep> MatchRecorded(CommandArgs args, string runStatePath, LocalStackRunState state, List<string> items)
	{
		var recorded = state.steps.Select(e => e.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		try
		{
			var settings = LocalStackCommand.ResolveManifestPath(args.ConfigService, null);
			var config = LocalStackConfigIO.Load(settings);
			var stacks = LocalStackStacks.Load(LocalStackConfigIO.FindWorkspaceRoot(settings));
			if (board?.stack != null)
			{
				var steps = stacks.Resolve(board.stack, config).steps;
				return LocalStackControl.Match(items, steps.Where(s => recorded.Contains(s.name) || s.build).ToList(), stacks.TagsOf)
					.Where(s => recorded.Contains(s.name)).ToList();
			}
		}
		catch (LocalStackStackException e)
		{
			Log.Verbose($"Matching by name only: {e.Message}");
		}

		return LocalStackControl.Match(items, state.steps.Select(e => new LocalStackStep { name = e.name }).ToList(), _ => Enumerable.Empty<string>());
	}
}

/// <summary>A tag of the running stack and its processes, as <c>beam local restart</c> lists them when nothing is named.</summary>
public class LocalStackRestartChoice
{
	public string tag;
	public string description;
	public List<LocalStackRestartChoiceStep> steps = new List<LocalStackRestartChoiceStep>();
}

public class LocalStackRestartChoiceStep
{
	public string name;
	public string label;
	public string state;
}
