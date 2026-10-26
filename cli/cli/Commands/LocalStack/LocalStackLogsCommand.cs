using Beamable.Server;
using cli.Services.LocalStack;
using cli.Utils;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using System.Text;
using System.Text.RegularExpressions;

namespace cli.Commands.LocalStack;

public class LocalStackLogsCommandArgs : CommandArgs
{
	public string configPath;
	public string step;
	public bool follow;
	public int lines = 200;
	public string forward;
	public bool clean;
}

public class LocalStackLogsCommandResult
{
	public string[] steps;
}

/// <summary>
/// Tails the per-step log files written by <c>beam local run</c>. With no step name, tails every recorded
/// step; <c>--follow</c> keeps streaming new lines until cancelled; <c>--lines</c> sets how much history to show.
/// </summary>
public class LocalStackLogsCommand
	: AtomicCommand<LocalStackLogsCommandArgs, LocalStackLogsCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	// It prints its own readable output; the result object still reaches tools through the data channel.
	public override bool AutoLogOutput => false;

	public LocalStackLogsCommand() : base("logs", "Tail the log files of the running local stack")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("step", () => null, "The step name to tail (default: all recorded steps)"),
			(args, v) => args.step = v);
		AddOption(new Option<string>("--config", "Path to the manifest whose run-state to read (defaults to .beamable/local/stack-settings.json)"),
			(args, v) => args.configPath = v);
		var follow = new Option<bool>("--follow", "Keep streaming new log lines until cancelled");
		follow.AddAlias("-f");
		AddOption(follow, (args, v) => args.follow = v);
		var lines = new Option<int>("--lines", () => 200, "How many trailing lines of history to show first");
		lines.AddAlias("-n");
		AddOption(lines, (args, v) => args.lines = v);
		AddOption(new Option<bool>("--clean", "Start the logs over: empty Loki and Tempo (their containers are recreated; nothing else in docker is touched) and delete earlier runs' log folders"),
			(args, v) => args.clean = v);
		// What the stack's `logs: forward` step runs (see LocalStackTemplate.LogsForwardStep).
		AddOption(new Option<string>("--forward", "INTERNAL Send every step's lines to this OTLP/HTTP endpoint until cancelled") { IsHidden = true },
			(args, v) => args.forward = v);
	}

	public override async Task<LocalStackLogsCommandResult> GetResult(LocalStackLogsCommandArgs args)
	{
		var runStatePath = LocalStackCommand.ResolveRunStatePath(args.ConfigService, args.configPath);
		if (args.clean)
		{
			Clean(args, runStatePath);
			return new LocalStackLogsCommandResult { steps = Array.Empty<string>() };
		}

		if (!string.IsNullOrEmpty(args.forward))
		{
			await LogForwarder.Run(runStatePath, Path.GetFileName(args.ConfigService.BeamableWorkspace), args.forward, args.Lifecycle.CancellationToken);
			return new LocalStackLogsCommandResult { steps = Array.Empty<string>() };
		}

		var state = LocalStackRunStateIO.Load(runStatePath);
		if (state == null || state.steps.Count == 0)
			state = FromAttachedBoard(runStatePath);
		if (state == null || state.steps.Count == 0)
			throw new CliException($"No running local stack recorded at {runStatePath}. Start one with `beam local run <stack>`.");

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

	/// <summary>
	/// Starts the logs over, without <c>stop --purge</c> (which also empties the local Mongo): recreates the Loki and
	/// Tempo containers of the stack's docker step, with that step's environment so their retention still applies,
	/// and starts them again if they were running; then deletes earlier runs' log folders, keeping the ones running
	/// steps still write to.
	/// </summary>
	private static void Clean(LocalStackLogsCommandArgs args, string runStatePath)
	{
		var settingsPath = LocalStackCommand.ResolveManifestPath(args.ConfigService, args.configPath);
		var config = LocalStackConfigIO.Load(settingsPath);
		var docker = config.steps.FirstOrDefault(s => s.name == LocalStackTemplate.ApiDepsStepName);
		if (docker == null)
		{
			Log.Warning($"No '{LocalStackTemplate.ApiDepsStepName}' step in {settingsPath}: Loki and Tempo were left as they are.");
		}
		else
		{
			var dir = LocalStackConfigIO.Substitute(docker.workingDirectory, config);
			var environment = docker.environment?.ToDictionary(e => e.Key, e => LocalStackConfigIO.Substitute(e.Value, config));
			string Compose(string arguments)
			{
				var result = StartProcessUtil.Run(docker.command, "compose " + arguments, useShell: true, environmentVariables: environment,
					workingDirectoryPath: dir).WaitForResult();
				if (result.exit != 0)
					throw new CliException($"`{docker.command} compose {arguments}` failed in {dir}:\n{result.stderr}\n{result.stdout}".Trim());
				return result.stdout ?? string.Empty;
			}

			var services = new[] { "loki", "tempo" };
			var running = Compose("ps --services --status running").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			Compose($"rm --stop --force --volumes {string.Join(" ", services)}");
			var restart = services.Where(s => running.Contains(s)).ToList();
			if (restart.Count > 0) Compose($"up -d --wait {string.Join(" ", restart)}");
			Log.Information($"Emptied Loki and Tempo{(restart.Count > 0 ? " and started them again" : string.Empty)}.");
		}

		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		var state = LocalStackRunStateIO.Load(runStatePath);
		var keep = (state?.steps ?? new List<LocalStackRunEntry>()).SelectMany(e => new[] { e.stdoutLog, e.stderrLog })
			.Where(f => !string.IsNullOrEmpty(f)).Select(Path.GetDirectoryName);
		var deleted = LocalStackRunCommand.PruneStaleTempLogs(settingsPath, board?.logsDir, keep);
		Log.Information($"Deleted {deleted} earlier run log folder(s); the running steps' logs are kept.");
	}

	/// <summary>
	/// An attached stack keeps no run-state, only its board, which says where the run writes each step's output. Its
	/// steps with a log file, as entries this command reads; null when no attached stack's board is there.
	/// </summary>
	public static LocalStackRunState FromAttachedBoard(string runStatePath)
	{
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		if (board == null || LocalStackBoardIO.IsStale(board) || string.IsNullOrEmpty(board.logsDir) || !Directory.Exists(board.logsDir))
			return null;

		var state = new LocalStackRunState();
		foreach (var row in board.rows)
		{
			var log = Path.Combine(board.logsDir, LocalStackRunCommand.SafeName(row.name));
			if (File.Exists(log + ".log") || File.Exists(log + ".err.log"))
				state.steps.Add(new LocalStackRunEntry { name = row.name, stdoutLog = log + ".log", stderrLog = log + ".err.log" });
		}

		return state;
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

/// <summary>
/// Follows every log file of the stack running here, those of steps a restart starts later too, and sends each line
/// to an OTLP/HTTP collector as a log record, with the stack, workspace, step and kind as attributes (Loki indexes
/// them). Lines while the collector is away are dropped; the files still have them.
/// </summary>
public static class LogForwarder
{
	/// <summary>The resource attributes each record carries, as BeamableAPI's Loki config indexes them.</summary>
	public const string StackAttribute = "beam.stack", WorkspaceAttribute = "beam.workspace", StepAttribute = "beam.step", KindAttribute = "beam.kind";

	private static readonly Regex Level = new Regex(@"\b(FATAL|CRITICAL|ERROR|ERR|FAIL|WARN|WARNING|INFO|INFORMATION|DEBUG|TRACE|VERBOSE)\b", RegexOptions.Compiled);

	/// <summary>A JSON line's own level field: the C# hosts' "l", Serilog's "@l", "level", "severity".</summary>
	private static readonly Regex JsonLevel = new Regex(@"""(?:@?l|level|severity)""\s*:\s*""(\w+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	public static async Task Run(string runStatePath, string workspace, string endpoint, CancellationToken token)
	{
		var followed = new Dictionary<string, (string step, LineTailer tailer)>(StringComparer.OrdinalIgnoreCase);
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
		var url = endpoint.TrimEnd('/') + "/v1/logs";
		var lastScan = DateTime.MinValue;
		string stack = null;
		Log.Information($"Forwarding the stack's logs to {url}.");
		try
		{
			while (!token.IsCancellationRequested)
			{
				// Restarts write to new folders: look for new files every few seconds.
				if (DateTime.UtcNow - lastScan > TimeSpan.FromSeconds(3))
				{
					lastScan = DateTime.UtcNow;
					foreach (var (step, path) in Files(runStatePath, out var current))
					{
						stack = current ?? stack;
						if (!followed.ContainsKey(path) && File.Exists(path))
							followed[path] = (step, new LineTailer(path, -1));
					}
				}

				var lines = new List<(string step, string line)>();
				foreach (var (step, tailer) in followed.Values)
					lines.AddRange(tailer.ReadAvailableLines().Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => (step, l)));

				if (lines.Count > 0)
				{
					try
					{
						using var body = new StringContent(Payload(lines, stack, workspace, DateTime.UtcNow).ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");
						using var response = await http.PostAsync(url, body, token);
						if (!response.IsSuccessStatusCode) Log.Verbose($"The collector answered {(int)response.StatusCode}; {lines.Count} lines dropped.");
					}
					catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested)
					{
						Log.Verbose($"Couldn't reach the collector ({e.Message}); {lines.Count} lines dropped.");
					}
				}

				await Task.Delay(1000, token);
			}
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			foreach (var (_, tailer) in followed.Values) tailer.Dispose();
		}
	}

	/// <summary>Every step's stdout and stderr files the run-state and the board know of, and the stack's name.</summary>
	private static List<(string step, string path)> Files(string runStatePath, out string stack)
	{
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		stack = board?.stack;
		var entries = (LocalStackRunStateIO.Load(runStatePath)?.steps ?? new List<LocalStackRunEntry>())
			.Concat(LocalStackLogsCommand.FromAttachedBoard(runStatePath)?.steps ?? new List<LocalStackRunEntry>());
		return entries.SelectMany(e => new[] { (e.name, e.stdoutLog), (e.name, e.stderrLog) })
			.Where(f => !string.IsNullOrEmpty(f.Item2))
			.Distinct()
			.ToList();
	}

	/// <summary>An OTLP/HTTP JSON logs request: one resource per step, each line a record at <paramref name="now"/>
	/// with the level it names, or INFO.</summary>
	public static JObject Payload(IEnumerable<(string step, string line)> lines, string stack, string workspace, DateTime now)
	{
		var nanos = ((now.ToUniversalTime().Ticks - DateTime.UnixEpoch.Ticks) * 100).ToString();
		JObject Attribute(string key, string value) => new JObject { ["key"] = key, ["value"] = new JObject { ["stringValue"] = value ?? string.Empty } };

		var resources = new JArray();
		foreach (var group in lines.GroupBy(l => l.step))
		{
			var kind = LocalStackConfigIO.InferTags(group.Key).FirstOrDefault() ?? "custom";
			resources.Add(new JObject
			{
				["resource"] = new JObject
				{
					["attributes"] = new JArray(Attribute("service.name", group.Key), Attribute(StackAttribute, stack),
						Attribute(WorkspaceAttribute, workspace), Attribute(StepAttribute, group.Key), Attribute(KindAttribute, kind))
				},
				["scopeLogs"] = new JArray(new JObject
				{
					["scope"] = new JObject { ["name"] = "beam local" },
					["logRecords"] = new JArray(group.Select(l => new JObject
					{
						["timeUnixNano"] = nanos,
						["severityText"] = SeverityOf(l.line),
						["body"] = new JObject { ["stringValue"] = l.line }
					}))
				})
			});
		}

		return new JObject { ["resourceLogs"] = resources };
	}

	public static string SeverityOf(string line)
	{
		line ??= string.Empty;
		var json = JsonLevel.Match(line);
		Group match = json.Success ? json.Groups[1] : Level.Match(line);
		if (!match.Success) return "INFO";
		return match.Value.ToUpperInvariant() switch
		{
			"FATAL" or "CRITICAL" => "FATAL",
			"ERROR" or "ERR" or "FAIL" => "ERROR",
			"WARN" or "WARNING" => "WARN",
			"DEBUG" => "DEBUG",
			"TRACE" or "VERBOSE" => "TRACE",
			_ => "INFO"
		};
	}
}
