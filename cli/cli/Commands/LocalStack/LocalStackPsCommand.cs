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
}

public class LocalStackPsCommandResult
{
	public string host;
	public string portalUrl;
	public bool backendHealthy;
	public List<LocalStackPsEntry> steps = new List<LocalStackPsEntry>();
}

/// <summary>
/// Reports the status of the local stack recorded by <c>beam local up</c>: which recorded steps are still
/// alive (by pid), plus a backend-health probe of <c>${host}/metadata</c> (PR#632). <c>--watch</c> re-renders
/// on an interval until cancelled.
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
		AddOption(new Option<string>("--config", "Path to the manifest whose run-state to read (defaults to .beamable/local-stack.json)"),
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
		if (state == null) return result;

		result.host = state.host;
		result.portalUrl = state.portalUrl;

		foreach (var s in state.steps)
		{
			// The recorded pid works once `up` has resolved it to the service leaf. For stacks brought up by
			// an older CLI (which recorded the Windows wrapper pid that has since died), fall back to matching
			// the service on a live JVM's command line so it isn't misreported as "stopped".
			var running = !s.waitForExit && IsRunning(s.pid);
			if (!running && !s.waitForExit)
				running = LocalStackProcess.FindByCommandLine(
					LocalStackStopCommand.BuildKillTokens(s), LocalStackProcess.ServiceImages).Count > 0;

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

	private static bool IsRunning(int pid)
	{
		if (pid <= 0) return false;
		try
		{
			var p = Process.GetProcessById(pid);
			return !p.HasExited;
		}
		catch
		{
			return false;
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
			Log.Information($"No running local stack recorded at {runStatePath}. Run `beam local up` first.");
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
		var backend = result.backendHealthy ? "[green]healthy[/]" : "[yellow]no /metadata response[/]";
		AnsiConsole.MarkupLine($"backend={Markup.Escape(result.host ?? "?")} ({backend})  portal={Markup.Escape(result.portalUrl ?? "?")}");
	}
}
