using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.Services.LocalStack;
using cli.Services.Web;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ServiceLogs = Beamable.Common.Constants.Features.Services.Logs;

namespace cli.Commands.LocalStack;

public class LocalStackRunCommandArgs : CommandArgs
{
	/// <summary>The stack to start, as <c>&lt;stack&gt;[:variant…]</c>. Null lists the stacks instead.</summary>
	public string stack;

	/// <summary>For a run started from code (a restart): also stop starting steps when this is cancelled. A detached
	/// run leaves what it started running.</summary>
	public CancellationToken cancel;

	/// <summary>Print what would run and check what it needs, starting nothing.</summary>
	public bool dryRun;

	public string host;
	public string portalUrl;
	public bool runDetached;
	public bool saveLogs;

	/// <summary>Stream every step's own output instead of the progress board.</summary>
	public bool verbose;
	public bool noCreateRealm;

	/// <summary>Runs against a target that asks before every run (prod) without asking, for scripts and agents.</summary>
	public bool confirmTarget;
	public string realmCustomer;
	public string realmProject;
	public string realmEmail;
	public string realmAlias;
	public string realmPassword;
}

public class LocalStackRunResultStream
{
	/// <summary>The step this update is about (empty for stack-level messages).</summary>
	public string step;

	/// <summary>One of: starting, ready, running, failed, skipped, stopped, tearing-down.</summary>
	public string status;

	/// <summary>Human-readable detail.</summary>
	public string message;

	/// <summary>0..1 across the whole bring-up.</summary>
	public float progressRatio;

	/// <summary>The step's readable name, as the board shows it.</summary>
	public string label;

	/// <summary>The step's row on the board after this update: Waiting, Building, Starting, Restarting, Ready, Running,
	/// Degraded, Done or Failed (see <see cref="LocalStackStepState"/>).</summary>
	public string state;

	/// <summary>Seconds since the step started, up to when it settled.</summary>
	public int elapsedSeconds;
}

public class LocalStackRunListingChannel : IResultChannel
{
	public string ChannelName => "stacks";
}

/// <summary>What a bare <c>beam local run</c> lists, as data for a piped caller.</summary>
public class LocalStackRunListing
{
	public List<LocalStackRunListedStack> stacks = new List<LocalStackRunListedStack>();

	/// <summary>The variants every stack has, such as <c>build</c> and <c>web</c>.</summary>
	public List<string> builtInVariants = new List<string>();

	/// <summary>The backends a stack can run against; each is also a variant (<c>:dev</c>).</summary>
	public List<string> targets = new List<string>();
}

public class LocalStackRunListedStack
{
	public string name;
	public string description;

	/// <summary>built-in, shared or local: the layer that defines it first.</summary>
	public string layer;

	/// <summary>The backend it runs against unless a variant names another.</summary>
	public string target;

	public List<string> changedBy = new List<string>();
	public List<LocalStackRunListedVariant> variants = new List<LocalStackRunListedVariant>();
}

public class LocalStackRunListedVariant
{
	public string name;
	public string description;
	public string layer;
}

public class LocalStackDryRunChannel : IResultChannel
{
	public string ChannelName => "dryRun";
}

/// <summary>What <c>--dry-run</c> prints, as data for a piped caller.</summary>
public class LocalStackDryRun
{
	public string stack;
	public string description;
	public List<LocalStackDryRunStep> steps = new List<LocalStackDryRunStep>();
	public List<LocalStackDryRunVariable> variables = new List<LocalStackDryRunVariable>();
	public List<LocalStackDependencyCheck> checks = new List<LocalStackDependencyCheck>();
}

public class LocalStackDryRunStep
{
	public string name;
	public string label;

	/// <summary>What happens to it: starts, runs once, builds, or why it is skipped.</summary>
	public string runs;

	/// <summary>False when the run skips it.</summary>
	public bool selected;

	public int port;
	public string folder;
}

public class LocalStackDryRunVariable
{
	public string name;
	public string value;
	public string source;
}

/// <summary>
/// Starts a stack: <c>beam local run &lt;stack&gt;[:variant…]</c>, like <c>npm run</c>. The name alone decides what
/// runs (see <see cref="LocalStackStacks"/>); a bare <c>beam local run</c> lists the stacks. Before starting, it
/// creates the settings file when there is none, checks what the stack needs and runs the parts of
/// <c>beam local setup</c> that are missing; <c>--dry-run</c> prints the steps and those checks and stops there.
///
/// Every resolved step comes up in order, waiting for each readiness gate. In a terminal a progress board shows one
/// row per step (see <see cref="LocalStackBoard"/>); without one, each state change is one line; piped as data, the
/// result stream carries the states. <c>--verbose</c> streams every step's own output instead. Each step's output
/// also goes to a log file for the run. Attached by default: the stack stops with the command (Ctrl+C). <c>--detach</c> launches long-running
/// services with their output in per-step log files and records them in a run-state file, so they survive this
/// command returning — like <c>docker compose up -d</c>. Use <c>beam local ps</c> / <c>logs</c> / <c>stop</c> to
/// manage them.
/// </summary>
public class LocalStackRunCommand
	: StreamCommand<LocalStackRunCommandArgs, LocalStackRunResultStream>
	, IResultSteam<LocalStackRunListingChannel, LocalStackRunListing>
	, IResultSteam<LocalStackDryRunChannel, LocalStackDryRun>
	, IStandaloneCommand, ISkipManifest
{
	private readonly object _launchedLock = new object();

	/// <summary>The board for this run; null before the first step launches.</summary>
	private LocalStackBoard _board;

	private bool _attached;
	private bool _verbose;

	// What each build step compiled when it last built, and where that is saved. Compared on the next run so a build
	// runs when its sources changed, rather than only when its output is missing or :build rebuilds everything.
	private LocalStackBuildState _builds;
	private string _buildsPath;

	// The stack's target login: handed to its beam steps as environment overrides, so running a stack against a
	// backend never rewrites the workspace's .beamable config that every other beam command uses. Null until known: a
	// remote target's before anything starts, the local realm's once the backend is up.
	private LocalStackTargetLogin _targetLogin;

	// The target's name, which its login is saved under: local and lan reach the same realm, each from its own address.
	private string _targetName = LocalStackTargets.Local;

	/// <summary>True when this run points the extensions at the local web packages, and the portal loads them too.</summary>
	private bool _localWebPackages;

	/// <summary>
	/// Why there is no working login to the local realm, or null when there is one. Beam steps don't start without it:
	/// they fell back to the workspace's own login, and with that one's refresh token rejected, every microservice and
	/// extension retried it many times a second until the gateway stopped answering.
	/// </summary>
	private string _noLogin;

	/// <summary>True for a step that logs in to the stack's backend: a beam step other than the web packages.</summary>
	/// <summary>A beam step that logs in to the backend: not the `beam local …` ones (the web packages, the log
	/// forwarding), which only work on this machine.</summary>
	private static bool NeedsLogin(LocalStackStep step) =>
		step.beam && !(step.arguments ?? string.Empty).TrimStart().StartsWith("local ", StringComparison.Ordinal);
	private string _workspaceRoot;

	/// <summary>True when the board prints (a terminal, or lines without one): failures are on the board, not warned twice.</summary>
	private bool Showing => _board != null && _board.Mode != LocalStackBoardMode.Silent;

	// The steps that ran to completion and failed (or couldn't start), with why. A failed step doesn't stop the stack:
	// the steps that need it are skipped, and the rest carry on.
	private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _failed =
		new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public LocalStackRunCommand() : base("run", "Start a local stack by name, like `npm run`: `beam local run` lists the stacks, `beam local run <stack>[:variant...]` starts one")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("stack", () => null, "The stack to start, with variants applied left to right: <stack>[:<variant>...]. Leave it out to list the stacks and their variants"),
			(args, v) => args.stack = v);
		AddOption(new Option<bool>("--dry-run", "Print the resolved steps, their folders and ports, the variables they use and the checks for what the stack needs, then stop without starting anything"),
			(args, v) => args.dryRun = v);
		AddOption(new Option<string>("--host", "Override the settings' backend host"),
			(args, v) => args.host = v);
		AddOption(new Option<string>("--portal-url", "Override the settings' portal URL"),
			(args, v) => args.portalUrl = v);
		var detach = new Option<bool>("--detach", "Return the prompt and keep the stack running; manage it with `beam local ps`/`logs`/`stop`. Default runs attached: the progress board shows while it starts, and the stack stops when `run` exits (Ctrl+C)");
		detach.AddAlias("-d");
		AddOption(detach, (args, v) => args.runDetached = v);

		AddOption(new Option<bool>("--save-logs", "Persist per-run logs under the workspace (.beamable/local-stack-logs/run-<id>); without it logs go to a temp folder and are removed on `beam local stop`"),
			(args, v) => args.saveLogs = v);
		AddOption(new Option<bool>("--verbose", "Stream every step's own output, prefixed with the step name, instead of the progress board"),
			(args, v) => args.verbose = v);

		AddOption(new Option<bool>("--no-create-realm", "Do not auto-create a local realm when the saved login is invalid — just warn (by default `run` creates one after a docker cleanup)"),
			(args, v) => args.noCreateRealm = v);
		AddOption(new Option<bool>("--confirm-target", "Run against a backend that asks first (prod) without asking; required without a terminal"),
			(args, v) => args.confirmTarget = v);
		AddOption(new Option<string>("--realm-customer", () => "beam", "Customer name to use when creating the local realm"),
			(args, v) => args.realmCustomer = v);
		AddOption(new Option<string>("--realm-project", () => "beam-project", "Project name to use when creating the local realm"),
			(args, v) => args.realmProject = v);
		AddOption(new Option<string>("--realm-email", () => "beam@beamable.com", "Account email to use when creating the local realm"),
			(args, v) => args.realmEmail = v);
		AddOption(new Option<string>("--realm-alias", () => "beam-project", "Alias to use when creating the local realm"),
			(args, v) => args.realmAlias = v);
		AddOption(new Option<string>("--realm-password", () => "123456", "Account password to use when creating the local realm"),
			(args, v) => args.realmPassword = v);
	}

	/// <summary>
	/// Which of a resolved stack's steps this run starts. Every non-build step runs. A build step runs when the
	/// name forces builds (<c>:build</c>), when it asks for that step outright (by name, by the <c>build</c> or
	/// <c>web-registry</c> tag, or through a variant), or when the step's output is missing.
	///
	/// Pure so the gate is testable: a step silently NOT selected is the failure mode this guards — the web steps
	/// used to be skipped on a run that still reported success, leaving the portal on a published toolkit pin.
	/// </summary>
	/// <param name="autoBuild">Build steps whose declared output is missing, so they build anyway.</param>
	public static List<LocalStackStep> SelectSteps(LocalStackResolvedStack resolved, HashSet<LocalStackStep> autoBuild) =>
		resolved.steps
			.Where(s => !s.build || resolved.forceBuild || autoBuild.Contains(s) || resolved.requested.Contains(s))
			.ToList();

	/// <summary>
	/// The build steps of <paramref name="steps"/> grouped into lanes, one per working folder, in stack order. The
	/// lanes run side by side and each lane's builds one after another. Builds in one folder compile the same
	/// projects: the C# hosts all build BeamableShared into the same obj/bin, and building them at once fails on
	/// locked files (MSB3021). Builds in different repos share nothing, and running them one after another made a
	/// full build wait for the sum of them, a cold Scala reactor build alone being ~15 minutes.
	/// </summary>
	public static List<List<LocalStackStep>> BuildLanes(IEnumerable<LocalStackStep> steps, LocalStackConfig config) =>
		steps.Where(s => s.build)
			.GroupBy(s => LaneOf(s, config), StringComparer.OrdinalIgnoreCase)
			.Select(g => g.ToList())
			.ToList();

	private static string LaneOf(LocalStackStep step, LocalStackConfig config)
	{
		var dir = LocalStackConfigIO.Substitute(step.workingDirectory, config) ?? string.Empty;
		try
		{
			return Path.GetFullPath(string.IsNullOrWhiteSpace(dir) ? "." : dir).TrimEnd('/', '\\');
		}
		catch (Exception)
		{
			return dir;
		}
	}

	/// <summary>
	/// Runs <paramref name="run"/> for each build of <paramref name="lanes"/>: the lanes side by side, each lane's
	/// builds in order. Returns each build's task. When <paramref name="run"/> throws, the builds after it in its lane
	/// fail the same way without running. (A build that fails doesn't throw: it is marked failed and the next one runs.)
	/// </summary>
	public static Dictionary<LocalStackStep, Task> RunInLanes(IEnumerable<List<LocalStackStep>> lanes, Func<LocalStackStep, Task> run)
	{
		var tasks = new Dictionary<LocalStackStep, Task>();
		foreach (var lane in lanes)
		{
			var previous = Task.CompletedTask;
			foreach (var build in lane)
			{
				previous = After(previous, build);
				tasks[build] = previous;
			}
		}

		return tasks;

		async Task After(Task previous, LocalStackStep build)
		{
			await previous;
			// Off the caller, so starting one lane's process never holds up the next lane.
			await Task.Yield();
			await run(build);
		}
	}

	/// <summary>
	/// True for a build that has to wait for its place in the stack instead of starting with the run: one that runs a
	/// beam command, which may need the steps before it. Building microservices runs one too: it generates the
	/// extensions' clients from the backend's beamo manifest (`dotnet beam project generate pe-client`).
	/// </summary>
	public static bool WaitsForItsPlace(LocalStackStep build) =>
		build.beam || build.name == LocalStackTemplate.MicroservicesBuildStepName;

	public static HashSet<string> NameSet(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			// Split on comma only — step names contain spaces (e.g. "portal frontend", "scala: gateway").
			: value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

	private static string LayerName(LocalStackLayer layer) => layer switch
	{
		LocalStackLayer.BuiltIn => "built-in",
		LocalStackLayer.Shared => "shared",
		_ => "local"
	};

	/// <summary>The listing as data: what a piped <c>beam local run</c> sends on the <c>stacks</c> channel.</summary>
	public static LocalStackRunListing Listing(LocalStackStacks stacks, LocalStackConfig config = null) => new LocalStackRunListing
	{
		targets = LocalStackTargets.Names(config),
		stacks = stacks.List().Select(s => new LocalStackRunListedStack
		{
			name = s.name,
			description = s.description,
			layer = LayerName(s.layer),
			target = s.target,
			changedBy = s.changedBy.Select(LayerName).ToList(),
			variants = s.variants.Select(v => new LocalStackRunListedVariant { name = v.name, description = v.description, layer = LayerName(v.layer) }).ToList()
		}).ToList(),
		builtInVariants = stacks.BuiltInVariants.Keys.ToList()
	};

	/// <summary>The listing a bare <c>beam local run</c> prints, like <c>npm run</c>: stacks by layer, each with its variants.</summary>
	private static void PrintStacks(LocalStackStacks stacks, LocalStackConfig config)
	{
		var listing = stacks.List();
		var width = listing.SelectMany(s => s.variants.Select(v => v.name.Length + 3).Append(s.name.Length + 1)).DefaultIfEmpty(10).Max() + 2;

		LocalStackStyle.H1("Stacks", "beam local run <stack>[:variant...]");
		foreach (var layer in listing.Select(s => s.layer).Distinct())
		{
			LocalStackStyle.H2(layer switch { LocalStackLayer.BuiltIn => "Built in", LocalStackLayer.Shared => "Shared with the team", _ => "Yours" },
				LayerName(layer));
			foreach (var stack in listing.Where(s => s.layer == layer))
			{
				var changed = stack.changedBy.Count == 0
					? string.Empty
					: $" [grey](changed in {string.Join(", ", stack.changedBy.Select(LayerName))})[/]";
				var against = string.Equals(stack.target, LocalStackTargets.Local, StringComparison.OrdinalIgnoreCase)
					? string.Empty
					: $" [blue](against {Markup.Escape(stack.target)})[/]";
				AnsiConsole.MarkupLine($"  [green]{Markup.Escape(stack.name.PadRight(width))}[/]{Markup.Escape(stack.description ?? string.Empty)}{against}{changed}");

				foreach (var (name, description, variantLayer) in stack.variants)
				{
					var from = variantLayer > stack.layer ? $" [grey]({LayerName(variantLayer)})[/]" : string.Empty;
					AnsiConsole.MarkupLine($"    {Markup.Escape((":" + name).PadRight(width - 2))}{Markup.Escape(description ?? string.Empty)}{from}");
				}
			}
		}

		AnsiConsole.MarkupLine($"[grey]every stack also has {string.Join(", ", stacks.BuiltInVariants.Keys.Select(k => ":" + k))}, and one variant per backend: "
		                       + $"{string.Join(", ", LocalStackTargets.Names(config).Select(t => ":" + t))}[/]");
		AnsiConsole.MarkupLine("[grey]start one with `beam local run <stack>[[:variant...]]`; `--dry-run` shows what it would run[/]");
	}

	/// <summary>
	/// What <c>--dry-run</c> shows: each step in order with what will happen to it, its folder and port, then every
	/// variable the steps use with its value and where the value comes from, then the checks.
	/// </summary>
	public static LocalStackDryRun DryRun(LocalStackResolvedStack resolved, List<LocalStackStep> selected, HashSet<LocalStackStep> autoBuild,
		LocalStackConfig config, HashSet<string> fromDefaults, List<LocalStackDependencyCheck> checks,
		IReadOnlyDictionary<LocalStackStep, string> buildReasons = null, HashSet<LocalStackStep> upToDate = null)
	{
		var dryRun = new LocalStackDryRun { stack = resolved.name, description = resolved.description, checks = checks };
		foreach (var step in resolved.steps)
		{
			string runs;
			if (!selected.Contains(step))
				runs = "up to date (add :build to build anyway)";
			else if (!step.build)
				runs = step.waitForExit ? "runs once" : "starts";
			else if (resolved.forceBuild)
				runs = "builds: :build";
			else if (resolved.requested.Contains(step) && upToDate?.Contains(step) == true)
				runs = "up to date (add :build to build anyway)";
			else if (resolved.requested.Contains(step))
				runs = "builds";
			else
				runs = buildReasons != null && buildReasons.TryGetValue(step, out var reason) ? $"builds: {reason}" : "builds";

			dryRun.steps.Add(new LocalStackDryRunStep
			{
				name = step.name, label = LocalStackCreateCommand.Label(step), runs = runs, selected = selected.Contains(step),
				port = step.port, folder = step.workingDirectory
			});
		}

		foreach (var (name, value, source) in LocalStackConfigIO.DescribeVariables(selected, config))
			dryRun.variables.Add(new LocalStackDryRunVariable { name = name, value = value, source = fromDefaults.Contains(name) ? "stack default" : source });

		return dryRun;
	}

	private static void PrintDryRun(LocalStackDryRun dryRun)
	{
		LocalStackStyle.H1($"Dry run of {dryRun.stack}");
		if (!string.IsNullOrWhiteSpace(dryRun.description)) AnsiConsole.MarkupLine($"[grey]{Markup.Escape(dryRun.description)}[/]");

		LocalStackStyle.H2("Steps", "in the order they start");
		var steps = new Table().Border(TableBorder.Simple);
		steps.AddColumn("[bold]#[/]");
		steps.AddColumn("[bold]step[/]");
		steps.AddColumn("[bold]runs[/]");
		steps.AddColumn("[bold]port[/]");
		steps.AddColumn("[bold]folder[/]");

		var index = 0;
		foreach (var step in dryRun.steps)
		{
			steps.AddRow(
				new Markup(step.selected ? (++index).ToString() : string.Empty),
				new Markup(Markup.Escape(step.name ?? string.Empty)),
				new Markup(step.selected ? Markup.Escape(step.runs) : $"[grey]{Markup.Escape(step.runs)}[/]"),
				new Markup(step.port > 0 ? step.port.ToString() : string.Empty),
				new Markup(Markup.Escape(step.folder ?? string.Empty)));
		}

		AnsiConsole.Write(steps);

		LocalStackStyle.H2("Variables", "what the steps' ${...} resolve to");
		var variables = new Table().Border(TableBorder.Simple);
		variables.AddColumn("[bold]variable[/]");
		variables.AddColumn("[bold]value[/]");
		variables.AddColumn("[bold]from[/]");
		foreach (var variable in dryRun.variables)
			variables.AddRow(Markup.Escape("${" + variable.name + "}"), Markup.Escape(variable.value ?? string.Empty), Markup.Escape(variable.source ?? string.Empty));

		AnsiConsole.Write(variables);
		LocalStackStyle.H2("Checks", "what the stack needs on this machine");
		LocalStackPreflight.Render(dryRun.checks);
		Log.Information($"Dry run: nothing was started. `beam local run {dryRun.stack}` starts these {index} steps.");
	}

	/// <summary>
	/// Gives every variable the stack declares a value: the settings' own value wins, then the stack's default; a
	/// variable with neither is asked for and saved to the settings, or fails the run when nobody can be asked.
	/// Returns the names that took the stack's default, so the dry run can say so.
	/// </summary>
	private static HashSet<string> ApplyStackVariables(LocalStackResolvedStack resolved, LocalStackConfig config, string path, bool canAsk)
	{
		var fromDefaults = new HashSet<string>(StringComparer.Ordinal);
		var missing = new List<(string name, LocalStackStackVariable declaration)>();
		foreach (var (name, declaration) in resolved.variables)
		{
			if (config.variables?.ContainsKey(name) == true || config.paths?.ContainsKey(name) == true)
				continue;

			if (declaration?.defaultValue != null)
			{
				config.variables ??= new Dictionary<string, string>();
				config.variables[name] = declaration.defaultValue;
				fromDefaults.Add(name);
				continue;
			}

			missing.Add((name, declaration));
		}

		if (missing.Count == 0)
			return fromDefaults;

		string Describe((string name, LocalStackStackVariable declaration) v) =>
			string.IsNullOrWhiteSpace(v.declaration?.description) ? "${" + v.name + "}" : "${" + v.name + "} (" + v.declaration.description + ")";

		if (!canAsk)
			throw new CliException(
				$"'{resolved.name}' needs a value for {string.Join(", ", missing.Select(Describe))}. Add it under \"variables\" in {path}, or run this in an interactive terminal to be asked.");

		var saved = LocalStackConfigIO.Load(path);
		saved.variables ??= new Dictionary<string, string>();
		foreach (var variable in missing)
		{
			var value = AnsiConsole.Prompt(new TextPrompt<string>($"Value for [green]{Markup.Escape(Describe(variable))}[/]:"));
			saved.variables[variable.name] = value;
			config.variables ??= new Dictionary<string, string>();
			config.variables[variable.name] = value;
		}

		LocalStackConfigIO.Save(path, saved);
		Log.Information($"Saved {missing.Count} variable(s) to {path}.");
		return fromDefaults;
	}

	/// <summary>Runs the given <c>beam local setup</c> steps in this process, as <c>--only</c> would.</summary>
	private static async Task<LocalStackSetupCommandResult> RunSetup(LocalStackRunCommandArgs args, IEnumerable<string> ids)
	{
		var setupArgs = args.Create<LocalStackSetupCommandArgs>();
		setupArgs.Quiet = args.Quiet;
		setupArgs.only = string.Join(",", ids);
		// What the options default to when setup runs from the command line.
		setupArgs.githubRepo = ScalaLocalVarsService.DefaultRepo;
		setupArgs.awsRegion = AwsPreflightService.DefaultRegion;
		setupArgs.scalaJvmArgs = LocalStackTemplate.DefaultScalaJvmArgs;
		return await new LocalStackSetupCommand().GetResult(setupArgs);
	}

	public override async Task Handle(LocalStackRunCommandArgs args)
	{
		var path = LocalStackCommand.ResolveManifestPath(args.ConfigService, null);

		LocalStackStacks stacks;
		try
		{
			stacks = LocalStackStacks.Load(LocalStackConfigIO.FindWorkspaceRoot(path));
		}
		catch (LocalStackStackException e)
		{
			throw new CliException(e.Message);
		}

		if (string.IsNullOrWhiteSpace(args.stack))
		{
			var settings = File.Exists(path) ? LocalStackConfigIO.Load(path) : null;
			PrintStacks(stacks, settings);
			this.SendResults<LocalStackRunListingChannel, LocalStackRunListing>(Listing(stacks, settings));
			return;
		}

		// Setup runs itself: a first run on this machine creates the settings, asking for what it can't detect.
		if (!File.Exists(path))
		{
			if (args.dryRun)
				throw new CliException($"No settings at {path} yet. `beam local run {args.stack}` creates them, or run `beam local setup`.");

			Log.Information($"No settings at {path} yet. Creating them.");
			await RunSetup(args, new[] { ToolchainPins.Settings });
			if (!File.Exists(path))
				throw new CliException($"Setup didn't create {path}. Run `beam local setup` to see why.");
		}

		LocalStackConfig config = null;
		LocalStackResolvedStack resolved = null;
		HashSet<string> fromDefaults = null;
		LocalStackTarget target = null;
		_workspaceRoot = LocalStackConfigIO.FindWorkspaceRoot(path);

		// The workspace's microservices and extensions, as they are now: a new one runs without setup.
		var found = await WorkspaceServices.Find(args, LocalStackConfigIO.Load(path));
		Log.Information($"Found {found.services.Count} microservice(s) and {found.extensions.Count} portal extension(s) in this workspace.");

		void LoadAndResolve()
		{
			config = LocalStackConfigIO.Load(path);
			found.AddTo(config.steps);
			if (!string.IsNullOrWhiteSpace(args.host)) config.host = args.host;
			if (!string.IsNullOrWhiteSpace(args.portalUrl)) config.portalUrl = args.portalUrl;
			config.javaHome = ResolveJavaHome(args, config);

			// A variable that refers back to itself can't be resolved, and every step using it would fail on its own.
			var cycles = LocalStackConfigIO.FindVariableCycles(config);
			if (cycles.Count > 0)
				throw new CliException("The settings' variables can't be resolved:\n  " + string.Join("\n  ", cycles));

			try
			{
				resolved = stacks.Resolve(args.stack, config);
			}
			catch (LocalStackStackException e)
			{
				throw new CliException(e.Message);
			}

			fromDefaults = ApplyStackVariables(resolved, config, path,
				canAsk: !args.dryRun && !args.Quiet && AnsiConsole.Profile.Capabilities.Interactive);

			// The backend it runs against: a remote target's host replaces the settings' one for this run, so
			// ${host} (the beam steps' --host, the portal's API base) follows it. --host still wins.
			try
			{
				target = LocalStackTargets.Get(resolved.target, config);
			}
			catch (LocalStackStackException e)
			{
				throw new CliException(e.Message);
			}

			// For local itself that is the same host; for lan, the same backend at this machine's LAN address.
			if (string.IsNullOrWhiteSpace(args.host))
				config.host = target.host;
			_targetName = string.IsNullOrWhiteSpace(resolved.target) ? LocalStackTargets.Local : resolved.target;
		}

		LoadAndResolve();

		// A stack already running here is changed to this name instead of being started a second time, which would
		// fight the running one for its ports, or making you stop everything to add or drop a few steps. One
		// attached to another terminal owns its services, so it is asked to do it. A detached one is changed by this
		// run: what the new name drops stops, what it adds starts, the rest stays up; and it stays detached.
		var runStatePath = LocalStackRunStateIO.ResolveRunStatePath(path);
		var runningBoard = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runStatePath));
		if (!args.dryRun && runningBoard is { attached: true } && !LocalStackBoardIO.IsStale(runningBoard)
		    && runningBoard.runnerPid != Environment.ProcessId)
		{
			Log.Information($"'{runningBoard.stack}' is running in another terminal (pid {runningBoard.runnerPid}); changing it to '{args.stack}'...");
			var answer = await LocalStackControl.Ask(runStatePath, new LocalStackControlRequest { action = "adjust", stack = args.stack },
				TimeSpan.FromMinutes(30), args.Lifecycle.CancellationToken);
			if (answer == null)
				throw new CliException($"The stack in the other terminal (pid {runningBoard.runnerPid}) didn't answer. Is it still starting? Try again once it's up.");
			if (!answer.ok)
				throw new CliException(answer.message);
			Log.Information(answer.message);
			return;
		}

		var previousRun = args.dryRun ? null : LocalStackRunStateIO.Load(runStatePath);
		var stillRunning = (previousRun?.steps ?? new List<LocalStackRunEntry>())
			.Where(e => LocalStackLiveness.Check(e, LocalStackStopCommand.BuildKillTokens(e)) != LocalStackLiveness.Liveness.Stopped)
			.Select(e => e.name)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		if (stillRunning.Count > 0 && !args.runDetached)
		{
			Log.Information("A detached stack is running here, so this changes it, and it stays detached.");
			args.runDetached = true;
		}

		bool UsesTag(string tag) => resolved.steps.Any(s => stacks.TagsOf(s).Contains(tag, StringComparer.OrdinalIgnoreCase));

		// What the stack needs: tools, generated config files, the portal's env, variables. A dry run prints it all;
		// a real run sets up whatever is missing before anything starts.
		var checks = await LocalStackPreflight.Check(config, resolved.steps, stacks, withCliPins: args.dryRun);

		// What the stack's steps need and it doesn't run. `beam local create` adds it; a stack written by hand, or one
		// whose base changed, can still miss it, and then a step fails in a way that doesn't say why (a C# host
		// without Mongo, a Scala service without dbflake's ids).
		var missingSteps = LocalStackDependencies.Missing(resolved.steps, config.steps, stacks.TagsOf, LocalStackTargets.RunsLocal(resolved.target, config));
		if (missingSteps.Count > 0)
			checks.Add(new LocalStackDependencyCheck
			{
				name = "dependencies", ok = false, warning = true,
				detail = $"'{args.stack}' doesn't run {string.Join("; ", missingSteps.Select(m => $"{m.name} ({m.reason})"))}. "
				         + $"Add them with `beam local edit {args.stack.Split(':')[0]}`."
			});
		if (!args.dryRun)
		{
			var missing = checks.Where(c => !c.ok && c.setupId != null).Select(c => c.setupId).Distinct().ToList();
			if (missing.Count > 0)
			{
				Log.Information($"'{args.stack}' needs setup first: {string.Join(", ", missing)}.");

				// A tool that gets installed has to be wired into the settings too, which is the settings step's job.
				if (missing.Any(id => ToolchainPins.ToolIds.Contains(id)))
					missing.Add(ToolchainPins.Settings);

				var setup = await RunSetup(args, missing);
				if (!setup.allOk)
					throw new CliException($"Setup couldn't finish, so '{args.stack}' wasn't started. Fix the rows marked failed above, then run it again.");

				LoadAndResolve();
			}

			foreach (var check in checks.Where(c => !c.ok && c.setupId == null))
				Log.Warning($"{check.name}: {check.detail}");

			// Keep the portal workspace's CLI pins on this CLI before anything restores or builds against them.
			LocalStackCliVersions.Report(
				LocalStackCliVersions.Check(config.repos?.portalDir, LocalStackCliVersions.RunningVersion(), fix: true));
		}

		// A build step that declares an output runs even without :build when that output is missing: the run step
		// after it launches a PRE-BUILT binary, so without it the run would retry a nonexistent executable
		// `readyRetries` times and then report the stack as up minus that service. Mirrors the reference
		// scripts/run-local-stack.sh, which builds each gateway/worker binary when it isn't there.
		//
		// It also builds when what it compiles changed since its last successful build. Without that, a stack kept
		// running stale binaries after a pull until someone remembered :build, which rebuilds everything (a cold
		// Scala reactor build alone is ~15 minutes). Now, after a pull or an
		// edit, the C# hosts rebuild (incrementally, in seconds) and the Scala reactor only when BeamableBackend
		// changed. Everything else shows as up to date.
		_buildsPath = LocalStackBuilds.PathFor(path);
		_builds = LocalStackBuilds.Load(_buildsPath);
		var buildReasons = new Dictionary<LocalStackStep, string>();
		var upToDate = new HashSet<LocalStackStep>();
		if (!resolved.forceBuild)
		{
			var gitAnswers = new Dictionary<string, string>();
			foreach (var s in resolved.steps.Where(s => s.enabled && s.build))
			{
				var reason = LocalStackConfigIO.BuildOutputMissing(s, config)
					? "output missing"
					: LocalStackBuilds.WhyBuild(s, config, _builds, gitAnswers);
				if (reason != null) buildReasons[s] = reason;
				else if (LocalStackBuilds.IsUpToDate(s, config, _builds, gitAnswers)) upToDate.Add(s);
			}
		}

		var autoBuild = buildReasons.Keys.ToHashSet();
		_buildReasons = buildReasons;

		if (UsesTag("scala"))
		{
			// The per-service launcher's `mvn -o dependency:build-classpath` needs Maven to resolve the `dependency`
			// plugin PREFIX offline — which requires the maven-dependency-plugin's plugin-group metadata to be cached.
			// On a machine where that was never cached (or was cached as a miss), the offline resolve dies with "No
			// plugin found for prefix 'dependency'" even with core present — and `:build` does NOT fix it, because the
			// reactor resolves every plugin by explicit version and never touches the prefix metadata. Probe it, and
			// prime it online once if it is missing. Not on a dry run, which changes nothing.
			if (!args.dryRun)
				EnsureMavenDependencyPluginResolvableOffline(config, resolved.forceBuild);
		}

		var steps = SelectSteps(resolved, autoBuild);

		// The extensions install the local web packages only while a stack that points them there runs; a run
		// without it drops what an earlier one left, so they install their locked versions again.
		_localWebPackages = steps.Any(s => string.Equals(s.name, LocalStackTemplate.WebInstallStepName, StringComparison.OrdinalIgnoreCase));
		if (!_localWebPackages && _workspaceRoot != null && File.Exists(WebPackagesService.OverlayPath(_workspaceRoot)))
			File.Delete(WebPackagesService.OverlayPath(_workspaceRoot));

		// Changing a running detached stack: stop what the new name drops, and skip a build whose services all still
		// run. Rebuilding a running host fails on its locked files, and the running service would keep the old code
		// anyway; `beam local restart` rebuilds it.
		var buildsOfRunning = new HashSet<LocalStackStep>();
		if (stillRunning.Count > 0)
		{
			var dropped = previousRun.steps.Where(e => stillRunning.Contains(e.name)
			                                           && !resolved.steps.Any(s => string.Equals(s.name, e.name, StringComparison.OrdinalIgnoreCase)))
				.Select(e => e.name)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
			if (dropped.Count > 0)
			{
				LocalStackStopCommand.Stop(runStatePath, previousRun, e => dropped.Contains(e.name), purge: false);
				Log.Information($"Stopped {string.Join(", ", dropped)}: '{args.stack}' doesn't run them.");
			}

			// A build several services share (the Scala reactor, the microservices) is skipped as soon as one of them
			// still runs: it would rewrite files that one holds open. The others start on what is built, as StartAll does.
			foreach (var build in steps.Where(s => s.build).ToList())
			{
				var users = resolved.steps.Where(s => !s.build && LocalStackControl.BuildsFor(s, resolved.steps, stacks.TagsOf).Contains(build)).ToList();
				var running = users.Where(u => stillRunning.Contains(u.name)).Select(u => u.name).ToList();
				if (running.Count == 0) continue;

				steps.Remove(build);
				buildsOfRunning.Add(build);
				// A microservice builds itself when it starts; the shared build is only for starting them all together.
				if (running.Count < users.Count && build.name != LocalStackTemplate.MicroservicesBuildStepName)
					Log.Warning($"[{build.name}] skipped: {string.Join(", ", running)} still run on it; restart them together to rebuild.");
			}
		}
		if (args.dryRun)
		{
			var dryRun = DryRun(resolved, steps, autoBuild, config, fromDefaults, checks, buildReasons, upToDate);
			PrintDryRun(dryRun);
			this.SendResults<LocalStackDryRunChannel, LocalStackDryRun>(dryRun);
			return;
		}

		if (steps.Count == 0)
		{
			throw new CliException($"'{args.stack}' has no steps to run on this machine.");
		}

		// A remote target needs its login before anything starts: asked for once in the terminal, then saved for
		// this workspace's stacks. The local realm can only be logged into once the backend is up (see below).
		// A backend whose services serve live traffic (prod) is only used when someone says so, every time.
		if (target.confirm == true)
		{
			var question = $"Run '{resolved.name}' against {resolved.target} ({target.host})? The services you start here serve that realm's live traffic.";
			if (!args.confirmTarget)
			{
				if (args.Quiet || !AnsiConsole.Profile.Capabilities.Interactive)
					throw new CliException($"'{resolved.name}' runs against {resolved.target} ({target.host}). Pass --confirm-target to run it without a terminal.");
				if (!AnsiConsole.Confirm(question, false))
					throw new CliException("Not started.");
			}
		}

		if (target.local != true)
		{
			_targetLogin = await LocalStackTargets.EnsureRemote(_workspaceRoot, resolved.target, target,
				canAsk: !args.Quiet && AnsiConsole.Profile.Capabilities.Interactive, args.Lifecycle.CancellationToken);
			Log.Information($"Running against {resolved.target}: {_targetLogin.host}, cid {_targetLogin.cid}, pid {_targetLogin.pid}.");
		}

		// Say WHY a build is running when nobody asked for one, so it never looks like a mystery step. The per-step
		// reason also rides along on the stream message when it launches (see the loop below).
		foreach (var s in steps.Where(s => autoBuild.Contains(s) && !resolved.requested.Contains(s)))
		{
			var unbuilt = LocalStackConfigIO.FirstUnbuiltScalaModule(s, config);
			Log.Information(unbuilt != null
				? $"[{s.name}] '{unbuilt}' has never been compiled. Building the reactor once so the services have classes to run."
				: $"[{s.name}] building: {Reason(s)} (add :no-build to skip, :build to rebuild everything).");
		}

		// A clean build should re-resolve deps too: drop the shared Scala classpath cache so each service's
		// launch rebuilds it (the cache otherwise only refreshes when core/pom.xml changes).
		if (resolved.forceBuild && UsesTag("scala"))
			TryDeleteDir(Path.Combine(Path.GetTempPath(), "beam-scala-cp"));

		// Fail fast if the stack needs Docker but its daemon isn't running — otherwise the first docker step
		// dies mid-bring-up with a cryptic daemon error.
		EnsureDockerRunning(steps);

		// Free the Mongo host port if a foreign process is squatting 127.0.0.1:27015. Docker publishes mongo_master on 0.0.0.0/[::], but a specific
		// 127.0.0.1 bind shadows it for `localhost` connections, timing out the gateway + every scala service.
		FreeMongoPortIfSquatted(steps);

		// Fail fast if Windows has reserved a port the compose files publish — docker otherwise dies mid-bring-up on
		// an error that names no cause and no owner, and the stack gets rolled back.
		var reservedDockerPorts = LocalStackPortGuard.DescribeReservedDockerPorts(steps);
		if (reservedDockerPorts != null)
			throw new CliException(reservedDockerPorts);

		// Fail fast (or auto-repair) when the product checkout is missing web/ or beam-portal-toolkit/. Without
		// this, the stack builds, launches every docker/scala/c# service, and only THEN fails at web: build —
		// cascading a full shutdown minutes in. The wipe is common enough (an unrelated `git clean`,
		// a manual delete) that the recovery is worth automating.
		if (steps.Any(s => LocalStackTemplate.IsWebStep(s.name)))
		{
			WebLocalRegistryService.EnsureProductDirIntact(config.repos?.productDir);
		}

		// Resolve how to invoke this same beam CLI for `beam: true` steps, and the workspace to run them from.
		var (beamExe, beamLeading) = ResolveBeam(args);
		var beamWorkspaceFallback = args.ConfigService?.BeamableWorkspace
		                            ?? args.ConfigService?.WorkingDirectory
		                            ?? Directory.GetCurrentDirectory();

		// Default is attached: stream each step's output live and stop the stack when `up` exits. --run-detached
		// keeps the file-based model (services survive `up`, managed via ps/logs/stop).
		var attached = !args.runDetached;

		// A unique per-run dir (temp by default, workspace under --save-logs) holding launcher scripts and — in
		// detached or --save-logs runs — the log files. Unique paths mean a fresh run never collides with a log a
		// leftover wrapper from a crashed run still holds open.
		var logsDir = LocalStackRunStateIO.ResolveRunLogsDir(path, args.saveLogs);
		Directory.CreateDirectory(logsDir);

		// Idempotency + run-state are only meaningful for detached stacks (they outlive `up`). Attached runs are
		// always fresh and torn down on exit, so they carry nothing over and write no run-state.
		var aliveByName = new Dictionary<string, LocalStackRunEntry>(StringComparer.OrdinalIgnoreCase);
		LocalStackRunState runState = null;
		if (args.runDetached)
		{
			// Carry over any steps from a previous detached `up` that are still running (alive pid), and skip
			// re-launching them below — lets you re-run `up` (e.g. to add the portal) without restarting the
			// Scala backend, and avoids duplicate processes fighting over the same ports.
			var existing = LocalStackRunStateIO.Load(runStatePath);
			if (existing?.steps != null)
			{
				foreach (var e in existing.steps)
				{
					// Identity-checked, not just pid-alive: a recycled pid used to read as "already running" and
					// silently cost the stack that service — see LocalStackLiveness.
					var liveness = LocalStackLiveness.Check(e, LocalStackStopCommand.BuildKillTokens(e));
					if (liveness == LocalStackLiveness.Liveness.Stopped)
						continue;

					// Unverified carries over: relaunching a service that IS still up would fight it for its port
					// (and duplicate a JVM), which is worse than skipping a service that has actually died — the
					// next `up` re-checks, and `ps` shows it.
					if (liveness == LocalStackLiveness.Liveness.Unverified)
						Log.Verbose($"[{e.name}] pid {e.pid} could not be confirmed; assuming it is still running.");

					aliveByName[e.name] = e;
				}
			}
			runState = new LocalStackRunState
			{
				host = config.host, portalUrl = config.portalUrl,
				logsDir = logsDir, ephemeralLogs = !args.saveLogs,
				steps = aliveByName.Values.ToList()
			};
			LocalStackRunStateIO.Save(runStatePath, runState);
		}

		// Clear temp launcher/log dirs left by previous (crashed/detached) runs, keeping this run's dir and any
		// dir a carried-over live service still logs to. Keeps the temp folder from growing across runs.
		PruneStaleTempLogs(path, logsDir, aliveByName.Values.Select(e => Path.GetDirectoryName(e.stdoutLog)));

		var launched = new List<Launched>();
		using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(args.Lifecycle.CancellationToken, args.cancel);
		var token = cancelled.Token;
		// A remote target is already logged into; only the local realm is ensured once the backend is up.
		var realmEnsured = target.local != true;

		// Port pre-flight, BEFORE anything is launched and before the try/catch below. A squatted port is
		// otherwise invisible until the service loses the bind and shows up as a readiness timeout, a Caddy 502,
		// or dependents failing to fetch dbids — so it is worth stopping for. It runs here, not in the launch
		// loop, for two reasons: nothing has started yet (so aborting cannot orphan half a stack, nor clear the
		// run-state that is the only record of carried-over services still running from a previous `up`), and
		// every conflict is reported at once instead of one per run.
		await EnsurePortsFree(steps, config, aliveByName, token);

		// Attached: put every launched process in a kill-on-close job so the whole stack dies with the CLI —
		// even on a terminal close / IDE stop / hard kill that skips the graceful teardown (Windows only; no-op
		// elsewhere). Held for the command's lifetime; disposed in the teardown paths below.
		var job = attached ? LocalStackJobObject.CreateKillOnClose() : null;

		// The board: one row per step, live in a terminal, a line per change without one, and a file `ps` reads.
		// While it shows, the runner's own progress messages would only repeat it, so only warnings and errors
		// print (unless --logs asked for more).
		_attached = attached;
		_verbose = args.verbose;
		var boardPath = LocalStackBoardIO.PathFor(runStatePath);
		// Every step the stack resolves to has a row, the build steps that don't need to build too, as up to date.
		_board = new LocalStackBoard(new LocalStackBoardSnapshot
		{
			stack = resolved.name, runnerPid = Environment.ProcessId, attached = attached,
			host = config.host, portalUrl = config.portalUrl, startedUtc = DateTime.UtcNow, logsDir = logsDir,
			rows = resolved.steps.Where(s => steps.Contains(s) || s.build).Select(s => new LocalStackBoardRow
			{
				name = s.name, label = LocalStackCreateCommand.Label(s), port = s.port,
				timeoutSeconds = s.readyTimeoutSeconds, once = s.waitForExit || s.build, changedUtc = DateTime.UtcNow
			}).ToList()
		}, BoardMode(args), boardPath);
		foreach (var skipped in resolved.steps.Where(s => s.build && !steps.Contains(s)))
			_board.Set(skipped.name, LocalStackStepState.UpToDate,
				buildsOfRunning.Contains(skipped) ? "its services are running (restart them to rebuild)"
				: skipped.inputs != null ? "unchanged since the last build" : "output is there");

		var logSwitch = args.AppContext?.LogSwitch;
		var quietedLogs = Showing && logSwitch != null && logSwitch.Level == LogLevel.Information;
		if (quietedLogs) logSwitch.Level = LogLevel.Warning;
		// In an attached terminal the board stays up once the stack is, and its keys act on it: Space on a service
		// offers to restart, stop or start it, through the same requests another terminal sends (set up below, once
		// they can be answered).
		Func<string, string, Task<string>> boardAction = null;
		var keepBoard = attached && _board.Mode == LocalStackBoardMode.Live;
		var live = _board.ShowLive(token,
			keepBoard ? (action, step) => boardAction?.Invoke(action, step) ?? Task.FromResult("The stack can't take that yet.") : null,
			keepBoard ? LocalStackBoardView.ActionKeys : null);

		// Takes the board off the screen (once) and brings the logs back, before anything is printed about the end.
		async Task EndBoard()
		{
			_board.Complete();
			try { await live; }
			catch { /* the board never fails the run */ }

			if (quietedLogs) logSwitch.Level = LogLevel.Information;
			quietedLogs = false;
		}

		// Launch a step and record it in the run-state (upsert by name, so a retry replaces the dead entry
		// rather than duplicating it). Returned as a local function so readiness can relaunch on early exit.
		Launched LaunchAndRegister(LocalStackStep step)
		{
			var l = StartStep(step, config, beamExe, beamLeading, beamWorkspaceFallback, logsDir, attached, args.saveLogs, job);
			lock (_launchedLock)
			{
				launched.Add(l);

				// Build steps compile a component and exit; they are not a running service, so they are not
				// recorded in the run-state (nothing for ps/stop to track). AwaitStep still waits on completion.
				if (step.build)
				{
					Log.Information($"[{step.name}] building (pid={SafePid(l)})");
					return l;
				}

				// Attached runs keep nothing across invocations — no run-state to write.
				if (attached)
				{
					Log.Information($"[{step.name}] started (pid={SafePid(l)})");
					return l;
				}

				var entry = runState.steps.FirstOrDefault(e => e.name == step.name);
				if (entry == null)
				{
					entry = new LocalStackRunEntry { name = step.name };
					runState.steps.Add(entry);
				}

				entry.group = step.group;
				entry.pid = SafePid(l);
				// Pin the pid to this exact process, so a later `up`/`ps`/`stop` can tell it apart from whatever
				// recycles that pid number after the stack dies.
				entry.startedAtUtcTicks = LocalStackLiveness.StartTicksOf(entry.pid);
				entry.matchToken = LocalStackConfigIO.Substitute(step.mainClass, config);
				entry.kind = l.Kind;
				entry.stdoutLog = l.StdoutLog;
				entry.stderrLog = l.StderrLog;
				entry.workingDirectory = l.WorkingDirectory;
				// Substituted, like the stop arguments below: `beam local stop` runs this command directly,
					// so an unresolved ${...} token would reach the shell verbatim.
					entry.command = LocalStackConfigIO.Substitute(step.command, config);
				entry.stopArguments = LocalStackConfigIO.Substitute(step.stopArguments, config);
 				entry.purgeStopArguments = LocalStackConfigIO.Substitute(step.purgeStopArguments, config);
				entry.waitForExit = step.waitForExit;
				LocalStackRunStateIO.Save(runStatePath, runState);
			}

			Log.Information($"[{step.name}] started (pid={SafePid(l)}), logs: {l.StdoutLog}");
			return l;
		}

		// Requests from another terminal while the stack runs attached: restart, start or stop steps, stop the whole
		// stack, or change it to another stack name. Its services are children of this process, so only it can stop
		// and relaunch them without orphaning them; before this, restarting one service meant stopping the whole stack.
		var current = steps.ToList();

		Launched Latest(LocalStackStep step)
		{
			lock (_launchedLock)
				return launched.LastOrDefault(l => string.Equals(l.Step.name, step.name, StringComparison.OrdinalIgnoreCase));
		}

		bool IsRunning(LocalStackStep step)
		{
			var l = Latest(step);
			return l != null && !l.Step.waitForExit && !l.ExitedTask.IsCompleted;
		}

		void Say(LocalStackStep step)
		{
			var row = _board.Row(step.name);
			if (row != null && _board.Mode != LocalStackBoardMode.Silent)
				AnsiConsole.MarkupLine($"[grey]{DateTime.Now:HH:mm:ss}[/]  {Markup.Escape(row.label ?? row.name)}: "
				                       + $"[{LocalStackBoard.Colour(row.state)}]{LocalStackBoard.Text(row.state)}[/]"
				                       + (string.IsNullOrWhiteSpace(row.detail) ? string.Empty : $" [grey]({Markup.Escape(row.detail)})[/]"));
		}

		void StopRunning(LocalStackStep step, string why)
		{
			var l = Latest(step);
			if (l == null || l.Step.waitForExit || l.ExitedTask.IsCompleted) return;
			KillLaunched(l);
			l.ExitedTask.Wait(TimeSpan.FromSeconds(15));
			_board.Set(step.name, LocalStackStepState.Stopped, why, announce: false);
			Say(step);
		}

		// Starts steps again: first each one's build check (a build whose inputs changed or whose output is missing
		// builds, unless another service still running uses it), then the steps, run-once steps in order and
		// services together, each waiting for its readiness like at startup. <paramref name="starting"/> cancelled stops
		// starting services (a newer restart of the same steps replaced this one); builds always finish.
		async Task StartAll(List<LocalStackStep> toStart, string source, CancellationToken starting)
		{
			// The backend may have come back since the login failed: try again before starting anything that logs in.
			if (_noLogin != null && target.local == true && toStart.Any(NeedsLogin))
			{
				foreach (var step in toStart.Where(NeedsLogin))
					Send(step.name, "starting", LoggingIn, 1f, LocalStackStepState.Starting, LoggingIn);
				await EnsureRealmAndLogin(args, config, steps);
			}
			foreach (var step in toStart.Where(s => NeedsLogin(s) && _noLogin != null))
				NoLogin(step, 1f);
			toStart = toStart.Where(s => !NeedsLogin(s) || _noLogin == null).ToList();

			var builds = toStart.SelectMany(s => LocalStackControl.BuildsFor(s, resolved.steps, stacks.TagsOf)).Distinct().ToList();
			var reasons = new Dictionary<LocalStackStep, string>();
			foreach (var build in builds)
			{
				var why = LocalStackConfigIO.BuildOutputMissing(build, config) ? "output missing" : LocalStackBuilds.WhyBuild(build, config, _builds);
				if (why == null) continue;

				var users = resolved.steps.Where(s => !s.build && LocalStackControl.BuildsFor(s, resolved.steps, stacks.TagsOf).Contains(build));
				var others = users.Where(u => !toStart.Contains(u) && IsRunning(u)).Select(u => u.name).ToList();
				if (others.Count > 0)
				{
					// A microservice builds itself when it starts; the shared build is only for starting them all together.
					if (build.name != LocalStackTemplate.MicroservicesBuildStepName)
						Log.Warning($"[{build.name}] {why}, but {string.Join(", ", others)} still run on it; restart them together to rebuild.");
					continue;
				}

				reasons[build] = why;
			}

			var rebuilds = RunInLanes(BuildLanes(reasons.Keys, config), async build =>
			{
				_board.AddRows(new[] { new LocalStackBoardRow { name = build.name, label = LocalStackCreateCommand.Label(build), once = true, changedUtc = DateTime.UtcNow } });
				Send(build.name, "starting", reasons[build], 1f, LocalStackStepState.Building, reasons[build]);
				await AwaitStep(build, LaunchAndRegister(build), config, 1f, current.Count, token, null);
				Say(build);
			});
			await Task.WhenAll(rebuilds.Values);

			// Services start paced like at startup (StartLimit, StartSpacing); a run-once step waits for those before it.
			var services = new List<LocalStackStep>();
			async Task StartServices()
			{
				if (services.Count == 0) return;
				var limit = services.Select(s => StartLimit(s.group)).Where(n => n > 0).DefaultIfEmpty(0).Min();
				await RunPooled(services, limit, s => StartSpacing(s.group), step =>
				{
					Send(step.name, "starting", "restarting", 1f, LocalStackStepState.Restarting, source);
					return Task.FromResult(AwaitStep(step, LaunchAndRegister(step), config, 1f, current.Count, starting, () => LaunchAndRegister(step)));
				}, starting);
				foreach (var step in services) Say(step);
				services.Clear();
			}

			foreach (var step in toStart)
			{
				if (!step.waitForExit)
				{
					services.Add(step);
					continue;
				}

				await StartServices();
				starting.ThrowIfCancellationRequested();
				await AwaitStep(step, LaunchAndRegister(step), config, 1f, current.Count, token, null);
				Say(step);
			}

			await StartServices();
		}

		async Task<LocalStackControlResponse> Adjust(string stackName, LocalStackControlResponse response)
		{
			LocalStackResolvedStack next;
			try
			{
				next = stacks.Resolve(stackName, config);
			}
			catch (LocalStackStackException e)
			{
				return new LocalStackControlResponse { ok = false, message = e.Message };
			}

			if (!string.Equals(next.target, resolved.target, StringComparison.OrdinalIgnoreCase))
				return new LocalStackControlResponse
				{
					ok = false,
					message = $"'{stackName}' runs against {next.target}, and the running stack against {resolved.target}. Stop it first (Ctrl+C in its terminal, or `beam local stop`)."
				};

			bool In(IEnumerable<LocalStackStep> list, LocalStackStep step) =>
				list.Any(s => string.Equals(s.name, step.name, StringComparison.OrdinalIgnoreCase));

			var drop = current.Where(s => !s.build && !In(next.steps, s)).Reverse().ToList();
			foreach (var step in drop) StopRunning(step, $"not in {stackName}");
			_board.RemoveRows(drop.Select(s => s.name));

			var add = next.steps.Where(s => !s.build && !In(current, s)).ToList();
			_board.AddRows(add.Select(s => new LocalStackBoardRow
			{
				name = s.name, label = LocalStackCreateCommand.Label(s), port = s.port, timeoutSeconds = s.readyTimeoutSeconds,
				once = s.waitForExit, changedUtc = DateTime.UtcNow
			}));

			resolved = next;
			current = next.steps.Where(s => !s.build || In(current, s)).ToList();
			_board.Rename(stackName);
			await StartAll(add, "added to the stack", token);

			response.steps = add.Concat(drop).Select(s => s.name).ToList();
			response.message = $"Now running {stackName}: started {(add.Count == 0 ? "nothing" : string.Join(", ", add.Select(s => s.name)))}; "
			                   + $"stopped {(drop.Count == 0 ? "nothing" : string.Join(", ", drop.Select(s => s.name)))}; the rest stayed up.";
			AnsiConsole.MarkupLine(Markup.Escape(response.message));
			return response;
		}

		// The restart or start under way, while it is still starting its steps (see Controlled).
		(List<LocalStackStep> steps, CancellationTokenSource cancel)? startingNow = null;

		// The services a restart or start names: nothing named means every service.
		List<LocalStackStep> Named(LocalStackControlRequest request) =>
			request.items.Count == 0
				? current.Where(s => !s.build && !s.waitForExit).ToList()
				: LocalStackControl.Match(request.items, current, stacks.TagsOf);

		bool Covers(LocalStackControlRequest request, List<LocalStackStep> steps)
		{
			try
			{
				var named = Named(request);
				return steps.All(named.Contains);
			}
			catch (CliException)
			{
				return false;
			}
		}

		// <paramref name="source"/> is who asked, for the board: another terminal, or a key on this one's board.
		async Task<LocalStackControlResponse> Control(LocalStackControlRequest request, string source)
		{
			var response = new LocalStackControlResponse { id = request.id, ok = true };
			switch (request.action)
			{
				case "stop" when request.items.Count == 0:
					response.message = "Stopping the whole stack.";
					return response;
				case "stop":
				{
					var matched = LocalStackControl.Match(request.items, current, stacks.TagsOf);
					foreach (var step in Enumerable.Reverse(matched)) StopRunning(step, "stopped " + source);
					response.steps = matched.Select(s => s.name).ToList();
					response.message = $"Stopped {string.Join(", ", response.steps)}.";
					return response;
				}
				case "start":
				case "restart":
				{
					// Nothing named (`restart --all`): every service. Run-once steps aren't stopped, so docker's
					// containers stay up.
					var matched = Named(request);
					var toStart = request.action == "start" ? matched.Where(s => !IsRunning(s)).ToList() : matched;
					if (request.action == "restart")
					{
						foreach (var step in Enumerable.Reverse(matched)) StopRunning(step, "restarting");
					}

					using (var starting = CancellationTokenSource.CreateLinkedTokenSource(token))
					{
						startingNow = (toStart, starting);
						try
						{
							await StartAll(toStart, source, starting.Token);
						}
						catch (OperationCanceledException) when (!token.IsCancellationRequested)
						{
							response.steps = toStart.Select(s => s.name).ToList();
							response.message = $"A newer {request.action} of the same steps replaced this one.";
							return response;
						}
						finally
						{
							startingNow = null;
						}
					}

					response.steps = toStart.Select(s => s.name).ToList();
					response.message = toStart.Count == 0
						? "Nothing to start: all of it is running."
						: $"{(request.action == "restart" ? "Restarted" : "Started")} {string.Join(", ", response.steps)}.";
					return response;
				}
				case "adjust":
					return await Adjust(request.stack, response);
				default:
					return new LocalStackControlResponse { ok = false, message = $"Unknown request '{request.action}'." };
			}
		}

		// Requests from another terminal and keys on this one's board are done one at a time, so the two never act on
		// the same steps at once.
		var controlGate = new SemaphoreSlim(1, 1);
		async Task<LocalStackControlResponse> Controlled(LocalStackControlRequest request, string source)
		{
			// A restart of every step one in progress is still starting replaces it, rather than waiting minutes behind
			// it (`web: watch` asks again when a newer build lands): that one stops starting more, and this one starts all.
			var inProgress = startingNow;
			if (inProgress != null && request.action is "restart" or "start" && Covers(request, inProgress.Value.steps))
				inProgress.Value.cancel.Cancel();
			await controlGate.WaitAsync(token);
			try
			{
				return await Control(request, source);
			}
			finally
			{
				controlGate.Release();
			}
		}

		boardAction = async (action, step) =>
			(await Controlled(new LocalStackControlRequest { action = action, items = { step } }, "from the board")).message;

		// Record a step this `up` did NOT launch because it was already answering its readiness endpoint, so the
		// stack it is part of can still be stopped as a whole.
		//
		// Without this the step becomes an untracked orphan: `stop` brings down everything it launched, honestly
		// reports "0 still recorded", and leaves the carried-over process running. That is not just untidy — the
		// survivor holds its build outputs open, so the NEXT `run <stack>:build` dies in a build step
		// (MSB3021 "Unable to copy file ...dll" for the .NET hosts, maven-clean "Failed to delete ...jar" for a
		// Scala service), and a failed build step tears the whole stack down. Observed end to end on 2026-08-13.
		//
		// This also makes the two skip paths consistent: a step skipped because the RUN-STATE says it is alive
		// stays recorded and stoppable, so a step skipped because HTTP says it is alive should be too.
		//
		// No pid is recorded: the owning process was never a child of this `up`, and guessing one from the port
		// would be wrong exactly where it matters (a docker step's port is held by Docker's proxy, not by the
		// container). `stop` needs none — docker steps reverse via `stopArguments` (`compose stop`), and the rest
		// are found by the token sweep in `KillTree`, which is gated to stack runtime images and this step's own
		// identity string.
		void AdoptAlreadyServing(LocalStackStep step)
		{
			// Build steps are never recorded (nothing to stop), and attached runs keep no run-state at all.
			if (step.build || attached)
				return;

			lock (_launchedLock)
			{
				var entry = runState.steps.FirstOrDefault(e => e.name == step.name);
				if (entry == null)
				{
					entry = new LocalStackRunEntry { name = step.name };
					runState.steps.Add(entry);
				}

				entry.group = step.group;
				entry.pid = 0;
				entry.startedAtUtcTicks = 0;
				entry.matchToken = LocalStackConfigIO.Substitute(step.mainClass, config);
				entry.kind = ResolveKind(step);
				// This run launched nothing, so it produced no logs for the step. Leave any paths a previous run
				// recorded intact rather than pointing `beam local logs` at a file this run never wrote.
				entry.workingDirectory = LocalStackConfigIO.Substitute(step.workingDirectory, config);
				entry.command = step.command;
				entry.stopArguments = LocalStackConfigIO.Substitute(step.stopArguments, config);
				entry.purgeStopArguments = LocalStackConfigIO.Substitute(step.purgeStopArguments, config);
				entry.waitForExit = step.waitForExit;
				entry.adopted = true;
				LocalStackRunStateIO.Save(runStatePath, runState);
			}
		}

		// The step a failed step leaves without what it needs: its build, or a step it depends on (by name or tag).
		// Null when nothing it needs failed.
		var localBackend = LocalStackTargets.RunsLocal(resolved.target, config);
		// Against this machine's backend the workspace's services send their own logs and traces to the local
		// collector, so they land in Grafana next to the C# hosts'; against a remote one they keep Beamable's. Not with
		// :no-logs, which leaves the stack's logs out of Grafana.
		if (localBackend && steps.Any(s => s.name == LocalStackTemplate.LogsForwardStepName))
		{
			foreach (var step in steps.Where(IsWorkspaceService))
				step.environment = WithLocalTelemetry(step.environment);
		}
		string FailedNeed(LocalStackStep step)
		{
			var needs = LocalStackControl.BuildsFor(step, steps, stacks.TagsOf).Select(b => b.name)
				.Concat(LocalStackDependencies.Needs(step, localBackend).SelectMany(need =>
					steps.Where(s => string.Equals(s.name, need, StringComparison.OrdinalIgnoreCase)
					                 || stacks.TagsOf(s).Contains(need, StringComparer.OrdinalIgnoreCase)).Select(s => s.name)));
			return needs.FirstOrDefault(n => !string.Equals(n, step.name, StringComparison.OrdinalIgnoreCase) && _failed.ContainsKey(n));
		}

		// Marks a step that logs in as unable to start while there is no login to the local realm.
		void NoLogin(LocalStackStep step, float baseProgress)
		{
			_failed[step.name] = "no login";
			var why = $"can't start: no login to the local realm ({_noLogin})";
			Send(step.name, "failed", why, baseProgress, LocalStackStepState.Failed, "can't start: no login", hint: why);
			if (!Showing) Log.Warning($"[{step.name}] {why}");
		}

		// Marks a step that can't start because something it needs failed; the steps that need it are skipped in turn.
		void CantStart(LocalStackStep step, string failedNeed, float baseProgress)
		{
			_failed[step.name] = $"{failedNeed} failed";
			Send(step.name, "failed", $"can't start: {failedNeed} failed", baseProgress, LocalStackStepState.Failed, $"can't start: {failedNeed} failed");
			if (!Showing) Log.Warning($"[{step.name}] can't start: {failedNeed} failed.");
		}

		// What a step's start says, and why an unrequested build builds: a build nobody asked for is surprising unless
		// it says why, and the stream channel is what the user (and the Unity/portal tooling) actually sees.
		string Launching(LocalStackStep step, int idx, out string why)
		{
			var launching = $"launching ({idx + 1}/{steps.Count})";
			why = null;
			if (autoBuild.Contains(step) && !resolved.requested.Contains(step) && !resolved.forceBuild)
			{
				why = Reason(step);
				launching += $" — {why}, so it builds";
			}

			return launching;
		}

		// Builds start with the run instead of at their place in the stack, one lane per folder (see BuildLanes), so
		// the C# hosts, the Scala reactor and the portal's dependencies compile at the same time, and alongside the
		// docker deps coming up. The loop below still waits for each build at its place, so a service never starts
		// before its own build, and one whose build failed is skipped. An unexpected error in a lane (not a failed
		// build) stops the bring-up at once rather than when the loop gets to it.
		var buildFailed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
		var buildReached = steps.Where(s => s.build)
			.ToDictionary(s => s, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
		using var stopBuilds = CancellationTokenSource.CreateLinkedTokenSource(token);
		var buildTasks = new Dictionary<LocalStackStep, Task>();

		async Task Build(LocalStackStep build)
		{
			try
			{
				if (WaitsForItsPlace(build))
					await buildReached[build].Task.WaitAsync(stopBuilds.Token);
				stopBuilds.Token.ThrowIfCancellationRequested();

				var idx = steps.IndexOf(build);
				var baseProgress = (float)idx / steps.Count;

				if (FailedNeed(build) is { } failedNeed)
				{
					CantStart(build, failedNeed, baseProgress);
					return;
				}

				// A build the stack asks for outright (by name or the build tag) still doesn't build when what it
				// compiles hasn't changed since its last build.
				if (!resolved.forceBuild && resolved.requested.Contains(build) && upToDate.Contains(build))
				{
					Send(build.name, "ready", "unchanged since the last build", baseProgress, LocalStackStepState.UpToDate,
						"unchanged since the last build");
					return;
				}

				var blocked = CannotStartReason(build, config);
				if (blocked != null)
				{
					Send(build.name, "failed", blocked, baseProgress, LocalStackStepState.Failed, "can't start", hint: blocked);
					if (!Showing) Log.Warning($"[{build.name}] {blocked}");
					return;
				}

				Send(build.name, "starting", Launching(build, idx, out var why), baseProgress, LocalStackStepState.Building, why);
				await AwaitStep(build, LaunchAndRegister(build), config, baseProgress, steps.Count, token, null);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				buildFailed.TrySetResult(e);
				throw;
			}
		}

		try
		{
			var lanes = BuildLanes(steps, config);
			if (lanes.Count > 1)
				Log.Information("Building in parallel: " + string.Join(", ", lanes.Select(l =>
					Path.GetFileName(LaneOf(l[0], config)) + (l.Count > 1 ? $" ({l.Count})" : string.Empty))) + ".");
			buildTasks = RunInLanes(lanes, Build);

			var i = 0;
			while (i < steps.Count)
			{
				token.ThrowIfCancellationRequested();

				// Gather the next batch: a run of consecutive steps sharing a non-empty group is launched and
				// awaited in parallel; an ungrouped step is a batch of one (sequential).
				var groupName = steps[i].group;
				var batch = new List<int> { i };
				if (!string.IsNullOrEmpty(groupName))
				{
					while (i + 1 < steps.Count && steps[i + 1].group == groupName)
						batch.Add(++i);
				}
				i++;

				var limit = StartLimit(groupName);
				if (batch.Count > 1)
					Log.Information($"Starting {batch.Count} '{groupName}' steps in parallel{(limit > 0 && batch.Count > limit ? $", {limit} at a time" : string.Empty)}.");

				// What has to happen one step at a time (its checks, the login, the launch); returns what to wait on for
				// it to be up, or null when nothing was started.
				async Task<Task> StartOne(int idx)
				{
					var step = steps[idx];
					var baseProgress = (float)idx / steps.Count;

					// A build already started in its lane: its place in the stack is where the steps after it wait
					// for it, and where one that waits for its place may start.
					if (buildTasks.TryGetValue(step, out var buildTask))
					{
						buildReached[step].TrySetResult();
						return buildTask;
					}

					// Something it needs failed (its build, the docker deps, a seed): it can't work, so it's marked and skipped.
					if (FailedNeed(step) is { } failedNeed)
					{
						CantStart(step, failedNeed, baseProgress);
						return null;
					}

					// Already running from a previous `up`? Leave it alone.
					if (aliveByName.TryGetValue(step.name, out var running))
					{
						Send(step.name, "running", $"already running (pid={running.pid}) — skipping", baseProgress + 1f / steps.Count,
							LocalStackStepState.Ready, "already running");
						Log.Information($"[{step.name}] already running (pid={running.pid}) — skipping.");
						return null;
					}

					// Already serving on its HTTP readiness endpoint (e.g. a gateway/portal from a previous
					// session, the IDE, or a stray process)? Don't launch a conflicting duplicate that would
					// fail to bind the port and hang at "still starting". Adopt it into the run-state so it is
					// still part of the stack `stop` takes down — see AdoptAlreadyServing.
					if (await AlreadyServing(step, config, token))
					{
						AdoptAlreadyServing(step);
						Send(step.name, "running", "already serving — skipping launch", baseProgress + 1f / steps.Count,
							LocalStackStepState.Ready, "already serving");
						Log.Information($"[{step.name}] already serving at its readiness endpoint — skipping launch " +
						                "(adopted into the run-state, so `beam local stop` will still stop it).");
						return null;
					}

					// Can this step even start? A missing working directory (a build output folder whose build
					// failed or timed out, or an unedited `<EDIT: ...>` placeholder) means it cannot. Report the
					// step as failed and keep going: one unusable step is not a reason to tear down a stack that
					// is otherwise coming up, and the old behavior — launching it anyway — just produced a raw
					// "not recognized" several retries later.
					var blocked = CannotStartReason(step, config);
					if (blocked != null)
					{
						Send(step.name, "failed", blocked, baseProgress, LocalStackStepState.Failed, "can't start", hint: blocked);
						if (!Showing) Log.Warning($"[{step.name}] {blocked}");
						return null;
					}

					// Ensure a valid local realm/login before the first beam step — microservices and
					// extensions authenticate against the local backend on startup. The web package steps don't
					// log in, and `:web` runs them before the backend is up, so they don't trigger it.
					if (NeedsLogin(step) && !realmEnsured)
					{
						realmEnsured = true;
						// It waits for the backend to answer, which can take minutes after a slow start: say so, or
						// the row sits on "waiting" as if the run were stuck.
						Send(step.name, "starting", LoggingIn, baseProgress, LocalStackStepState.Starting, LoggingIn);
						await EnsureRealmAndLogin(args, config, steps);
					}

					if (NeedsLogin(step) && _noLogin != null)
					{
						NoLogin(step, baseProgress);
						return null;
					}

					Send(step.name, "starting", Launching(step, idx, out var why), baseProgress,
						step.build ? LocalStackStepState.Building : LocalStackStepState.Starting, why);

					var stepToRun = step;
					var l = LaunchAndRegister(stepToRun);
					return AwaitStep(stepToRun, l, config, baseProgress, steps.Count, token,
						() => LaunchAndRegister(stepToRun));
				}

				// Launches in a group are spaced out (StartSpacing), so ~34 JVMs don't storm the Mongo port-proxy at
				// once. A build failing in its lane ends the bring-up now, even while this batch is still waiting on
				// something else.
				var batchDone = RunPooled(batch, limit, _ => StartSpacing(groupName), StartOne, token);
				if (await Task.WhenAny(batchDone, buildFailed.Task) != batchDone)
					System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(buildFailed.Task.Result);
				await batchDone;
			}

			// Detached only: on Windows the tracked pid is the cmd.exe wrapper, not the service (cmd → powershell
			// → java). Rewrite each recorded pid to the real service leaf so `ps`/`stop` (and the next `up`'s
			// idempotency check) act on the JVM instead of a wrapper that will die. Attached keeps no run-state.
			if (args.runDetached)
				ReconcileLeafPids(runState, runStatePath, launched);

			// No beam steps ran the hook above — still ensure/validate the realm once the backend is up.
			if (!realmEnsured)
				await EnsureRealmAndLogin(args, config, steps);

			// From here the board says where the stack is. An attached terminal keeps it up for its keys; otherwise it
			// closes and the summary says it instead.
			_board.MarkUp();
			if (!keepBoard) await EndBoard();
			Send(string.Empty, "running",
				$"Stack is up. Backend={config.host} ({resolved.target}) Portal={config.portalUrl}.", 1f);
			if (!Showing)
				Log.Information($"Stack is up. Backend={config.host}  Portal={config.portalUrl}");
			if (!_failed.IsEmpty && !Showing)
				Log.Warning($"{_failed.Count} step(s) failed and the stack carried on without them: "
				            + string.Join(", ", _failed.Select(f => $"{f.Key} ({f.Value})")) + ". `beam local logs <step>` shows why.");
			else if (!keepBoard)
				PrintSummary(config, args.runDetached);

			if (args.runDetached)
			{
				// Fire-and-return (docker compose up -d style). NOTE: don't use this from an IDE run-config —
				// when this process exits the IDE ends the run session and kills the child process tree.
				if (!Showing)
					Log.Information("Detached — the stack keeps running. Manage it with: beam local ps | beam local logs | beam local stop");
			}
			else
			{
				// Attached (default): hold the foreground until the user stops (Ctrl+C) or all long-running services
				// exit, then tear the whole stack down. With --verbose the children's output keeps streaming.
				if (!Showing)
					Log.Information("Attached — streaming logs. Press Ctrl+C to stop the stack.");
				try { await WaitAttached(launched, steps, config, token, r => Controlled(r, "from another terminal"), runStatePath); }
				catch (OperationCanceledException) { /* Ctrl+C — fall through to teardown */ }
				await EndBoard();
				Log.Information("Stopping the local stack...");
				TearDown(launched);
				if (!args.saveLogs) TryDeleteDir(logsDir);
			}
		}
		catch (OperationCanceledException)
		{
			stopBuilds.Cancel();
			await EndBoard();
			if (args.runDetached)
			{
				// Detached: cancellation during bring-up leaves the stack running; run-state lets `stop` bring it down.
				Send(string.Empty, "running", "detached — stack left running (use `beam local stop`)", 1f);
				Log.Information("Detached — stack left running. Use `beam local stop` to bring it down.");
			}
			else
			{
				// Attached: Ctrl+C during bring-up tears down what started so nothing is left orphaned.
				Log.Information("Stopping the local stack...");
				TearDown(launched);
				if (!args.saveLogs) TryDeleteDir(logsDir);
			}
		}
		catch (Exception)
		{
			// A genuine bring-up failure: tear down what we started to avoid orphans, and clear detached run-state.
			// No lane starts another build once the teardown is under way.
			stopBuilds.Cancel();
			await EndBoard();
			TearDown(launched);
			if (args.runDetached) LocalStackRunStateIO.Clear(runStatePath);
			// Don't leave an empty temp log dir behind for a run that never came up.
			if (!args.saveLogs) TryDeleteDir(logsDir);
			throw;
		}
		finally
		{
			// Closing the job kills anything still assigned (attached only) — belt-and-suspenders on top of the
			// OS's own kill-on-close when the process exits.
			job?.Dispose();

			// An attached stack is gone with this command, so its board is too; a detached one's lasts until `stop`.
			if (attached) LocalStackBoardIO.Clear(boardPath);
		}
	}

	/// <summary>
	/// How the board reaches the person: full screen in a terminal, a line per change without one, nothing when the output is
	/// piped as data (the result stream carries the states), and nothing with <c>--verbose</c>, which streams the
	/// steps' own output instead.
	/// </summary>
	private static LocalStackBoardMode BoardMode(LocalStackRunCommandArgs args)
	{
		var app = args.AppContext;
		if (args.verbose || (app != null && (app.UsePipeOutput || app.ShowRawOutput) && !app.ShowPrettyOutput))
			return LocalStackBoardMode.Silent;

		// The full-screen board needs the terminal's alternate screen; a terminal without one gets the lines.
		var capabilities = AnsiConsole.Profile.Capabilities;
		return capabilities.Interactive && capabilities.Ansi && capabilities.AlternateBuffer
			? LocalStackBoardMode.Live
			: LocalStackBoardMode.Lines;
	}

	/// <summary>
	/// What replaces the board once the stack is up: how many steps are ready, where the portal and the backend are,
	/// each step that failed with why, and how to stop or manage the stack. After this only warnings and errors print.
	/// </summary>
	private void PrintSummary(LocalStackConfig config, bool detached)
	{
		var rows = _board.Rows;
		var failed = rows.Where(r => r.state == LocalStackStepState.Failed).ToList();
		LocalStackStyle.H1($"Stack {_board.Name} is up", LocalStackBoard.Summary(rows));
		AnsiConsole.MarkupLine($"  {LocalStackStyle.H3("Portal")}   {Markup.Escape(config.portalUrl ?? string.Empty)}");
		AnsiConsole.MarkupLine($"  {LocalStackStyle.H3("Backend")}  {Markup.Escape(config.host ?? string.Empty)}"
		                       + (_targetLogin != null ? $" [grey](cid {Markup.Escape(_targetLogin.cid ?? "?")}, pid {Markup.Escape(_targetLogin.pid ?? "?")})[/]" : string.Empty));
		if (failed.Count > 0 || rows.Any(r => r.state == LocalStackStepState.Degraded))
			LocalStackStyle.H2("Needs a look");
		foreach (var row in failed.Concat(rows.Where(r => r.state == LocalStackStepState.Degraded)))
			AnsiConsole.MarkupLine($"  [{LocalStackBoard.Colour(row.state)}]{row.state}[/] {Markup.Escape(row.label ?? row.name)}"
			                       + (string.IsNullOrWhiteSpace(row.detail) ? string.Empty : $" [grey]({Markup.Escape(row.detail)})[/]"));

		AnsiConsole.MarkupLine(detached
			? "[grey]It keeps running. `beam local ps` shows it, `beam local logs <step>` reads a step, `beam local stop` stops it.[/]"
			: "[grey]Ctrl+C stops the stack. `beam local ps` in another terminal shows this board.[/]");
	}

	/// <summary>
	/// Attached mode: hold the foreground while logs stream, until the user cancels (Ctrl+C) or every
	/// long-running service has exited on its own. Throws <see cref="OperationCanceledException"/> on cancel
	/// so the caller tears the stack down.
	///
	/// The container watchdog runs alongside the wait but is never a reason to return — a dead container is
	/// reported and restarted, it does not end the run. Attached only: detached <c>up</c> returns immediately
	/// and has no foreground to poll from.
	/// </summary>
	private async Task WaitAttached(List<Launched> launched, List<LocalStackStep> steps, LocalStackConfig config,
		CancellationToken token, Func<LocalStackControlRequest, Task<LocalStackControlResponse>> control, string runStatePath)
	{
		// Stops the watchdog when the wait ends for any reason, so it can't outlive the run it is watching.
		using var watchdogStop = CancellationTokenSource.CreateLinkedTokenSource(token);
		var watchdog = WatchDockerContainers(steps, config, watchdogStop.Token);

		try
		{
			while (true)
			{
				token.ThrowIfCancellationRequested();

				// Requests from another terminal (beam local restart/start/stop, or run of another name), one at a time.
				foreach (var request in LocalStackControl.Take(runStatePath))
				{
					LocalStackControlResponse response;
					try
					{
						response = await control(request);
					}
					catch (Exception e) when (e is not OperationCanceledException)
					{
						response = new LocalStackControlResponse { ok = false, message = e.Message };
					}

					response.id = request.id;
					LocalStackControl.Answer(runStatePath, response);
					if (response.ok && request.action == "stop" && request.items.Count == 0)
						return;
				}

				// Every service down (exited, or stopped from another terminal): the run is over. With none ever
				// started, only Ctrl+C ends it.
				List<Launched> services;
				lock (_launchedLock)
					services = launched.Where(l => !l.Step.waitForExit)
						.GroupBy(l => l.Step.name, StringComparer.OrdinalIgnoreCase)
						.Select(g => g.Last())
						.ToList();
				if (services.Count > 0 && services.All(l => l.ExitedTask.IsCompleted))
					return;

				await Task.Delay(1000, token);
			}
		}
		finally
		{
			watchdogStop.Cancel();
			try { await watchdog; } catch { /* the watchdog never fails the run */ }
		}
	}

	/// <summary>
	/// Polls the containers this stack's <c>docker compose up</c> steps started, for as long as <c>up</c> is
	/// attached. Docker steps are run-to-completion, so nothing else in the command ever looks at their
	/// containers again — one can die mid-run and the only symptom is a rising tide of connection errors under
	/// an already-printed "Stack is up". This reports each death with its cause and re-runs the owning step.
	///
	/// See <see cref="LocalStackDockerWatchdog"/> for the failure this was written against.
	/// </summary>
	private async Task WatchDockerContainers(List<LocalStackStep> steps, LocalStackConfig config,
		CancellationToken token)
	{
		var interval = config?.dockerWatchdogSeconds ?? 0;
		if (interval <= 0) return;

		var targets = LocalStackDockerWatchdog.DiscoverTargets(steps, config);
		if (targets.Count == 0) return;

		if (!DockerPathOption.TryGetDockerPath(out var dockerPath, out _))
			return; // the stack already failed EnsureDockerRunning if it needed docker

		// Reporting is per container (so every casualty is named), but restarting is per step — one
		// `compose up` brings the step's whole project back, so a Docker-VM teardown that kills 13 containers
		// must not fire 13 restarts.
		var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var restarts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var gaveUp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		while (!token.IsCancellationRequested)
		{
			try { await Task.Delay(TimeSpan.FromSeconds(interval), token); }
			catch (OperationCanceledException) { return; }

			foreach (var target in targets)
			{
				if (token.IsCancellationRequested) return;

				var containers = LocalStackDockerWatchdog.Poll(target, dockerPath);

				// Classify the whole poll at once — the first one also seals this project's liveness baseline.
				var dead = target.liveness.Dead(containers);

				// A container that came back clears its mark, so a later crash is reported afresh.
				foreach (var alive in containers.Except(dead))
					reported.Remove(alive.name);

				foreach (var container in dead.Where(c => reported.Add(c.name)))
					ReportDeadContainer(target, container, dockerPath);

				if (dead.Count > 0)
					TryRestartTarget(target, dead, config, dockerPath, restarts, gaveUp);
			}
		}
	}

	/// <summary>Reports one dead container with its exit code, the reason, and the tail of its own log.</summary>
	private void ReportDeadContainer(LocalStackDockerWatchdog.Target target,
		LocalStackDockerWatchdog.ContainerState container, string dockerPath)
	{
		var headline = $"[{target.step.name}] container '{container.name}' is {container.state} " +
		               $"(exit {container.exitCode}) — {container.Explain()}";

		Log.Error(headline);
		Send(target.step.name, "failed", headline, 1f, LocalStackStepState.Failed, $"container {container.name} died", announce: false);

		var tail = LocalStackDockerWatchdog.LogTail(dockerPath, container.name, 20);
		if (!string.IsNullOrWhiteSpace(tail))
			Log.Error($"[{target.step.name}] last output from '{container.name}':{Environment.NewLine}{tail}");
	}

	/// <summary>
	/// Re-runs one step's <c>docker compose up</c> to bring its dead containers back, up to the step's attempt
	/// budget. Budgeted per step, not per container, because a single <c>compose up</c> recovers the step's whole
	/// project — a Docker-VM teardown kills every container at once and must not fire one restart per casualty.
	///
	/// Restarting containers is not the same as recovering the stack: the Scala/C# services hold connections to
	/// whatever just died and do not re-establish them, which is why a mid-run <c>mongo_master</c> crash is fatal
	/// in practice. So say that, rather than implying the stack is healed.
	/// </summary>
	private void TryRestartTarget(LocalStackDockerWatchdog.Target target,
		List<LocalStackDockerWatchdog.ContainerState> dead, LocalStackConfig config, string dockerPath,
		Dictionary<string, int> restarts, HashSet<string> gaveUp)
	{
		var step = target.step.name;
		if (gaveUp.Contains(step))
			return; // already reported as beyond automatic recovery — don't repeat it every poll

		var budget = LocalStackDockerWatchdog.RestartAttemptsFor(target.step);
		restarts.TryGetValue(step, out var used);

		if (used >= budget)
		{
			gaveUp.Add(step);
			Log.Error($"[{step}] its containers have been restarted {used}/{budget} times and are still dying — " +
			          "giving up on them. Fix the underlying crash, then run the stack again.");
			return;
		}

		restarts[step] = used + 1;
		var names = string.Join(", ", dead.Select(c => c.name));
		Log.Warning($"[{step}] restarting (attempt {used + 1}/{budget}) to recover {names}: " +
		            $"docker {target.step.arguments}");

		if (!LocalStackDockerWatchdog.Restart(target, dockerPath, config, out var error))
		{
			Log.Error($"[{step}] restart failed: {error}");
			return;
		}

		Log.Warning($"[{step}] containers restarted, but the services that were already connected to them still " +
		            "hold dead connections and will not recover on their own — stop this run (Ctrl+C) and " +
		            "run the stack again.");
	}

	/// <summary>
	/// Fails the whole command — before anything has been launched — when a step's declared port is already held
	/// by something that is not this stack. Steps carried over from a previous detached `up`, and steps whose
	/// readiness endpoint is already answering, are skipped: those are OUR service on that port, and the launch
	/// loop skips them for the same reason. Every conflict is collected so one run reports them all.
	/// </summary>
	private async Task EnsurePortsFree(List<LocalStackStep> steps, LocalStackConfig config,
		Dictionary<string, LocalStackRunEntry> aliveByName, CancellationToken token)
	{
		var conflicts = new List<string>();
		foreach (var step in steps.Where(s => s.port > 0))
		{
			token.ThrowIfCancellationRequested();
			if (aliveByName.ContainsKey(step.name))
			{
				continue; // our own service from a previous `up` owns that port
			}

			if (!LocalStackPortGuard.IsPortTaken(step.port))
			{
				continue;
			}

			// Taken — but by us? A gateway/portal left running outside the run-state answers its own readiness
			// endpoint, and the launch loop will skip launching a duplicate, so that is not a conflict.
			if (await AlreadyServing(step, config, token))
			{
				continue;
			}

			var conflict = LocalStackPortGuard.DescribeConflict(step.name, step.port);
			if (conflict != null)
			{
				conflicts.Add(conflict);
			}
		}

		if (conflicts.Count == 0)
		{
			return;
		}

		foreach (var conflict in conflicts)
		{
			Send(string.Empty, "failed", conflict, 0f);
		}

		throw new CliException(string.Join(Environment.NewLine, conflicts)
		                       + Environment.NewLine
		                       + "Nothing was started, so no part of the stack was touched.");
	}

	/// <summary>
	/// Why a step cannot be launched at all, or null when it can. Only the working directory is checked: every
	/// non-<c>beam</c> step names a real directory (a repo, or a build output folder), and when it is absent the
	/// launcher would silently fall back to the CLI's own cwd and fail with a raw "not recognized". <c>beam</c>
	/// steps are exempt — <see cref="StartStep"/> substitutes the workspace for them.
	/// </summary>
	private static string CannotStartReason(LocalStackStep step, LocalStackConfig config)
	{
		if (step.beam)
		{
			return null;
		}

		var workDir = LocalStackConfigIO.Substitute(step.workingDirectory, config);
		if (string.IsNullOrEmpty(workDir) || Directory.Exists(workDir))
		{
			return null;
		}

		// Deliberately does NOT tell the user to pass :build: when this is a build output folder, its build step
		// has already run (or been filtered out), so the honest advice is to look at why it produced nothing.
		return $"working directory '{workDir}' does not exist, so this step cannot start. If it is a build "
		       + "output folder, its build step failed, timed out, or was skipped (see `beam local logs`); "
		       + "otherwise fix the path in the settings (or recreate them with `beam local setup --only settings --force`)";
	}

	/// <summary>Best-effort recursive delete of a directory (used to clean up ephemeral temp log dirs).</summary>
	private static void TryDeleteDir(string dir)
	{
		try { if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
		catch { /* best-effort */ }
	}

	/// <summary>
	/// Deletes stale <c>run-*</c> temp log dirs for this workspace, so temp logs from previous crashed or
	/// detached runs don't accumulate. Keeps <paramref name="currentLogsDir"/> (if any) and any dir in
	/// <paramref name="keepDirs"/> (the log dirs of carried-over live services). Best-effort — a dir still
	/// held open by a leftover wrapper is skipped rather than failing the run. Returns how many it deleted.
	/// </summary>
	public static int PruneStaleTempLogs(string manifestPath, string currentLogsDir, IEnumerable<string> keepDirs)
	{
		var deleted = 0;
		try
		{
			var baseDir = LocalStackRunStateIO.ResolveTempLogsBase(manifestPath);
			if (!Directory.Exists(baseDir)) return 0;

			var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var d in keepDirs.Append(currentLogsDir))
				if (!string.IsNullOrEmpty(d)) keep.Add(Path.GetFullPath(d));

			foreach (var d in Directory.GetDirectories(baseDir))
			{
				if (keep.Contains(Path.GetFullPath(d))) continue;
				try
				{
					Directory.Delete(d, recursive: true);
					deleted++;
				}
				catch { /* a live run or leftover wrapper may still hold a handle — skip it */ }
			}
		}
		catch { /* best-effort — never fail bring-up over log cleanup */ }

		return deleted;
	}

	/// <summary>
	/// Reports one update on the result stream. With <paramref name="state"/>, the step's board row moves there too;
	/// a failure passes its last log lines (<paramref name="tail"/>) and where the rest is (<paramref name="hint"/>),
	/// which the board prints unless <paramref name="announce"/> is false (something else already said it).
	/// </summary>
	private void Send(string step, string status, string message, float progress, LocalStackStepState? state = null,
		string detail = null, IReadOnlyList<string> tail = null, string hint = null, bool announce = true)
	{
		LocalStackBoardRow row = null;
		if (_board != null && !string.IsNullOrEmpty(step))
		{
			if (state != null)
				_board.Set(step, state.Value, detail, tail, hint, announce);
			row = _board.Row(step);
		}

		SendResults(new LocalStackRunResultStream
		{
			step = step, status = status, message = message, progressRatio = Math.Clamp(progress, 0f, 1f),
			label = row?.label, state = row?.state.ToString(), elapsedSeconds = row?.ElapsedSeconds(DateTime.UtcNow) ?? 0
		});
	}

	/// <summary>Why a build step builds this run, for the board and the logs.</summary>
	private string Reason(LocalStackStep step) => _buildReasons != null && _buildReasons.TryGetValue(step, out var reason)
		? reason
		: "needed by the services that run on it";

	private Dictionary<LocalStackStep, string> _buildReasons;

	/// <summary>The last lines a failed step wrote, from whichever of its two log files it wrote to last.</summary>
	private static List<string> Tail(Launched l, int count = 6)
	{
		var file = new[] { l.StdoutLog, l.StderrLog }
			.Where(f => !string.IsNullOrEmpty(f) && File.Exists(f) && new FileInfo(f).Length > 0)
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.FirstOrDefault();
		if (file == null) return new List<string>();

		return ReadTail(file, 8192).Split('\n').Select(x => x.TrimEnd('\r')).Where(x => !string.IsNullOrWhiteSpace(x))
			.TakeLast(count).Select(x => Trim(x, 200)).ToList();
	}

	/// <summary>Where a step's whole log is: <c>beam local logs</c> for a detached stack, the file for an attached one
	/// (kept with --save-logs; the run's temp folder goes when the run ends).</summary>
	private string LogHint(Launched l) =>
		_attached ? $"log: {l.StdoutLog}" : $"beam local logs \"{l.Step.name}\"";

	/// <summary>
	/// Runs once, before the first beam step of a stack on the local backend: reuses the local target's saved login
	/// while its token still works, and otherwise creates the local realm (or logs into it when it exists) and saves
	/// that login under <c>.beamable/local/targets/local/</c>. The stack's beam steps get it as environment
	/// overrides; the workspace's own <c>.beamable</c> login is never changed. When the stack runs the C# gateway, the
	/// org's realms are then bound to the zones dev, staging and prod. Never aborts the stack — realm issues
	/// are surfaced, not fatal.
	/// </summary>
	private async Task EnsureRealmAndLogin(LocalStackRunCommandArgs args, LocalStackConfig config, IReadOnlyList<LocalStackStep> steps)
	{
		try
		{
			var token = args.Lifecycle.CancellationToken;
			var saved = await LocalStackTargets.Saved(_workspaceRoot, _targetName, config.host, token);
			if (saved != null)
			{
				// A login saved on another realm (prod, when only the games' roots were listed) moves to the dev realm;
				// its token is the org's, so only the realm changes.
				var realm = LocalStackTargets.LocalRealm(await LocalStackTargets.Realms(config.host, saved, token));
				if (realm != null && realm.Pid != saved.pid)
				{
					Log.Information($"The saved local login was on realm {saved.pid}; using {realm.Pid} ({realm.ProjectName}).");
					saved.pid = realm.Pid;
					LocalStackTargets.Save(_workspaceRoot, _targetName, saved);
				}

				_targetLogin = saved;
				_noLogin = null;
				Log.Information("Local login OK.");
				await EnsureZones(args, config, steps, saved, token);
				return;
			}

			// None yet, or it stopped working (e.g. the realm was wiped by a docker cleanup). By default, create the
			// realm so the microservices/extensions can authenticate; --no-create-realm turns this into a warning.
			if (args.noCreateRealm)
			{
				Log.Warning("There is no working login to the local realm (it may have been wiped). Drop --no-create-realm to create it, or log in to it.");
				_noLogin = "--no-create-realm, and the saved one doesn't work";
				Send(string.Empty, "running", "local login invalid — create skipped (--no-create-realm)", 1f);
				return;
			}

			Log.Information("Local login is invalid — ensuring the local realm (create if missing, else log in)...");
			var opts = new RealmSeedOptions
			{
				customerName = args.realmCustomer ?? "beam",
				projectName = args.realmProject ?? "beam-project",
				email = args.realmEmail ?? "beam@beamable.com",
				alias = args.realmAlias ?? "beam-project",
				password = args.realmPassword ?? "123456",
			};
			var login = await LocalRealmService.EnsureRealmAsync(config.host, opts, token);
			LocalStackTargets.Save(_workspaceRoot, _targetName, login);
			_targetLogin = login;
			_noLogin = null;
			Send(string.Empty, "running", $"local realm ready cid={login.cid} pid={login.pid}", 1f);
			await EnsureZones(args, config, steps, login, token);
		}
		catch (Exception e)
		{
			// Don't tear down an otherwise-healthy stack over a realm/login problem — surface it and continue.
			Log.Warning($"Realm/login setup issue: {e.Message}");
			_noLogin = e.Message;
		}
	}

	/// <summary>
	/// Binds the local org's realms to the zones dev, staging and prod (see <see cref="LocalStackTargets.EnsureZones"/>)
	/// when the stack runs the C# gateway, which serves the zone calls. A login whose token may not change zones is
	/// replaced, for these calls only, by the org's password login. A failure is a warning.
	/// </summary>
	private static async Task EnsureZones(LocalStackRunCommandArgs args, LocalStackConfig config, IReadOnlyList<LocalStackStep> steps,
		LocalStackTargetLogin login, CancellationToken token)
	{
		if (!steps.Any(s => string.Equals(s.name, LocalStackDependencies.GatewayStep, StringComparison.OrdinalIgnoreCase))) return;
		try
		{
			try
			{
				await LocalStackTargets.EnsureZones(config.host, login, token);
			}
			catch (CliException e) when (e.Message.Contains("(401)") || e.Message.Contains("(403)"))
			{
				var admin = await LocalStackTargets.Login(config.host, login.cid, args.realmEmail ?? "beam@beamable.com", args.realmPassword ?? "123456", token);
				await LocalStackTargets.EnsureZones(config.host, admin, token);
			}

			Log.Information($"Local zones ready: {string.Join(", ", LocalStackTargets.ZoneTiers)}, each bound to its realm.");
		}
		catch (Exception e) when (e is not OperationCanceledException)
		{
			Log.Warning($"Couldn't set up the local zones ({e.Message}). Zone-scoped services and extensions won't find a zone.");
		}
	}

	/// <summary>
	/// If any step launches docker, verify the Docker <b>daemon</b> is actually running (not just that the CLI
	/// binary exists) before we start anything, so bring-up fails fast with a clear message instead of the
	/// first `docker compose up` dying on a daemon-connection error mid-run.
	/// </summary>
	/// <summary>
	/// Host port the local stack's <c>mongo_master</c> container is published on — mirrors
	/// <c>BeamableAPI/docker-compose.yml</c> (<c>"27015:27017"</c>) and the <c>MongoDB:MainMongoHost</c> =
	/// <c>localhost:27015</c> that the gateway + scala services connect to. Not modeled in CLI config.
	/// </summary>
	private const int MongoHostPort = 27015;

	/// <summary>
	/// Delay between launches within a parallel group (the ~34-service "scala" batch). Launching them all at
	/// once storms Docker Desktop's Windows userspace port-proxy with simultaneous <c>localhost:27015</c> Mongo
	/// connects; a subset stall in CONNECTING, hit the driver's 5s serverSelectionTimeout, and hang (they never
	/// exit, so per-step <c>readyRetries</c> never re-fires). Staggering spreads the connect attempts so each
	/// completes well within the timeout. Readiness waits still overlap — only the launches are spaced out.
	/// </summary>
	private static readonly TimeSpan GroupLaunchStagger = TimeSpan.FromMilliseconds(2000);

	/// <summary>The microservice, portal extension and service group steps: <c>beam project run</c>s of this workspace.</summary>
	private static bool IsWorkspaceService(LocalStackStep step) =>
		new[] { LocalStackTemplate.MicroservicePrefix, LocalStackTemplate.ExtensionPrefix, LocalStackTemplate.GroupPrefix }
			.Any(p => (step.name ?? string.Empty).StartsWith(p, StringComparison.OrdinalIgnoreCase));

	/// <summary><paramref name="environment"/> plus what points a Beamable service's telemetry at the local collector
	/// (see <c>MicroserviceArgs</c>). Outside docker a service sets up no exporter without BEAM_LOCAL_OTEL, and that
	/// alone would also start Beamable's own collector (ClickHouse credentials from the backend); the standard setup is
	/// turned off, so the endpoint below gets it all. A value already set wins.</summary>
	public static Dictionary<string, string> WithLocalTelemetry(Dictionary<string, string> environment)
	{
		var result = new Dictionary<string, string>(environment ?? new Dictionary<string, string>());
		result.TryAdd("BEAM_LOCAL_OTEL", "true");
		result.TryAdd("BEAM_DISABLE_STANDARD_OTEL", "true");
		result.TryAdd("BEAM_OTEL_EXPORTER_OTLP_ENDPOINT", LocalStackTemplate.LocalOtlpEndpoint);
		result.TryAdd("BEAM_OTEL_EXPORTER_OTLP_PROTOCOL", "HttpProtobuf");
		return result;
	}

	/// <summary>What a step that logs in shows while the run logs in to the local realm before starting it.</summary>
	private const string LoggingIn = "logging in to the local realm (waits for the backend to answer)";

	/// <summary>
	/// How many steps of <paramref name="group"/> may be coming up at once; 0 for no limit. Portal extensions each
	/// install their dependencies and build a bundle before they are up, and dozens at once only slow each other down.
	/// </summary>
	public static int StartLimit(string group) =>
		group == LocalStackTemplate.ExtensionsGroup ? Math.Clamp(Environment.ProcessorCount / 3, 2, 10) : 0;

	/// <summary>The wait after launching a step of <paramref name="group"/> before the next. Extensions are paced by
	/// <see cref="StartLimit"/> already.</summary>
	public static TimeSpan StartSpacing(string group) =>
		group == LocalStackTemplate.ExtensionsGroup ? TimeSpan.FromMilliseconds(500) : GroupLaunchStagger;

	/// <summary>
	/// Starts <paramref name="items"/> in order and waits for all of them. <paramref name="start"/> does what has to
	/// happen one at a time and returns the task to wait on for that item (null when it started nothing). At most
	/// <paramref name="limit"/> of those are waited on at once (0: no limit), and after an item that started something
	/// the next waits <paramref name="spacing"/> for it.
	/// </summary>
	public static async Task RunPooled<T>(IReadOnlyList<T> items, int limit, Func<T, TimeSpan> spacing, Func<T, Task<Task>> start,
		CancellationToken token)
	{
		var all = new List<Task>();
		var running = new List<Task>();
		var wait = TimeSpan.Zero;
		foreach (var item in items)
		{
			token.ThrowIfCancellationRequested();
			while (limit > 0 && running.Count >= limit)
				running.Remove(await Task.WhenAny(running).WaitAsync(token));
			if (wait > TimeSpan.Zero) await Task.Delay(wait, token);

			var task = await start(item);
			wait = task != null ? spacing(item) : TimeSpan.Zero;
			if (task == null) continue;
			all.Add(task);
			running.Add(task);
		}

		await Task.WhenAll(all);
		// The readiness waits end early when cancelled, as if the steps were up: they weren't.
		token.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Inputs for the offline maven-dependency-plugin probe, resolved from the manifest by
	/// <see cref="PlanDependencyPluginProbe"/>. Public so the decision (should we probe at all, and against which
	/// module) can be unit-tested without launching Maven.
	/// </summary>
	public class MavenDependencyProbePlan
	{
		/// <summary>True when there is a Scala launch step and enough resolved inputs to run the probe.</summary>
		public bool shouldProbe;

		/// <summary>The <c>mvn</c> command the probe runs — the toolchain's pinned path, or bare <c>mvn</c>.</summary>
		public string mvnCommand;

		/// <summary>The BeamableBackend working directory the probe runs in.</summary>
		public string scalaDir;

		/// <summary>The module the probe targets (e.g. <c>tools/account</c>), mirroring the launcher's <c>-pl</c>.</summary>
		public string probeModule;

		/// <summary>When <see cref="shouldProbe"/> is false, the reason — for tests and tracing.</summary>
		public string skipReason;
	}

	/// <summary>
	/// The Maven error that — and only that — identifies the plugin-prefix trap
	/// <see cref="EnsureMavenDependencyPluginResolvableOffline"/> can actually fix by priming the plugin online.
	/// Every other probe failure has a different cause, so it must not trigger the prefix remediation.
	/// </summary>
	private const string MavenMissingPrefixMarker = "No plugin found for prefix";

	/// <summary>
	/// True when a <c>dependency:help</c> probe failed BECAUSE Maven could not resolve the plugin prefix — the one
	/// failure <see cref="BuildDependencyPluginRemediation"/> describes and the online prime can fix. Maven's
	/// wording is "No plugin found for prefix 'dependency' in the current project and in the plugin groups […]",
	/// so match only the stable leading phrase; a version that rewords the tail still matches. <c>-q</c> does not
	/// suppress <c>[ERROR]</c>, so the line is present. If it ever were not, this under-fires and the user falls
	/// back on the launcher's own empty-cache guard, which explains the same cause — a safe degradation, unlike
	/// over-firing, which aborts the whole stack with a false diagnosis. Public so it can be unit-tested against
	/// captured mvn output without launching Maven.
	/// </summary>
	public static bool IsPluginPrefixFailure(string output) =>
		!string.IsNullOrEmpty(output)
		&& output.Contains(MavenMissingPrefixMarker, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// True when <paramref name="step"/> is one of the host-JVM <c>scala: *</c> services — i.e. a step that really
	/// maps to a <c>tools/$SVC</c> Maven module and whose launcher runs the offline
	/// <c>mvn -o dependency:build-classpath</c>. Two signals, cheapest first:
	/// <list type="bullet">
	/// <item><c>shell</c> — the same narrowing the skill template applies to this prefix
	/// (<c>LocalStackSkillTemplate.AppendSelection</c>); <c>scala: redis</c> is a <c>command = docker</c> step.</item>
	/// <item>the launcher fingerprint <c>dependency:build-classpath</c> in <c>arguments</c> — the exact resolve the
	/// probe protects. It keeps a
	/// hand-edited manifest from pointing the probe at a module that never runs a classpath resolve.</item>
	/// </list>
	/// </summary>
	private static bool IsScalaServiceLauncher(LocalStackStep step)
	{
		if (step == null || !step.enabled || string.IsNullOrEmpty(step.name))
		{
			return false;
		}

		if (!step.name.StartsWith("scala: ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (!step.shell || string.IsNullOrEmpty(step.arguments))
		{
			return false;
		}

		return step.arguments.Contains("dependency:build-classpath", StringComparison.Ordinal);
	}

	/// <summary>
	/// Pure decision for <see cref="EnsureMavenDependencyPluginResolvableOffline"/>: reads the manifest and decides
	/// whether the offline maven-dependency-plugin probe should run, and against which module. No filesystem check
	/// (beyond string validity) and no process launch, so it is unit-testable. The probe is only relevant when a
	/// <c>scala: *</c> service will launch — those are the steps whose launcher runs <c>mvn -o
	/// dependency:build-classpath</c>. The module is derived from the launch step's name (<c>scala: account</c> →
	/// <c>tools/account</c>), exactly the <c>-pl tools/$SVC</c> the launcher uses.
	/// <para>
	/// The <c>scala: </c> prefix alone is NOT enough to pick that step: <c>scala: redis</c> shares the prefix but is
	/// a docker container the Scala services depend on, not one of them — there is no <c>tools/redis</c> module, and
	/// it sorts FIRST in every generated manifest because the JVMs need it up before they launch. Probing
	/// <c>-pl tools/redis</c> fails with "Could not find the selected project in the reactor", which has nothing to
	/// do with the plugin prefix this method exists to check. <see cref="IsScalaServiceLauncher"/> is the narrowing.
	/// </para>
	/// </summary>
	public static MavenDependencyProbePlan PlanDependencyPluginProbe(LocalStackConfig config)
	{
		var plan = new MavenDependencyProbePlan();

		if (config?.steps == null)
		{
			plan.skipReason = "no manifest steps";
			return plan;
		}

		var scalaLaunch = config.steps.FirstOrDefault(IsScalaServiceLauncher);
		if (scalaLaunch == null)
		{
			// Report the two misses apart, so a trace says whether this stack runs no Scala services at all or
			// only ones that own no Maven module — the second is the case that used to fabricate `tools/redis`.
			var anyScalaPrefixed = config.steps.Any(s =>
				s != null && s.enabled && !string.IsNullOrEmpty(s.name)
				&& s.name.StartsWith("scala: ", StringComparison.OrdinalIgnoreCase));
			plan.skipReason = anyScalaPrefixed
				? "no scala step that maps to a maven module"
				: "no scala launch step";
			return plan;
		}

		var svc = scalaLaunch.name.Substring("scala: ".Length).Trim();
		if (string.IsNullOrEmpty(svc))
		{
			plan.skipReason = "scala launch step has no service name";
			return plan;
		}
		plan.probeModule = "tools/" + svc;

		var scalaDir = config.repos?.scalaDir;
		if (string.IsNullOrWhiteSpace(scalaDir)
		    || scalaDir.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal))
		{
			plan.skipReason = "no BeamableBackend path on the manifest";
			return plan;
		}
		plan.scalaDir = scalaDir;

		var mvn = LocalStackConfigIO.Substitute("${maven}", config);
		if (string.IsNullOrWhiteSpace(mvn))
		{
			plan.skipReason = "no maven command resolved";
			return plan;
		}
		plan.mvnCommand = mvn;

		plan.shouldProbe = true;
		return plan;
	}

	/// <summary>
	/// Ensures the per-service launcher's offline <c>mvn -o dependency:build-classpath</c> can resolve the
	/// <c>dependency</c> plugin prefix. Probes it offline; if that fails, primes the plugin once ONLINE (which
	/// downloads the plugin-group metadata and clears any cached "not found" miss), re-probes, and — if it still
	/// cannot be made to work (no network, a blocked Maven repo) — throws with the exact manual fix. See the call
	/// site in <see cref="Handle"/> for why <c>:build</c> alone does not cover this.
	/// </summary>
	private static void EnsureMavenDependencyPluginResolvableOffline(LocalStackConfig config, bool forceBuild)
	{
		var plan = PlanDependencyPluginProbe(config);
		if (!plan.shouldProbe) return;

		// A manifest can name a BeamableBackend path that isn't checked out on this machine — nothing to run mvn in.
		if (!Directory.Exists(plan.scalaDir)) return;

		// The module has to be a real reactor module, or `-pl` fails with "Could not find the selected project in
		// the reactor" no matter what the plugin metadata looks like — a failure this method must not mistake for
		// the prefix trap. The check lives here rather than in PlanDependencyPluginProbe, which is deliberately
		// filesystem-free so the decision stays unit-testable.
		if (!File.Exists(Path.Combine(plan.scalaDir, plan.probeModule, "pom.xml"))) return;

		// Fast path: a previous run already wrote a non-empty classpath cache, which means the offline resolve
		// demonstrably works here — skip the maven probe so a healthy `up` pays nothing. `:build` wipes that cache
		// later in Handle, so always probe on a build: it is exactly the run where priming matters.
		if (!forceBuild && HasNonEmptyScalaClasspathCache()) return;

		var offline = RunMavenDependencyGoalProbe(config, plan, online: false);
		if (!offline.started) return;      // couldn't even launch mvn — let the normal launch path surface that
		if (offline.exitCode == 0) return; // resolves offline — healthy, nothing to do

		// A non-zero exit only means "this mvn invocation failed" — it is the OUTPUT that says whether the failure
		// is the prefix trap. Anything else (a broken pom, a missing module, a JDK mismatch) has its own cause and
		// its own fix, so priming the plugin cannot help and the prefix remediation below would be a lie. Surface
		// what Maven actually said and let the real launch report it in context.
		if (!IsPluginPrefixFailure(offline.output))
		{
			Log.Verbose($"[maven] the offline '{plan.probeModule}' dependency-plugin probe failed with exit " +
			            $"{offline.exitCode}, but not with \"{MavenMissingPrefixMarker}\" — so this is not the " +
			            "plugin-prefix trap and priming it online would not help. Continuing; the Scala launch " +
			            $"step will report the real cause.\n{offline.output.Trim()}");
			return;
		}

		Log.Information(
			"[maven] the Scala services resolve their classpath with an offline 'mvn dependency:build-classpath', " +
			"but Maven can't resolve the 'dependency' plugin prefix offline yet (its plugin metadata isn't cached). " +
			$"Priming it once online (mvn -U dependency:help in {plan.scalaDir}) so the services can launch — this " +
			"does not rebuild anything.");

		var online = RunMavenDependencyGoalProbe(config, plan, online: true);
		if (online.started && online.exitCode == 0)
		{
			var reprobe = RunMavenDependencyGoalProbe(config, plan, online: false);
			if (reprobe.started && reprobe.exitCode == 0)
			{
				Log.Information("[maven] maven-dependency-plugin is now resolvable offline — the Scala classpath " +
				                "caches will regenerate on launch.");
				return;
			}
		}

		throw new CliException(BuildDependencyPluginRemediation(plan));
	}

	/// <summary>Outcome of one <c>dependency:help</c> probe: whether mvn started at all, its exit code, and its
	/// combined output (for the remediation message).</summary>
	private readonly struct MvnProbeOutcome
	{
		public MvnProbeOutcome(bool started, int exitCode, string output)
		{
			this.started = started;
			this.exitCode = exitCode;
			this.output = output;
		}

		public readonly bool started;
		public readonly int exitCode;
		public readonly string output;
	}

	/// <summary>
	/// Runs <c>dependency:help</c> — the cheapest goal that still forces prefix resolution of
	/// maven-dependency-plugin, i.e. the exact resolution that fails as "No plugin found for prefix 'dependency'".
	/// Offline (<c>-o</c>) reproduces the launcher's own resolve; online adds <c>-U</c> to invalidate a cached
	/// "not found" miss and fetch the plugin-group metadata that makes subsequent offline resolves succeed. Runs
	/// under the same PATH/JAVA_HOME as the reactor so it hits the same <c>~/.m2</c> and JDK.
	/// </summary>
	private static MvnProbeOutcome RunMavenDependencyGoalProbe(LocalStackConfig config, MavenDependencyProbePlan plan, bool online)
	{
		var mvnArgs = new List<string>();
		if (!online) mvnArgs.Add("-o");
		mvnArgs.Add("-q");
		if (online) mvnArgs.Add("-U");
		mvnArgs.Add("-pl");
		mvnArgs.Add(plan.probeModule);
		mvnArgs.Add("dependency:help");

		var psi = new ProcessStartInfo
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = plan.scalaDir,
		};

		if (OperatingSystem.IsWindows())
		{
			// mvn is a .cmd batch file — Process.Start can't run it directly, so go through cmd.exe. The doubled
			// quotes are cmd's rule for "everything after /s /c is one command that carries its own quotes".
			psi.FileName = "cmd.exe";
			psi.Arguments = $"/s /c \"\"{plan.mvnCommand}\" {string.Join(" ", mvnArgs)}\"";
		}
		else
		{
			psi.FileName = plan.mvnCommand;
			foreach (var a in mvnArgs) psi.ArgumentList.Add(a);
		}

		ApplyToolchainEnvironment(psi, config);

		var output = new StringBuilder();
		try
		{
			var proc = Process.Start(psi);
			if (proc == null) return new MvnProbeOutcome(false, -1, string.Empty);

			proc.OutputDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
			proc.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();

			// The offline resolve is fast; the online prime may download plugin-group metadata. Bound both so a
			// wedged mvn can't hang `up` forever.
			var timeoutMs = (int)(online ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(2)).TotalMilliseconds;
			if (!proc.WaitForExit(timeoutMs))
			{
				try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
				return new MvnProbeOutcome(false, -1, output.ToString());
			}

			proc.WaitForExit(); // let the async output readers flush
			return new MvnProbeOutcome(true, proc.ExitCode, output.ToString());
		}
		catch (Exception)
		{
			// mvn not found / not runnable — "couldn't probe", not the trap this method detects.
			return new MvnProbeOutcome(false, -1, output.ToString());
		}
	}

	/// <summary>
	/// True when a previous run left a non-empty <c>cp-*.txt</c> in the shared Scala classpath cache
	/// (<c>&lt;temp&gt;/beam-scala-cp</c>) — proof the offline resolve works on this machine, so the probe can be
	/// skipped. Mirrors the temp location the launcher scripts write to and that <c>up</c> wipes on <c>:build</c>.
	/// </summary>
	private static bool HasNonEmptyScalaClasspathCache()
	{
		try
		{
			var dir = Path.Combine(Path.GetTempPath(), "beam-scala-cp");
			if (!Directory.Exists(dir)) return false;
			foreach (var f in Directory.EnumerateFiles(dir, "cp-*.txt"))
			{
				try { if (new FileInfo(f).Length > 0) return true; } catch { /* unreadable — ignore */ }
			}
		}
		catch { /* ignore */ }
		return false;
	}

	/// <summary>
	/// The fail-fast message when the offline resolve is broken and the automatic online prime could not fix it.
	/// Names the real cause, says plainly that <c>:build</c> does not fix it, and gives the exact online command
	/// plus the cached-miss cleanup — the same guidance the per-service launcher's empty-cache guard prints.
	/// </summary>
	private static string BuildDependencyPluginRemediation(MavenDependencyProbePlan plan)
	{
		var m2Plugin = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? "~",
			".m2", "repository", "org", "apache", "maven", "plugins", "maven-dependency-plugin");
		var mvnDisplay = plan.mvnCommand?.Contains(' ') == true ? $"\"{plan.mvnCommand}\"" : plan.mvnCommand;

		return
			"The Scala services can't start: Maven can't resolve the 'dependency' plugin prefix offline, so each " +
			"service's classpath step ('mvn -o dependency:build-classpath') fails with \"No plugin found for prefix " +
			"'dependency'\" — even though com.kickstand:core is present in ~/.m2.\n\n" +
			"Adding :build does NOT fix this: the reactor resolves plugins by version, never by prefix, so " +
			"it never caches the plugin-prefix metadata this needs. Beam tried to prime it online automatically and " +
			"could not (usually no network, or a blocked Maven repo).\n\n" +
			"Fix it by priming the plugin once, online, then run the stack again:\n" +
			$"  cd \"{plan.scalaDir}\"\n" +
			$"  {mvnDisplay} -U dependency:build-classpath -pl {plan.probeModule}\n\n" +
			"If it still fails, delete the cached-miss markers and retry:\n" +
			$"  (Windows)      del /s \"{m2Plugin}\\*.lastUpdated\"\n" +
			$"  (macOS/Linux)  rm {m2Plugin.Replace('\\', '/')}/*/*.lastUpdated";
	}

	/// <summary>
	///  if a non-Docker process is squatting <c>127.0.0.1:27015</c> it shadows the docker-published mongo_master, so
	/// every host process' <c>localhost:27015</c> Mongo connection times out and the gateway + scala services
	/// die. Freeing it lets those connections fall through to Docker's <c>0.0.0.0</c> bind. Only runs for
	/// Docker-dependent stacks; a no-op on non-Windows.
	/// </summary>
	private static void FreeMongoPortIfSquatted(List<LocalStackStep> steps)
	{
		var needsDocker = steps.Any(s => string.Equals(s.command, "docker", StringComparison.OrdinalIgnoreCase));
		if (!needsDocker) return;

		var freed = LocalStackProcess.FreeLoopbackPortSquatter(MongoHostPort);
		if (freed != null)
			Log.Warning($"Freed '{freed}' off 127.0.0.1:{MongoHostPort} — it was shadowing the local stack's " +
			            "mongo_master; Mongo connections will now reach Docker.");
	}

	private static void EnsureDockerRunning(List<LocalStackStep> steps)
	{
		var needsDocker = steps.Any(s => string.Equals(s.command, "docker", StringComparison.OrdinalIgnoreCase));
		if (!needsDocker) return;

		if (!DockerPathOption.TryGetDockerPath(out var dockerPath, out var err))
			throw new CliException($"This stack needs Docker, but the Docker CLI wasn't found: {err}");

		var psi = new ProcessStartInfo
		{
			FileName = dockerPath,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		// `docker info` talks to the daemon (unlike `docker --version`, which only checks the CLI binary).
		psi.ArgumentList.Add("info");
		psi.ArgumentList.Add("--format");
		psi.ArgumentList.Add("{{.ServerVersion}}");

		try
		{
			var proc = Process.Start(psi);
			if (proc == null)
				throw new CliException("Could not run docker to check whether the daemon is running.");

			if (!proc.WaitForExit(15000))
			{
				try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
				throw new CliException("Docker did not respond within 15s — is Docker Desktop (the daemon) running?");
			}

			if (proc.ExitCode != 0)
				throw new CliException("Docker does not appear to be running. Start Docker Desktop (or the Docker daemon), then run the stack again.");

			Log.Information("Docker daemon is running.");
		}
		catch (CliException)
		{
			throw;
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to verify Docker is running ({dockerPath}): {e.Message}. Start Docker and re-run `beam local run`.");
		}
	}

	/// <summary>
	/// Prepends the manifest's toolchain <c>bin</c> directories to the child's <c>PATH</c> and points
	/// <c>JAVA_HOME</c> at the resolved Java 8 home. No-op when the manifest records no toolchain, so a stack
	/// that was never <c>beam local setup</c>-provisioned keeps resolving everything through the inherited
	/// environment exactly as before.
	///
	/// <c>JAVA_HOME</c> is set for every step, not just the Maven ones: the inherited value is frequently a
	/// JDK 17/21 (an IDE bundle, or a newer default install), and Maven prefers <c>JAVA_HOME</c> over
	/// <c>PATH</c> — so leaving it alone would let the wrong JDK win despite the PATH prefix.
	/// </summary>
	private static void ApplyToolchainEnvironment(ProcessStartInfo psi, LocalStackConfig config)
	{
		var prefix = LocalStackConfigIO.ToolchainPathPrefix(config).ToList();
		if (prefix.Count > 0)
		{
			// psi.Environment is seeded from the current process, so read the inherited PATH from it (the child
			// would otherwise lose docker, git and everything else the steps rely on).
			psi.Environment.TryGetValue("PATH", out var inherited);
			inherited ??= Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

			var combined = string.Join(Path.PathSeparator.ToString(), prefix);
			psi.Environment["PATH"] = string.IsNullOrEmpty(inherited)
				? combined
				: combined + Path.PathSeparator + inherited;
		}

		if (!string.IsNullOrWhiteSpace(config?.javaHome)
		    && !config.javaHome.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal))
		{
			psi.Environment["JAVA_HOME"] = config.javaHome;
		}

		ApplyToolchainDotnetEnvironment(psi, config);
	}

	/// <summary>
	/// Pins the .NET environment to the toolchain's SDK.
	///
	/// Putting the private <c>dotnet</c> on PATH — or even invoking it by absolute path — is NOT enough. TWO
	/// independent mechanisms can splice a foreign SDK's build files into a toolchain-hosted MSBuild, and both
	/// have to be blocked:
	/// <list type="number">
	/// <item><b>Environment redirects.</b> <c>MSBuildSDKsPath</c>, <c>NuGetRestoreTargets</c> and friends override
	/// where MSBuild resolves <c>&lt;Project Sdk="..."&gt;</c> / <c>NuGet.targets</c> from; IDEs and build tooling
	/// set them. Removed below. Verified locally: a 10.0.100 muxer with <c>MSBuildSDKsPath</c> pointed at a 9.0.x
	/// <c>Sdks</c> directory loads the 9.0.x targets.</item>
	/// <item><b>The resolver's install-location scan.</b> With NO env var set,
	/// <c>Microsoft.DotNet.MSBuildSdkResolver</c> walks the machine's registered install locations for candidate
	/// SDKs, and from a PRE-RELEASE muxer SDK it promotes the reference to the newest STABLE SDK found there. This
	/// is why <c>dotnet --list-sdks</c> can report only the toolchain SDK while MSBuild still loads a newer
	/// install's <c>NuGet.targets</c>. Blocked by PINNING <c>DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR</c> to the
	/// toolchain — there is no variable to scrub for this one.</item>
	/// </list>
	/// Either way the mix fails as <c>MSB4062: could not load NuGet.Build.Tasks ... IMultiThreadableTask</c>: both
	/// SDKs ship <c>Microsoft.Build.Framework.dll</c> at assembly version 15.1.0.0, so the CLR treats them as
	/// type-identical and will not reload — but the newer copy added the <c>IMultiThreadableTask</c> interface its
	/// <c>NuGet.Build.Tasks.dll</c> needs, and the already-loaded older copy has no such type.
	///
	/// <c>DOTNET_ROOT</c> is pinned too, but note it affects NEITHER mechanism (the muxer finds SDKs relative to
	/// its own location) — it governs runtime resolution for apps the build spawns. Do not mistake it for the fix.
	/// </summary>
	public static void ApplyToolchainDotnetEnvironment(ProcessStartInfo psi, LocalStackConfig config)
	{
		var dotnetHome = config?.toolchain?.dotnet;
		if (string.IsNullOrWhiteSpace(dotnetHome)
		    || dotnetHome.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal)
		    || !Directory.Exists(dotnetHome))
		{
			return;
		}

		psi.Environment["DOTNET_ROOT"] = dotnetHome;

		// Architecture-specific roots take precedence over DOTNET_ROOT, so a stale one would win.
		foreach (var arch in new[] { "DOTNET_ROOT_X64", "DOTNET_ROOT_X86", "DOTNET_ROOT(x86)", "DOTNET_ROOT_ARM64" })
			psi.Environment.Remove(arch);

		// Mechanism 1 — environment redirects. Each of these points MSBuild at a specific SDK/CLI/targets path
		// and silently overrides DOTNET_ROOT for resolution. `NuGetRestoreTargets` is the narrow variant that
		// hits only restore. Removed, not blanked: an empty value is still a value the resolver treats as a path.
		foreach (var redirect in new[]
		{
			"MSBuildSDKsPath",
			"MSBuildToolsPath",
			"MSBUILD_EXE_PATH",
			"NuGetRestoreTargets",
			"DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR",
			"DOTNET_MSBUILD_SDK_RESOLVER_MSBUILD_DIR",
		})
		{
			psi.Environment.Remove(redirect);
		}

		// Mechanism 2 — PIN this one rather than removing it. With no env vars set at all,
		// Microsoft.DotNet.MSBuildSdkResolver walks the machine's registered install locations
		// (HKLM\SOFTWARE\dotnet\Setup\InstalledVersions on Windows) for candidate SDKs, and from a
		// PRE-RELEASE muxer SDK it promotes the project's SDK reference to the newest STABLE one it finds there.
		// That is how `dotnet --list-sdks` can report only the toolchain SDK while MSBuild still loads
		// C:\Program Files\dotnet\sdk\<newer>\NuGet.targets. Scrubbing cannot fix it — there is no variable
		// to scrub — so the resolver's search has to be confined to the toolchain install instead.
		psi.Environment["DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR"] = dotnetHome;

		// The muxer used by anything the build shells out to (SDK tasks, `dotnet tool`, source generators).
		var hostExe = Path.Combine(dotnetHome, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
		if (File.Exists(hostExe))
			psi.Environment["DOTNET_HOST_PATH"] = hostExe;

		// Blocks the legacy Windows behaviour where a private install ALSO searches the global one.
		psi.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";

		// A freshly extracted SDK has no first-run marker, so its first invocation prints the welcome banner AND
		// installs an ASP.NET Core HTTPS development certificate — a machine-level side effect nobody asked a
		// build step for. Suppress the whole first-run experience.
		psi.Environment["DOTNET_NOLOGO"] = "1";
		psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
		psi.Environment["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false";
		if (!psi.Environment.ContainsKey("DOTNET_CLI_TELEMETRY_OPTOUT"))
			psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
	}

	private static string ResolveJavaHome(CommandArgs args, LocalStackConfig config)
	{
		// 1. Explicit --java-path CLI option (user wants this exact path for this invocation)
		var explicitPath = args.AppContext?.ExplicitJavaPath;
		if (!string.IsNullOrWhiteSpace(explicitPath)) return explicitPath;

		// 2. BEAM_JAVA_HOME env var (machine-level explicit override)
		var envHome = ConfigService.CustomJavaHome;
		if (!string.IsNullOrWhiteSpace(envHome)) return envHome;

		// 3. Manifest-pinned path (stored by beam local setup — the user's chosen version)
		if (!string.IsNullOrWhiteSpace(config.javaHome)) return config.javaHome;

		// 4. The toolchain `beam local setup` provisioned. `setup` also mirrors this into javaHome above, so this
		//    only carries a manifest whose javaHome was hand-cleared — but it must win over auto-detection, or a
		//    system JDK 8 would silently be preferred to the pinned one.
		if (!string.IsNullOrWhiteSpace(config.toolchain?.java)) return config.toolchain.java;

		// 5. Auto-detection as last resort (JAVA_HOME, macOS java_home, common install dirs)
		if (JavaPathOption.TryGetJavaHome(out var home, out _)) return home;
		return null; // Scala launch shell will fail clearly if a JDK is needed and missing
	}

	// ----------------------------------------------------------------------------------
	// Launching
	// ----------------------------------------------------------------------------------

	private class Launched
	{
		public LocalStackStep Step;
		public Process Process;
		public Task ExitedTask;
		public string StdoutLog;
		public string StderrLog;
		public string WorkingDirectory;
		public string Kind;
		/// <summary>Attached mode: the live stdout/stderr buffer feeding readiness. Null in detached mode
		/// (readiness tails the log files instead).</summary>
		public StreamLineBuffer LineBuffer;

		/// <summary>Health signals scraped from this step's log stream. Attached mode only — in detached mode
		/// nothing reads the child's output live, so it stays silent.</summary>
		public StepHealth Health = new StepHealth();

		/// <summary>The line source(s) the readiness gate should poll: the in-memory stream buffer (attached)
		/// or fresh tailers over the stdout/stderr log files (detached).</summary>
		public IReadOnlyList<ILineSource> OpenLineSources() =>
			LineBuffer != null
				? new ILineSource[] { LineBuffer }
				: new ILineSource[] { new LineTailer(StdoutLog, -1), new LineTailer(StderrLog, -1) };
	}

	/// <summary>
	/// Per-step health that only the log stream can tell us about. A microservice whose federation components
	/// fail to register still starts, still passes every readiness gate, and still reports itself ready for
	/// traffic — the only evidence is one line in its log. Without this the stack printed "Stack is up" over a
	/// service that was running but unroutable, which is exactly how a whole class of federation bugs stayed
	/// invisible.
	/// </summary>
	private sealed class StepHealth
	{
		private readonly object _gate = new();
		private bool _federationFailed;
		private Action _handler;

		/// <summary>Called from the log pump on the first failure line; fires the handler once.</summary>
		public void MarkFederationFailed()
		{
			Action handler;
			lock (_gate)
			{
				if (_federationFailed)
					return;
				_federationFailed = true;
				handler = _handler;
			}

			handler?.Invoke();
		}

		/// <summary>
		/// Registers the one-shot reporter. Fires immediately when the failure has already been seen, so a
		/// failure that lands before the readiness wait is reported just like one that lands after it — the
		/// retry backoff means it can arrive either side of the grace period.
		/// </summary>
		public void WhenFederationFailed(Action handler)
		{
			bool already;
			lock (_gate)
			{
				_handler = handler;
				already = _federationFailed;
			}

			if (already)
			{
				handler();
			}
		}
	}

	private (string exe, string[] leading) ResolveBeam(CommandArgs args)
	{
		// Run the SAME beam build as a subprocess. When hosted by `dotnet` (dev: `dotnet Beamable.Tools.dll`),
		// invoke that exact dll directly rather than `dotnet beam` (which resolves through the tool cache / cwd).
		var processPath = Environment.ProcessPath ?? string.Empty;
		var isDotnetHost = processPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
		                   || processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);

		if (!isDotnetHost)
			return (processPath, Array.Empty<string>());

		var entryDll = Assembly.GetEntryAssembly()?.Location;
		if (!string.IsNullOrEmpty(entryDll) && File.Exists(entryDll))
			return (processPath, new[] { entryDll });

		return (args.AppContext.DotnetPath, new[] { "beam" });
	}

	/// <summary>
	/// The step's run-state <c>kind</c> (<c>beam | docker | shell | process</c>), which drives how <c>stop</c>
	/// reverses it and which command-line tokens identify its runtime. Derived from the step definition alone so
	/// a step that is adopted rather than launched (see the already-serving path in <c>Handle</c>) is classified
	/// exactly as the launched one would have been.
	/// </summary>
	private static string ResolveKind(LocalStackStep step) =>
		step.beam ? "beam"
		: string.Equals(step.command, "docker", StringComparison.OrdinalIgnoreCase) ? "docker"
		: step.shell ? "shell"
		: "process";

	private Launched StartStep(LocalStackStep step, LocalStackConfig config, string beamExe, string[] beamLeading,
		string beamWorkspaceFallback, string logsDir, bool attached, bool saveLogs, LocalStackJobObject job)
	{
		var workDir = LocalStackConfigIO.Substitute(step.workingDirectory, config);
		var argsText = LocalStackConfigIO.Substitute(step.arguments, config) ?? string.Empty;
		// The command carries tokens too (${maven}/${npm}/${dotnet}), which resolve to the toolchain's pinned
		// executables — or back to the bare command name when no toolchain was provisioned.
		var command = LocalStackConfigIO.Substitute(step.command, config);

		// `:build` always does a CLEAN Scala build: an incremental `mvn package` can leave cross-module classes
		// skewed (the NoSuchMethodError class of failure). Inject `clean` for manifests generated before this
		// whose mvn build step only says `package`; new manifests already include it.
		if (step.build && IsMvn(command) && !argsText.Contains("clean"))
			argsText = InsertCleanBeforeMavenGoal(argsText);

		// beam sub-commands need to run inside a .beamable workspace to see the local service manifest.
		if (step.beam && (string.IsNullOrEmpty(workDir) || !Directory.Exists(workDir)))
			workDir = beamWorkspaceFallback;

		// A step whose working directory is missing cannot run at all; the launch loop checks that up front
		// (CannotStartReason) and reports the step as failed rather than launching it into the CLI's own cwd.

		var safe = SafeName(step.name);
		var stdoutLog = Path.Combine(logsDir, safe + ".log");
		var stderrLog = Path.Combine(logsDir, safe + ".err.log");
		// Every run writes each step's output to its log files: detached redirects to them, attached tees to them
		// (the board shows no output, and a failure shows the file's last lines). Reset them for this run.
		ResetLog(stdoutLog);
		ResetLog(stderrLog);

		var kind = ResolveKind(step);

		var inner = BuildInnerScript(step, beamExe, beamLeading, argsText, workDir, command);
		var launcher = WriteLauncher(step, logsDir, safe, inner, argsText);
		var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };

		if (!OperatingSystem.IsWindows())
		{
			psi.FileName = "/bin/sh";
			psi.ArgumentList.Add("-c");
			// Attached: run the launcher as a normal foreground child (dies with `up`); pipe its output to us.
			// Detached: daemonize so it survives `up` returning —
			//   exec → tracked pid stays == the service; nohup → immune to SIGHUP; < /dev/null → detach stdin
			//   (macOS nohup doesn't redirect stdin); > log 2> err → logs persist after this CLI exits.
			psi.ArgumentList.Add(attached
				? $"exec sh {Sq(launcher)}"
				: $"exec nohup sh {Sq(launcher)} < /dev/null > {Sq(stdoutLog)} 2> {Sq(stderrLog)}");
		}
		else
		{
			psi.FileName = "cmd.exe";
			// Attached: run the launcher and pipe stdout/stderr back (redirected below). Detached: redirect to
			// the log files and detach stdin from the console.
			psi.Arguments = attached
				? $"/c \"\"{launcher}\"\""
				: $"/c \"\"{launcher}\" < NUL > \"{stdoutLog}\" 2> \"{stderrLog}\"\"";
		}

		if (attached)
		{
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardInput = true; // closed after start so children never block reading the console
		}

		if (!string.IsNullOrEmpty(workDir) && Directory.Exists(workDir))
			psi.WorkingDirectory = workDir;

		// Put the toolchain ahead of the inherited PATH, and pin JAVA_HOME to it, BEFORE the step's own
		// environment is applied (so an explicit value in the manifest still wins). Substituting the command is
		// not enough on its own: every one of these tools execs another one — npm execs node, mvn execs java,
		// dotnet resolves SDKs — and those nested lookups go through PATH/JAVA_HOME. Without this a
		// toolchain-pinned `mvn` still compiles the Scala reactor with whatever JDK happens to be first on the
		// developer's PATH, which is the drift the toolchain exists to remove.
		ApplyToolchainEnvironment(psi, config);

		// The stack's target. Beam steps get it as environment overrides, which `project run`, the services it starts
		// and their generate-env all read instead of the workspace's .beamable config. The portal gets its API host as
		// a process variable, which Vite puts ahead of .env.local, so that file is never rewritten. Rewriting both
		// used to repoint every other beam command and the portal too, and left them there after the stack stopped.
		if (step.beam)
		{
			foreach (var (k, v) in LocalStackTargets.Environment(_targetLogin))
				psi.Environment[k] = v;
		}

		if (string.Equals(step.name, LocalStackTemplate.PortalFrontendStepName, StringComparison.OrdinalIgnoreCase))
		{
			psi.Environment[PortalEnvService.ApiBaseKey] = config.host;
			// The extensions' SDK comes from the local CDN, which serves local builds and proxies the published ones.
			if (_localWebPackages)
				psi.Environment[PortalEnvService.SdkCdnBaseKey] = PortalEnvService.LocalSdkCdnBase;
		}

		foreach (var (k, v) in step.environment)
			psi.Environment[k] = LocalStackConfigIO.Substitute(v, config);

		// Beam microservice/extension runtimes shell out to `beam generate-env` on startup. By default that is
		// `dotnet tool run beam` (the workspace's LOCAL tool), which fails with "Execute dotnet tool restore ..."
		// when the tool manifest isn't restored. Point BEAM_PATH at THIS beam build so the child reuses it
		// (`dotnet <this-dll> generate-env ...`) instead of the local tool.
		if (step.beam)
		{
			var entryDll = Assembly.GetEntryAssembly()?.Location;
			if (!string.IsNullOrEmpty(entryDll) && File.Exists(entryDll))
				psi.Environment["BEAM_PATH"] = entryDll.Contains(' ') ? $"\"{entryDll}\"" : entryDll;
		}

		Process proc;
		try
		{
			proc = Process.Start(psi);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to start step '{step.name}' ({psi.FileName}): {e.Message}");
		}

		if (proc == null)
			throw new CliException($"Failed to start step '{step.name}' ({psi.FileName}).");

		// Attached: enroll in the kill-on-close job immediately — before the child spawns grandchildren, so the
		// whole subtree (powershell → java, etc.) inherits membership and is reaped when the CLI exits.
		job?.Assign(proc);

		var exitTcs = new TaskCompletionSource();
		proc.EnableRaisingEvents = true;
		proc.Exited += (_, _) => exitTcs.TrySetResult();

		StreamLineBuffer buffer = null;
		var health = new StepHealth();
		if (attached)
		{
			// Detach stdin so the child never blocks reading the console.
			try { proc.StandardInput.Close(); } catch { /* some children don't open stdin */ }

			// Pipe stdout/stderr live: to the readiness buffer, to the log files, and with --verbose to the console
			// (prefixed with the step name).
			buffer = new StreamLineBuffer();
			_ = PumpAsync(proc.StandardOutput, step.name, isError: false, buffer, stdoutLog, health, _verbose);
			_ = PumpAsync(proc.StandardError, step.name, isError: true, buffer, stderrLog, health, _verbose);
		}

		return new Launched
		{
			Step = step, Process = proc, ExitedTask = exitTcs.Task,
			StdoutLog = stdoutLog, StderrLog = stderrLog, WorkingDirectory = workDir, Kind = kind,
			LineBuffer = buffer, Health = health
		};
	}

	/// <summary>Writes the per-step launcher script to <paramref name="logsDir"/> and returns its path. On
	/// Windows a PowerShell shell step gets a <c>.launch.ps1</c> plus a <c>.cmd</c> shim that invokes it; every
	/// other case writes the inner command directly.</summary>
	private static string WriteLauncher(LocalStackStep step, string logsDir, string safe, string inner, string argsText)
	{
		if (!OperatingSystem.IsWindows())
		{
			var sh = Path.Combine(logsDir, safe + ".launch.sh");
			File.WriteAllText(sh, inner + "\n");
			return sh;
		}

		var cmd = Path.Combine(logsDir, safe + ".launch.cmd");
		if (step.shell && string.Equals(step.shellKind, "powershell", StringComparison.OrdinalIgnoreCase))
		{
			// cmd.exe can't run the POSIX/PowerShell script directly — write it to a .launch.ps1 and have the
			// .cmd shim run powershell on it (captures powershell's + the java child's inherited output).
			var ps1 = Path.Combine(logsDir, safe + ".launch.ps1");
			File.WriteAllText(ps1, argsText + "\r\n");
			File.WriteAllText(cmd, $"@powershell -NoProfile -ExecutionPolicy Bypass -File \"{ps1}\"\r\n");
		}
		else
		{
			File.WriteAllText(cmd, inner + "\r\n");
		}

		return cmd;
	}

	/// <summary>Attached mode: feeds the readiness buffer from one of a child's piped readers, appends each line to
	/// the log file at <paramref name="teePath"/>, and with <paramref name="echo"/> (--verbose) prints it, prefixed
	/// with the step name.</summary>
	private static async Task PumpAsync(StreamReader reader, string name, bool isError, StreamLineBuffer buffer,
		string teePath, StepHealth health = null, bool echo = true)
	{
		try
		{
			string line;
			while ((line = await reader.ReadLineAsync()) != null)
			{
				if (echo && isError) Log.Warning($"[{name}] {line}");
				else if (echo) Log.Information($"[{name}] {line}");
				buffer.Append(line);

				// Substring match on the raw line: the runtime logs structured JSON under
				// LOG_TYPE=structured+file, so the marker arrives embedded in a JSON payload rather than as
				// a bare message. This is the same approach RunProjectCommand's progress table takes.
				if (health != null
				    && line.Contains(ServiceLogs.FEDERATION_REGISTRATION_FAILED, StringComparison.Ordinal))
				{
					health.MarkFederationFailed();
				}

				if (teePath != null)
				{
					try { File.AppendAllText(teePath, line + Environment.NewLine); }
					catch { /* best-effort tee */ }
				}
			}
		}
		catch { /* reader closes when the child exits or is killed */ }
	}

	/// <summary>Builds the shell body written to the per-step launcher file (its last command is exec'd so the
	/// tracked pid becomes the service).</summary>
	private static string BuildInnerScript(LocalStackStep step, string beamExe, string[] beamLeading, string argsText,
		string workDir, string command)
	{
		if (step.shell)
			return argsText; // already a shell script; the Scala launcher ends with its own `exec`.

		IEnumerable<string> parts;
		if (step.beam)
			parts = new[] { beamExe }.Concat(beamLeading).Concat(SplitArgs(argsText));
		else
		{
			// A program saved with its arguments in it runs as the program and those arguments.
			var (program, arguments) = LocalStackCustomSteps.SplitProgram(command, argsText);
			parts = new[] { ResolveExecutable(program, workDir) }.Concat(SplitArgs(arguments));
		}

		if (OperatingSystem.IsWindows())
			return string.Join(" ", parts.Select(WinQuote));

		return "exec " + string.Join(" ", parts.Select(Sq));
	}

	/// <summary>A step's name as its log files are named.</summary>
	public static string SafeName(string name)
	{
		var cleaned = Regex.Replace(name ?? "step", @"[^A-Za-z0-9._-]+", "_").Trim('_');
		return string.IsNullOrEmpty(cleaned) ? "step" : cleaned;
	}

	/// <summary>
	/// True when a step's (already substituted) command is Maven. Matches the file name rather than the whole
	/// string, because a toolchain-provisioned <c>${maven}</c> substitutes to an absolute path — comparing the
	/// full value would silently stop recognising the Scala build step and drop the injected <c>clean</c>.
	/// </summary>
	public static bool IsMvn(string command)
	{
		if (string.IsNullOrWhiteSpace(command)) return false;

		string name;
		try
		{
			name = Path.GetFileName(command.Trim().Trim('"'));
		}
		catch
		{
			name = command;
		}

		return name.Equals("mvn", StringComparison.OrdinalIgnoreCase)
		       || name.Equals("mvn.cmd", StringComparison.OrdinalIgnoreCase)
		       // An unsubstituted token still means "this is the Maven step".
		       || command.Contains(LocalStackTemplate.MavenToken, StringComparison.Ordinal);
	}

	/// <summary>Inserts a <c>clean</c> goal before the first build phase in an mvn argument string. Maven runs
	/// goals in command-line order, so <c>clean</c> must precede <c>package</c>/<c>install</c>/etc.</summary>
	public static string InsertCleanBeforeMavenGoal(string args)
	{
		foreach (var goal in new[] { "package", "install", "verify", "test", "compile" })
		{
			var token = " " + goal;
			var idx = args.IndexOf(token, StringComparison.Ordinal);
			if (idx >= 0) return args.Substring(0, idx) + " clean" + args.Substring(idx);
		}
		return "clean " + args;
	}

	/// <summary>Creates/truncates a per-step log file for a fresh run. Opens with shared read/write so the
	/// tailer and the launched child can both hold it; a genuine lock (non-writable dir, or a concurrent
	/// second `up`) surfaces as an actionable <see cref="CliException"/> rather than a raw IOException.</summary>
	private static void ResetLog(string path)
	{
		try
		{
			using var _ = new FileStream(path, FileMode.Create, FileAccess.Write,
				FileShare.ReadWrite | FileShare.Delete);
		}
		catch (IOException e)
		{
			throw new CliException(
				$"Could not reset log '{path}': {e.Message}. It may be held by another process — a previous " +
				"`beam local run` that is still running or was interrupted. Run `beam local stop`, close any other " +
				"`beam local run`, or kill the leftover process, then retry.");
		}
	}

	/// <summary>Single-quotes a value for <c>/bin/sh</c>.</summary>
	private static string Sq(string s) => "'" + (s ?? string.Empty).Replace("'", "'\\''") + "'";

	private static string WinQuote(string s) =>
		string.IsNullOrEmpty(s) ? "\"\"" : (s.Contains(' ') || s.Contains('"') ? "\"" + s.Replace("\"", "\\\"") + "\"" : s);

	private static string ResolveExecutable(string command, string workDir)
	{
		if (string.IsNullOrWhiteSpace(command))
			throw new CliException("A non-beam/non-shell step must define a 'command'.");

		// Prefer the executable inside the working directory when it exists there. This covers an explicit
		// relative path (./BeamableGateway) AND a bare exe name (BeamableGateway.exe): resolving the latter to
		// a full path lets it launch even when cmd.exe won't search the cwd (NoDefaultCurrentDirectoryInExePath).
		// Commands not present in workDir (docker, npm.cmd) fall through and resolve via PATH as before.
		if (!string.IsNullOrEmpty(workDir))
		{
			var combined = Path.GetFullPath(Path.Combine(workDir, command));
			if (File.Exists(combined))
				return combined;
		}

		return command;
	}

	private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

	private static IEnumerable<string> SplitArgs(string value) =>
		string.IsNullOrWhiteSpace(value)
			? Array.Empty<string>()
			: Whitespace.Split(value.Trim());

	// ----------------------------------------------------------------------------------
	// Readiness / completion
	// ----------------------------------------------------------------------------------

	private Task AwaitStep(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress, int totalSteps,
		CancellationToken token, Func<Launched> relaunch)
	{
		// Report a federation-registration failure whenever it surfaces, rather than gating the step on it.
		// Registration retries for several seconds, so the failure can land either side of a readiness wait —
		// and a service that came up but cannot route federated traffic should be called out either way. It is
		// reported as degraded, not failed: the stack is still usable and tearing it down would be worse.
		l.Health.WhenFederationFailed(() =>
		{
			Send(step.name, "degraded",
				"running, but its federation components failed to register — see `beam local logs`",
				baseProgress + 1f / totalSteps, LocalStackStepState.Degraded, "federation didn't register");
			Log.Warning($"[{step.name}] running, but federation registration failed — this service will not "
			            + "receive federated traffic. See `beam local logs` for the platform's rejection.");
		});

		if (step.waitForExit)
			return AwaitCompletion(step, l, config, baseProgress);

		if (!string.IsNullOrEmpty(step.readyWhenHttpOk)
		    || !string.IsNullOrEmpty(step.readyWhenHttp200)
		    || !string.IsNullOrEmpty(step.readyWhenLogContains))
			return AwaitReadiness(step, l, config, baseProgress, token, relaunch);

		return AwaitBriefLiveness(step, l, baseProgress, totalSteps, token);
	}

	/// <summary>
	/// For steps with no readiness gate (e.g. beam microservice/extension runs), wait a short grace period and
	/// surface an immediate exit as a failure with its last log line — so a service that dies on startup is
	/// visible instead of being reported as "assuming up".
	/// </summary>
	private async Task AwaitBriefLiveness(LocalStackStep step, Launched l, float baseProgress, int totalSteps,
		CancellationToken token)
	{
		try { await Task.WhenAny(l.ExitedTask, Task.Delay(TimeSpan.FromSeconds(3), token)); }
		catch (OperationCanceledException) { return; }

		if (l.ExitedTask.IsCompleted)
		{
			var code = SafeExitCode(l);
			Send(step.name, "failed", $"exited on startup (code {code}) — see `beam local logs`", baseProgress,
				LocalStackStepState.Failed, $"exit {code} on startup", Tail(l), LogHint(l));
			if (!Showing) Log.Warning($"[{step.name}] exited on startup (code {code}). Last log: {LastLogLine(l)}");
			return;
		}

		Send(step.name, "running", "no readiness gate; assuming up", baseProgress + 1f / totalSteps,
			LocalStackStepState.Running, "no readiness check");
	}

	/// <summary>Marks a step that ran to completion as failed, for the board and for the steps that need it.</summary>
	private void Fail(LocalStackStep step, string detail, string message, float baseProgress, Launched l)
	{
		_failed[step.name] = detail;
		Send(step.name, "failed", message, baseProgress, LocalStackStepState.Failed, detail, Tail(l), LogHint(l));
		if (!Showing) Log.Warning($"[{step.name}] {message}; the stack carries on without it. Last log: {LastLogLine(l)}");
	}

	private async Task AwaitCompletion(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress)
	{
		Send(step.name, "starting", "waiting for completion", baseProgress);
		var seconds = Math.Max(1, step.readyTimeoutSeconds);
		var done = await Task.WhenAny(l.ExitedTask, Task.Delay(TimeSpan.FromSeconds(seconds)));
		if (done != l.ExitedTask)
		{
			// Stopped, so it doesn't run on unwatched: the extension repoint once kept reinstalling packages for ten
			// more minutes after the stack said it was up, while the services that need them had already started.
			KillLaunched(l);
			Fail(step, $"didn't finish in {seconds}s", $"did not complete within {seconds}s, so it was stopped", baseProgress, l);
			return;
		}

		// A step that fails is marked failed and the stack carries on: the steps that need it are skipped when their
		// turn comes (see FailedNeed), and everything else still starts. Stopping the whole stack for one failed seed
		// or one host's build took everything down with it.
		var code = SafeExitCode(l);
		if (code != 0)
		{
			Fail(step, $"exit {code}", $"exited with code {code}", baseProgress, l);
			return;
		}

		// A build that exits 0 without producing what it declared is the reference script's `[[ -x $BIN ]] ||
		// die`: the run step after it would launch a binary that isn't there, so it counts as failed.
		var output = LocalStackConfigIO.ResolveRequiredOutput(step, config);
		if (output != null && !File.Exists(output) && !Directory.Exists(output))
		{
			_failed[step.name] = "no output";
			Send(step.name, "failed", $"did not produce {output}", baseProgress, LocalStackStepState.Failed, "no output", hint: $"expected {output}");
			if (!Showing) Log.Warning($"[{step.name}] completed but did not produce '{output}'. Check its command and working directory in the settings.");
			return;
		}

		// What it compiled, for the next run to compare against. Read after the build: it may have changed tracked
		// files (npm install rewrites package-lock.json).
		if (step.build && _builds != null)
		{
			try
			{
				lock (_launchedLock) LocalStackBuilds.Record(step, config, _builds, _buildsPath);
			}
			catch (Exception e)
			{
				Log.Debug($"[{step.name}] could not record the build: {e.Message}");
			}
		}

		Send(step.name, "ready", "completed", baseProgress, LocalStackStepState.Done);
		if (string.Equals(step.name, LocalStackTemplate.WebInstallStepName, StringComparison.OrdinalIgnoreCase))
			_board?.SetWebPackages(WebPackagesService.LoadOverlay(_workspaceRoot)?.packages);
	}

	private async Task AwaitReadiness(LocalStackStep step, Launched l, LocalStackConfig config, float baseProgress,
		CancellationToken token, Func<Launched> relaunch)
	{
		var httpUrl = LocalStackConfigIO.Substitute(step.readyWhenHttpOk, config);
		var http200Url = LocalStackConfigIO.Substitute(step.readyWhenHttp200, config);
		var timeout = Math.Max(1, step.readyTimeoutSeconds);
		var retriesLeft = Math.Max(0, step.readyRetries);
		Send(step.name, "starting", $"waiting for readiness (timeout {timeout}s)", baseProgress);

		// One-time diagnostic for the classic macOS trap: AirPlay Receiver squats :5000 and answers the
		// gateway's /health with a 403, so readiness can never pass (and the gateway can't bind the port).
		await WarnIfForeignServer(!string.IsNullOrEmpty(http200Url) ? http200Url : httpUrl, step.name, token);

		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
		// Attached: read readiness from the live stream buffer; detached: tail the log files. Same scan either way.
		var sources = l.OpenLineSources();
		try
		{
			var lastLine = "";
			var waited = 0;
			var nextBeat = 10;

			while (true)
			{
				token.ThrowIfCancellationRequested();

				if (waited >= timeout)
				{
					// The gate never tripped. The `readyRetries` path below only fires when the process EXITS, so a
					// service that catches its own startup failure and then parks was never retried — the Scala
					// gateway losing the Mongo connect race is the standing example. The bring-up logged
					// "continuing anyway" and still printed "Stack is up" with nothing listening on 9002, so every
					// `/basic/*` call through Caddy 502'd and every microservice and portal extension died on
					// startup, which reads as "the whole stack is broken" rather than "one step is hung".
					//
					// Only retry when the step is PROVABLY not serving. Two independent signals count as "provably":
					// port unbound (see <see cref="StepIsDeadOnItsPort"/>), and a Mongo startup timeout in the tail
					// of the log (see <see cref="LogShowsMongoStartupFailure"/>). Missing the gate is usually a
					// false negative — on a good run a dozen Scala services report "did not signal ready" while
					// serving traffic fine — so retrying on the timeout alone would kill and relaunch a working
					// stack. The Mongo case is safe to add because the service has explicitly written
					// "MongoTimeoutException ... state=CONNECTING" and then parked without ever binding.
					var deadOnPort = StepIsDeadOnItsPort(step);
					var mongoRace = !deadOnPort && LogShowsMongoStartupFailure(l);
					if (retriesLeft > 0 && relaunch != null && (deadOnPort || mongoRace))
					{
						retriesLeft--;
						var reason = deadOnPort
							? $"never signalled ready and nothing is listening on port {step.port} — hung rather than slow"
							: "lost the Mongo connect race at startup (MongoTimeoutException) and parked";
						var progressMsg = deadOnPort
							? $"hung with port {step.port} unbound — retrying ({retriesLeft} left)"
							: $"lost Mongo connect race — retrying ({retriesLeft} left)";
						Log.Warning($"[{step.name}] {reason}; killing and retrying ({retriesLeft} left). Last log: {LastLogLine(l)}");
						Send(step.name, "starting", progressMsg, baseProgress, LocalStackStepState.Restarting,
							$"retry {Math.Max(0, step.readyRetries) - retriesLeft} of {Math.Max(0, step.readyRetries)}, {(deadOnPort ? "hung" : "Mongo race")}");

						// Kill first: unlike the exit path below, this process is still alive, and leaving it behind
						// orphans a JVM that would race the relaunch for the same port.
						KillLaunched(l);
						try { await Task.Delay(3000, token); }
						catch (OperationCanceledException) { return; }

						l = relaunch();
						foreach (var s in sources) s.Dispose();
						sources = l.OpenLineSources();
						lastLine = "";
						waited = 0;
						nextBeat = 10;
						continue;
					}

					Send(step.name, "running", $"did not signal ready within {timeout}s; continuing", baseProgress,
						LocalStackStepState.Running, $"no ready signal in {timeout}s");
					Log.Warning($"[{step.name}] did not signal ready within {timeout}s — continuing anyway.");
					return;
				}

				if (l.ExitedTask.IsCompleted)
				{
					var code = SafeExitCode(l);
					if (retriesLeft > 0 && relaunch != null)
					{
						retriesLeft--;
						Log.Warning($"[{step.name}] exited early (code {code}); retrying ({retriesLeft} left). Last log: {LastLogLine(l)}");
						Send(step.name, "starting", $"exited early (code {code}) — retrying ({retriesLeft} left)", baseProgress,
							LocalStackStepState.Restarting, $"retry {Math.Max(0, step.readyRetries) - retriesLeft} of {Math.Max(0, step.readyRetries)}, exit {code}");
						try { await Task.Delay(3000, token); }
						catch (OperationCanceledException) { return; }

						// Relaunch and re-watch the fresh source (new stream buffer, or new tailers over the
						// truncated log files).
						l = relaunch();
						foreach (var s in sources) s.Dispose();
						sources = l.OpenLineSources();
						lastLine = "";
						waited = 0;
						nextBeat = 10;
						continue;
					}

					Send(step.name, "failed", $"process exited early (code {code})", baseProgress,
						LocalStackStepState.Failed, $"exit {code}", Tail(l), LogHint(l));
					if (!Showing) Log.Warning($"[{step.name}] exited before becoming ready (code {code}). Last log: {LastLogLine(l)}");
					return;
				}

				// Log-substring gate: scan any lines that appeared since the last poll (in either stream).
				if (!string.IsNullOrEmpty(step.readyWhenLogContains))
				{
					foreach (var line in sources.SelectMany(s => s.ReadAvailableLines()))
					{
						lastLine = line;
						if (line.Contains(step.readyWhenLogContains))
						{
							Send(step.name, "ready", $"ready after {waited}s", baseProgress, LocalStackStepState.Ready);
							Log.Information($"[{step.name}] ready after {waited}s.");
							return;
						}
					}
				}

				if (!string.IsNullOrEmpty(http200Url) && await HttpStatusOk(http, http200Url, require200: true, token))
				{
					Send(step.name, "ready", $"ready after {waited}s (200)", baseProgress, LocalStackStepState.Ready);
					Log.Information($"[{step.name}] ready after {waited}s (HTTP 200).");
					return;
				}

				if (!string.IsNullOrEmpty(httpUrl) && await HttpStatusOk(http, httpUrl, require200: false, token))
				{
					Send(step.name, "ready", $"ready after {waited}s", baseProgress, LocalStackStepState.Ready);
					Log.Information($"[{step.name}] ready after {waited}s.");
					return;
				}

				await Task.Delay(1000, token);
				waited++;

				if (waited >= nextBeat)
				{
					nextBeat += 10;
					var hint = string.IsNullOrEmpty(lastLine) ? "" : $" | {Trim(lastLine, 110)}";
					Send(step.name, "starting", $"still starting — {waited}/{timeout}s{hint}", baseProgress);
					Log.Information($"[{step.name}] still starting — {waited}/{timeout}s{hint}");
				}
			}
		}
		finally
		{
			foreach (var s in sources) s.Dispose();
		}
	}

	/// <summary>
	/// True when a step that missed its readiness gate is provably not serving: it declares the TCP port it binds
	/// and nothing holds that port. Steps leaving <c>port</c> at 0 always answer false — a missed gate there is
	/// usually a false negative (most Scala services register correctly but never log the exact substring the gate
	/// looks for), and treating that as failure would kill and relaunch healthy services. Only the three steps
	/// whose whole job is to serve a port declare one: the Scala gateway, the C# gateway and the portal frontend.
	/// </summary>
	private static bool StepIsDeadOnItsPort(LocalStackStep step) =>
		step.port > 0 && !LocalStackPortGuard.IsPortTaken(step.port);

	/// <summary>
	/// True if the step's HTTP readiness endpoint is already answering — i.e. something is already serving
	/// there, so launching another instance would just conflict on the port. Only meaningful for HTTP-gated
	/// steps (gateway, portal); log-gated / no-gate steps return false (their duplicate detection is the
	/// run-state pid check).
	/// </summary>
	/// <summary>
	/// Warns when a readiness endpoint is already answered by a foreign server — most commonly macOS AirPlay
	/// Receiver squatting :5000 (Server: AirTunes), which both blocks the gateway from binding the port and
	/// makes the /health readiness gate impossible to satisfy.
	/// </summary>
	private static async Task WarnIfForeignServer(string url, string stepName, CancellationToken token)
	{
		if (string.IsNullOrEmpty(url)) return;
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
			using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
			var server = res.Headers.Server?.ToString() ?? "";
			if ((int)res.StatusCode != 200 && server.Contains("AirTunes", StringComparison.OrdinalIgnoreCase))
			{
				Log.Warning($"[{stepName}] {url} is being answered by macOS AirPlay Receiver (Server: {server}), not your service. " +
				            "Port 5000 is taken — turn OFF System Settings → General → AirDrop & Handoff → \"AirPlay Receiver\" " +
				            "(or change the gateway port), then re-run.");
			}
		}
		catch { /* diagnostic only */ }
	}

	private static async Task<bool> AlreadyServing(LocalStackStep step, LocalStackConfig config, CancellationToken token)
	{
		var http200Url = LocalStackConfigIO.Substitute(step.readyWhenHttp200, config);
		var httpUrl = LocalStackConfigIO.Substitute(step.readyWhenHttpOk, config);
		if (string.IsNullOrEmpty(http200Url) && string.IsNullOrEmpty(httpUrl))
			return false;

		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
		if (!string.IsNullOrEmpty(http200Url) && await HttpStatusOk(http, http200Url, require200: true, token))
			return true;
		if (!string.IsNullOrEmpty(httpUrl) && await HttpStatusOk(http, httpUrl, require200: false, token))
			return true;
		return false;
	}

	private static async Task<bool> HttpStatusOk(HttpClient http, string url, bool require200, CancellationToken token)
	{
		try
		{
			using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
			return !require200 || (int)res.StatusCode == 200;
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			throw;
		}
		catch
		{
			return false;
		}
	}

	private static int SafeExitCode(Launched l)
	{
		try { return l.Process.HasExited ? l.Process.ExitCode : 0; }
		catch { return 0; }
	}

	private static string LastLogLine(Launched l)
	{
		foreach (var file in new[] { l.StderrLog, l.StdoutLog })
		{
			try
			{
				if (!File.Exists(file)) continue;
				var last = File.ReadLines(file).LastOrDefault(x => !string.IsNullOrWhiteSpace(x));
				if (!string.IsNullOrEmpty(last)) return Trim(last, 200);
			}
			catch { /* best-effort */ }
		}

		return "";
	}

	/// <summary>
	/// True when the tail of the step's log looks like a Scala service that raced Mongo at startup and lost.
	///
	/// The failure has a specific signature: the JVM throws <c>com.mongodb.MongoTimeoutException: Timed out
	/// after 5000 ms while waiting to connect</c> during index creation on startup, then the service catches it
	/// and parks — process still alive, no port bound (Scala services other than <c>gateway</c> and
	/// <c>analytics-gateway</c> don't set <c>step.port</c>, so <see cref="StepIsDeadOnItsPort"/> can never fire).
	/// The service will never reach the "Service Started" log line, so the readiness gate times out at 120s
	/// and every downstream call to that service 502s under a "Stack is up" banner.
	///
	/// Under a 15-service startup burst this hits 2-3 services stochastically on Windows (the Java Mongo
	/// driver's 5s serverSelectionTimeout is very tight against localhost:27015 when many services connect at
	/// once). A relaunch reliably works because the JVMs are no longer contending, so this justifies treating
	/// the pattern as "provably not serving" — the same bar the port-dead check clears.
	///
	/// Scan the last N bytes only; a full-file read on a 50 MB log per timeout would be wasteful. Also require
	/// that no <c>Service Started</c> or <c>Serving traffic</c> line appears AFTER the exception — a service
	/// that timed out early, retried its own connection and then bound is not what this gate is for.
	/// </summary>
	private static bool LogShowsMongoStartupFailure(Launched l) =>
		LogTailShowsMongoStartupFailure(l.StdoutLog) || LogTailShowsMongoStartupFailure(l.StderrLog);

	/// <summary>
	/// Same detection <see cref="LogShowsMongoStartupFailure"/> makes, but on a single log file — separated so
	/// tests can hand in a plain path without constructing a <c>Launched</c>.
	/// </summary>
	public static bool LogTailShowsMongoStartupFailure(string logPath)
	{
		try
		{
			if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath)) return false;
			var tail = ReadTail(logPath, 64 * 1024);
			if (string.IsNullOrEmpty(tail)) return false;

			var mongoIdx = tail.LastIndexOf("MongoTimeoutException", StringComparison.Ordinal);
			if (mongoIdx < 0)
			{
				// Fall back to the driver's own wording — a slightly earlier version of the driver logged the
				// message without the exception type in front of it in some code paths.
				mongoIdx = tail.LastIndexOf("Timed out after 5000 ms while waiting to connect", StringComparison.Ordinal);
			}
			if (mongoIdx < 0) return false;

			var after = tail.Substring(mongoIdx);
			if (after.Contains("Service Started", StringComparison.Ordinal)
			    || after.Contains("Serving traffic", StringComparison.Ordinal))
			{
				// The service raced Mongo, recovered on its own, and reached ready after all. Not our problem.
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Reads at most <paramref name="maxBytes"/> from the END of the file. Used to scan the readiness log's
	/// tail for a startup-failure signature without paying for a full-file read on every readiness timeout.
	/// </summary>
	private static string ReadTail(string path, int maxBytes)
	{
		try
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			if (fs.Length == 0) return string.Empty;
			var toRead = (int)Math.Min(fs.Length, maxBytes);
			fs.Seek(-toRead, SeekOrigin.End);
			var buf = new byte[toRead];
			var read = fs.Read(buf, 0, toRead);
			return System.Text.Encoding.UTF8.GetString(buf, 0, read);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string Trim(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

	// ----------------------------------------------------------------------------------
	// Teardown (attached exit/Ctrl-C, and the detached bring-up failure path)
	// ----------------------------------------------------------------------------------

	private void TearDown(List<Launched> launched)
	{
		List<Launched> snapshot;
		lock (_launchedLock) snapshot = launched.ToList();
		if (snapshot.Count == 0) return;

		for (var i = snapshot.Count - 1; i >= 0; i--)
		{
			var l = snapshot[i];
			try
			{
				// Tree-kill plus the command-line sweep for detached grandchildren — see KillLaunched.
				KillLaunched(l);

				Log.Information($"[{l.Step.name}] stopped");
			}
			catch (Exception e)
			{
				Log.Warning($"[{l.Step.name}] failed to stop: {e.Message}");
			}
		}

		if (_localWebPackages)
			WebPackagesService.RemoveOverlay(_workspaceRoot);
	}

	private static void KillPid(int pid)
	{
		try { Process.GetProcessById(pid).Kill(entireProcessTree: true); }
		catch { /* already gone */ }
	}

	/// <summary>
	/// Kills one launched step's whole process tree, best-effort. On Windows the tracked pid is usually a shell
	/// wrapper and the runtime grandchild may already have detached from it, so the tree-kill is followed by a
	/// sweep for the step's own command-line token (reusing <c>stop</c>'s per-kind derivation) — otherwise a JVM
	/// outlives its parent and keeps holding the port the caller is about to relaunch onto.
	/// </summary>
	private static void KillLaunched(Launched l)
	{
		try
		{
			if (!l.Process.HasExited)
				l.Process.Kill(entireProcessTree: true);
		}
		catch { /* already gone */ }

		try
		{
			var probe = new LocalStackRunEntry
			{
				name = l.Step.name,
				kind = l.Kind,
				matchToken = l.Step.mainClass,
				workingDirectory = l.WorkingDirectory,
			};
			foreach (var pid in LocalStackProcess.FindByCommandLine(
				         LocalStackStopCommand.BuildKillTokens(probe), LocalStackProcess.ServiceImages))
				KillPid(pid);
		}
		catch { /* best-effort */ }
	}

	private static int SafePid(Launched l)
	{
		try { return l.Process.Id; }
		catch { return 0; }
	}

	/// <summary>
	/// Windows only: rewrites each long-running step's recorded pid from the <c>cmd.exe</c> wrapper to the
	/// real service process (the topmost non-wrapper descendant — the JVM, node, dotnet runner, or gateway
	/// exe), so <c>ps</c>/<c>stop</c> and the next <c>up</c>'s idempotency check track the process that
	/// actually survives (and whose tree-kill takes the whole step down) rather than a wrapper that exits.
	/// No-op on unix (the launcher <c>exec</c>s the service, so the tracked pid already is the service) and
	/// for run-to-completion (docker) steps.
	/// </summary>
	private void ReconcileLeafPids(LocalStackRunState runState, string runStatePath, List<Launched> launched)
	{
		if (!OperatingSystem.IsWindows())
			return;

		var changed = false;
		lock (_launchedLock)
		{
			// ONLY the steps this run launched. A carried-over entry's pid is already the resolved service leaf, and
			// ResolveServiceRootPid always descends to a child — so re-reconciling it would walk the recorded pid
			// one level deeper on every `up`, until `ps`/`stop` were pointing at a helper process (esbuild, a
			// dotnet child) while the real service kept running and holding its port.
			var launchedNames = new HashSet<string>(launched.Select(l => l.Step.name), StringComparer.OrdinalIgnoreCase);

			foreach (var entry in runState.steps)
			{
				if (entry.waitForExit || entry.pid <= 0 || !launchedNames.Contains(entry.name))
					continue;

				var service = LocalStackProcess.ResolveServiceRootPid(entry.pid);
				if (service != entry.pid && service > 0)
				{
					Log.Verbose($"[{entry.name}] tracking service pid={service} (was wrapper pid={entry.pid})");
					entry.pid = service;
					// Re-pin: the start time recorded at launch belongs to the wrapper, not to this leaf.
					entry.startedAtUtcTicks = LocalStackLiveness.StartTicksOf(service);
					changed = true;
				}
			}

			if (changed)
				LocalStackRunStateIO.Save(runStatePath, runState);
		}
	}
}


/// <summary>One row of the checks a run makes before it starts anything.</summary>
public class LocalStackDependencyCheck
{
	public string name;
	public bool ok;
	public string detail;

	/// <summary>Where the dependency came from: <c>toolchain</c>, <c>system</c>, or null when not applicable.</summary>
	public string source;

	/// <summary>The version that was found, when one could be read.</summary>
	public string version;

	/// <summary>True when the finding is worth reporting but does not make the stack unrunnable.</summary>
	public bool warning;

	/// <summary>The <c>beam local setup</c> step that fixes a failed check, or null when setup can't.</summary>
	public string setupId;
}

/// <summary>
/// What a stack needs before it can start: the pinned tools for the kinds of steps it runs (JDK 8 and Maven for
/// Scala, the .NET SDK for C# hosts and microservices, Node for the portal and extensions, pnpm for the web
/// packages), the Docker daemon, the generated BeamableBackend config files, the portal's <c>.env.local</c> and
/// the variables. <c>beam local run</c> fixes the failures it can by running the matching <c>beam local setup</c>
/// step; <c>--dry-run</c> prints them all.
///
/// The important column is <b>source</b>: a dependency satisfied by the <c>toolchain</c> is pinned and
/// reproducible, while one satisfied by the <c>system</c> is whatever that machine happens to have — which is how
/// a Maven running under a JDK 21 from an IDE bundle, or a Node major the portal was never built against, gets
/// used without anyone noticing.
/// </summary>
public static class LocalStackPreflight
{
	/// <summary>The checks for <paramref name="steps"/>. <paramref name="withCliPins"/> adds one row per CLI pin, reported without changing it.</summary>
	public static async Task<List<LocalStackDependencyCheck>> Check(LocalStackConfig config, List<LocalStackStep> steps,
		LocalStackStacks stacks, bool withCliPins)
	{
		bool Uses(string tag) => steps.Any(s => stacks.TagsOf(s).Contains(tag, StringComparer.OrdinalIgnoreCase));

		var checks = new List<LocalStackDependencyCheck>();
		var toolchain = ToolchainService.LoadManifest(Path.Combine(ToolchainService.ResolveDir(null), ToolchainService.ManifestFileName));

		// docker (via the CLI's resolver) — the daemon, not just the binary.
		if (steps.Any(s => string.Equals(s.command, "docker", StringComparison.OrdinalIgnoreCase)))
		{
			if (DockerPathOption.TryGetDockerPath(out var dockerPath, out var dockerErr))
				checks.Add(await CheckProgram("docker", dockerPath, "--version",
					hint: "install Docker Desktop from https://docs.docker.com/get-docker/"));
			else
				checks.Add(new LocalStackDependencyCheck { name = "docker", ok = false, detail = dockerErr });
		}

		var tools = new List<string>();
		if (Uses("scala")) tools.AddRange(new[] { ToolchainPins.Jdk, ToolchainPins.Maven });
		if (Uses("csharp") || Uses("microservices") || Uses("extensions")) tools.Add(ToolchainPins.Dotnet);
		if (Uses("portal") || Uses("extensions") || Uses("web-registry")) tools.Add(ToolchainPins.Node);
		if (Uses("web-registry")) tools.Add(ToolchainPins.Pnpm);

		// Each pinned tool is reported with where it came from and how its version compares to the pin.
		foreach (var toolId in ToolchainPins.ToolIds.Where(tools.Contains))
			checks.Add(CheckTool(toolId, toolchain, config));

		// The generated BeamableBackend config files, which are gitignored and therefore missing on a fresh clone.
		if (Uses("scala"))
			checks.Add(CheckScalaConfig(config?.repos?.scalaDir));

		// The portal's .env.local — same class of gap, and its absence is far more confusing.
		if (Uses("portal"))
			checks.Add(CheckPortalEnv(config?.repos?.portalDir, config?.host));

		checks.Add(CheckVariables(config));

		if (withCliPins)
			checks.AddRange(CheckCliPins(config?.repos?.portalDir));

		return checks;
	}

	public static void Render(List<LocalStackDependencyCheck> checks)
	{
		var table = new Table();
		table.Border(TableBorder.Simple);
		table.AddColumn("[bold]check[/]");
		table.AddColumn("[bold]status[/]");
		table.AddColumn("[bold]source[/]");
		table.AddColumn("[bold]detail[/]");

		foreach (var c in checks)
		{
			var status = c.ok
				? (c.warning ? "[yellow]ok[/]" : "[green]ok[/]")
				: c.setupId != null ? "[red]missing[/] [grey](run sets it up)[/]" : "[red]missing[/]";

			// A system-sourced dependency is highlighted: it works today and can change under you tomorrow.
			var source = c.source switch
			{
				"system" => "[yellow]system[/]",
				null => "",
				_ => $"[green]{Markup.Escape(c.source)}[/]"
			};

			table.AddRow(
				new Markup(Markup.Escape(c.name)),
				new Markup(status),
				new Markup(source),
				new Markup(Markup.Escape(c.detail ?? "")));
		}

		AnsiConsole.Write(table);
	}

	/// <summary>
	/// Variable cycles make the stack unrunnable. A <c>${Name}</c> that matches nothing is only a warning: it is
	/// left as written, which may be intended.
	/// </summary>
	private static LocalStackDependencyCheck CheckVariables(LocalStackConfig config)
	{
		var check = new LocalStackDependencyCheck { name = "variables" };
		var cycles = LocalStackConfigIO.FindVariableCycles(config);
		var unknown = LocalStackConfigIO.FindUnknownVariables(config);

		if (cycles.Count > 0)
		{
			check.ok = false;
			check.detail = string.Join("; ", cycles);
		}
		else if (unknown.Count > 0)
		{
			check.ok = true;
			check.warning = true;
			check.detail = "unknown: " + string.Join(", ", unknown.Select(u => "${" + u.name + "} in " + u.where));
		}
		else
		{
			check.ok = true;
			var count = (config.paths?.Count ?? 0) + (config.variables?.Count ?? 0);
			check.detail = $"{count} defined, all resolve";
		}

		return check;
	}

	/// <summary>
	/// The portal workspace's CLI pins as they are, one row each. A real run re-pins local builds before it starts;
	/// a dry run only reports.
	/// </summary>
	private static IEnumerable<LocalStackDependencyCheck> CheckCliPins(string portalDir)
	{
		var result = LocalStackCliVersions.Check(portalDir, LocalStackCliVersions.RunningVersion(), fix: false);
		foreach (var pin in result.pins)
		{
			yield return new LocalStackDependencyCheck
			{
				name = "cli pin " + DisplayPath(pin.file, portalDir),
				ok = pin.status is "ok" or "repinned" or "mismatch",
				warning = pin.status == "mismatch",
				source = pin.kind,
				version = pin.version,
				detail = pin.detail,
			};
		}

		foreach (var error in result.errors)
			yield return new LocalStackDependencyCheck { name = "cli restore", ok = false, detail = error };
	}

	private static string DisplayPath(string file, string root)
	{
		try
		{
			return string.IsNullOrEmpty(root) ? file : Path.GetRelativePath(root, file).Replace('\\', '/');
		}
		catch
		{
			return file;
		}
	}

	/// <summary>
	/// Resolves one pinned tool the same way the run will: the toolchain first, then the settings, then the
	/// system. Reporting the source is the point — "ok" against a system tool of the wrong version is exactly the
	/// state this exists to make visible.
	/// </summary>
	private static LocalStackDependencyCheck CheckTool(string toolId, ToolchainManifest toolchain, LocalStackConfig config)
	{
		var check = new LocalStackDependencyCheck { name = toolId };

		// 1. Installed in the toolchain.
		if (toolchain.tools.TryGetValue(toolId, out var entry) && !string.IsNullOrEmpty(entry?.home))
		{
			var exe = ToolchainService.ExecutablePath(toolId, entry.home);
			if (exe != null && File.Exists(exe))
			{
				var version = ToolchainService.ReadVersion(toolId, entry.home) ?? entry.version;
				var matches = ToolchainService.SatisfiesPin(toolId, version);
				check.ok = true;
				check.source = entry.source ?? "toolchain";
				check.version = version;
				check.warning = !matches;
				check.detail = matches
					? $"{version} — {entry.home}"
					: $"{version} does not match the pin — {entry.home} (run `beam local setup --only {toolId} --force`)";
				return check;
			}
		}

		// 2. Recorded in the settings but not in the toolchain (hand-edited settings).
		var settingsHome = toolId switch
		{
			ToolchainPins.Jdk => config?.toolchain?.java ?? config?.javaHome,
			ToolchainPins.Maven => config?.toolchain?.maven,
			ToolchainPins.Dotnet => config?.toolchain?.dotnet,
			ToolchainPins.Node => config?.toolchain?.node,
			ToolchainPins.Pnpm => config?.toolchain?.pnpm,
			_ => null
		};

		if (!string.IsNullOrWhiteSpace(settingsHome)
		    && !settingsHome.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal)
		    && File.Exists(ToolchainService.ExecutablePath(toolId, settingsHome) ?? string.Empty))
		{
			check.ok = true;
			check.source = "settings";
			check.version = ToolchainService.ReadVersion(toolId, settingsHome);
			check.detail = $"{check.version ?? "?"} — {settingsHome}";
			return check;
		}

		// 3. Whatever is on the machine. Usable, but unpinned — say so.
		var systemVersion = ReadSystemVersion(toolId, out var systemHome);
		if (systemVersion != null)
		{
			var matches = ToolchainService.SatisfiesPin(toolId, systemVersion);
			check.ok = true;
			check.source = "system";
			check.version = systemVersion;
			check.warning = true;
			check.detail = matches
				? $"{systemVersion} — {systemHome} (unpinned; `beam local setup` installs a private copy)"
				: $"{systemVersion} — {systemHome} — does NOT match the pin ({PinOf(toolId)}); run `beam local setup --only {toolId}`";
			return check;
		}

		check.ok = false;
		check.setupId = toolId;
		check.detail = $"not found — `beam local setup --only {toolId}` installs {PinOf(toolId)}";
		return check;
	}

	/// <summary>The pinned version, for the "what you should have" half of a mismatch message.</summary>
	private static string PinOf(string toolId) => toolId switch
	{
		ToolchainPins.Jdk => $"JDK {ToolchainPins.JavaFeatureVersion}",
		ToolchainPins.Maven => $"Maven {ToolchainPins.MavenVersion}",
		ToolchainPins.Dotnet => $".NET SDK {ToolchainPins.DotnetVersion}",
		ToolchainPins.Node => $"Node {ToolchainPins.NodeMajor}",
		ToolchainPins.Pnpm => $"pnpm {ToolchainPins.PnpmVersion}",
		_ => toolId
	};

	/// <summary>
	/// The version of a tool as installed on the machine, plus the home it was found at. For the JDK this reuses
	/// the CLI's existing Java 8 discovery so the answer matches what the run would resolve.
	/// </summary>
	private static string ReadSystemVersion(string toolId, out string home)
	{
		home = null;

		if (toolId == ToolchainPins.Jdk)
		{
			if (!JavaPathOption.TryGetJavaHome(out var javaHome, out _)) return null;

			home = javaHome;
			return ToolchainService.ReadVersion(toolId, javaHome) ?? "8";
		}

		foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			         .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			string candidate;
			try
			{
				candidate = Path.Combine(dir.Trim(), ToolchainService.ProbeExecutable(toolId));
			}
			catch
			{
				continue;
			}

			if (!File.Exists(candidate)) continue;

			var binDir = Path.GetDirectoryName(Path.GetFullPath(candidate));
			var candidateHome = string.IsNullOrEmpty(ToolchainService.BinSubdir(toolId))
				? binDir
				: Path.GetDirectoryName(binDir);

			var version = ToolchainService.ReadVersion(toolId, candidateHome);
			if (version == null) continue;

			home = candidateHome;
			return version;
		}

		return null;
	}

	/// <summary>
	/// Reports the generated BeamableBackend config files. These are gitignored, so a fresh clone never has them,
	/// and a stack missing them starts the Scala services and then fails on config nobody told the developer to
	/// generate.
	/// </summary>
	private static LocalStackDependencyCheck CheckScalaConfig(string scalaDir)
	{
		var check = new LocalStackDependencyCheck { name = "backend config" };

		if (string.IsNullOrWhiteSpace(scalaDir)
		    || scalaDir.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal)
		    || !Directory.Exists(scalaDir))
		{
			check.ok = true;
			check.warning = true;
			check.detail = "no BeamableBackend path in the settings — cannot check the generated conf files";
			return check;
		}

		var missing = ScalaLocalVarsService.MissingConfigFiles(scalaDir);
		if (missing.Count == 0)
		{
			check.ok = true;
			check.detail = $"all {ScalaLocalVarsService.RelativeConfPaths.Length} generated conf files present";
			return check;
		}

		check.ok = false;
		check.setupId = ToolchainPins.ScalaConfig;
		check.detail = $"missing {string.Join(", ", missing.Select(Path.GetFileName))} — " +
		               "`beam local setup --only scala-config` renders them";
		return check;
	}

	/// <summary>
	/// Reports where the portal will send its API calls. This is a FAILURE, not a warning, when
	/// <c>VITE_API_BASE</c> is unset: the portal then falls back to <c>https://api.beamable.com</c> and logs in
	/// against production, so the local seed account appears not to exist while every local service is healthy.
	/// That symptom is indistinguishable from a broken backend, which is exactly why it needs naming here.
	/// </summary>
	private static LocalStackDependencyCheck CheckPortalEnv(string portalDir, string host)
	{
		var check = new LocalStackDependencyCheck { name = "portal env" };

		if (string.IsNullOrWhiteSpace(portalDir)
		    || portalDir.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal)
		    || !Directory.Exists(portalDir))
		{
			check.ok = true;
			check.warning = true;
			check.detail = "no portal path in the settings — cannot check .env.local";
			return check;
		}

		// The stack passes the API host to the portal itself, so .env.local only matters for a plain `npm run dev`.
		var apiBase = PortalEnvService.ReadApiBase(portalDir);
		check.ok = true;
		check.detail = $"the stack gives the portal {PortalEnvService.ApiBaseKey}={host}; {PortalEnvService.EnvFileName} is left as it is"
		               + (apiBase == null ? string.Empty : $" ({apiBase} there, for a plain npm run dev)");
		return check;
	}

	private static async Task<LocalStackDependencyCheck> CheckProgram(string name, string program, string arguments, string hint)
	{
		var check = new LocalStackDependencyCheck { name = name };
		var output = new StringBuilder();
		try
		{
			var res = await CliWrap.Cli.Wrap(program)
				.WithArguments(arguments)
				.WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
				.WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
				.WithValidation(CommandResultValidation.None)
				.ExecuteAsync();

			check.ok = res.ExitCode == 0;
			check.detail = check.ok
				? output.ToString().Split('\n').FirstOrDefault()?.Trim()
				: $"'{program}' exited {res.ExitCode} — {hint}";
		}
		catch (Exception)
		{
			check.ok = false;
			check.detail = $"'{program}' not found — {hint}";
		}

		return check;
	}
}
