using Beamable.Server;
using cli.Commands.Docs;
using cli.Services;
using cli.Services.LocalStack;
using cli.Utils;
using Scriban;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackSetupCommandArgs : CommandArgs
{
	public string toolchainDir;
	public string only;
	public string skip;
	public bool force;
	public int logRetentionDays;
	public bool preferSystem;
	public bool offline;
	public bool dryRun;
	public string githubToken;
	public string githubRepo;
	public string awsRegion;
	public string configPath;

	/// <summary>Overrides for the repo paths when there is no manifest to read them from yet.</summary>
	public string scalaDir;

	public string apiDir;

	public string portalDir;

	/// <summary>Print the AWS setup guide even when every AWS check passed.</summary>
	public bool awsGuide;

	// The settings file and its step catalogue.
	public string host;
	public string portalUrl;
	public string scalaServices;
	public string scalaJvmArgs;
	public string webRegistryDir;

	// Custom steps. addStep is null when not asked for, and empty when asked for without a name (it is then asked).
	public string addStep;
	public string removeStep;
	public bool listSteps;
	public string stepCommand;
	public string stepArgs;
	public string stepShell;
	public string stepShellKind;
	public string stepBeam;
	public string stepDir;
	public string[] stepEnv;
	public bool stepOnce;
	public string stepReadyUrl;
	public string stepReadyLog;
	public int stepPort;
	public int stepTimeout;
	public string stepTags;
	public string stepLabel;
	public string stepAfter;
}

/// <summary>One step of the settings' catalogue, as <c>--list-steps</c> reports it.</summary>
public class LocalStackCatalogueEntry
{
	public string name;
	public string label;
	public List<string> tags;

	/// <summary><c>process</c>, <c>shell</c> or <c>beam</c>.</summary>
	public string kind;

	/// <summary>True when it runs to completion rather than staying up.</summary>
	public bool once;

	/// <summary>True for a step added with <c>--add-step</c>.</summary>
	public bool custom;

	public bool enabled;
}

/// <summary>One line of the setup report.</summary>
public class LocalStackSetupStepResult
{
	public string name;

	/// <summary>One of <c>installed</c>, <c>cached</c>, <c>system</c>, <c>would install</c>, <c>ok</c>, <c>skipped</c>, <c>failed</c>, <c>warning</c>.</summary>
	public string status;

	public string detail;
	public bool ok;
}

public class LocalStackSetupCommandResult
{
	public string toolchainDir;
	public string manifestPath;

	/// <summary>The catalogue after <c>--add-step</c>, <c>--remove-step</c> or <c>--list-steps</c>; null otherwise.</summary>
	public List<LocalStackCatalogueEntry> catalogue;

	public bool allOk;
	public List<LocalStackSetupStepResult> steps = new List<LocalStackSetupStepResult>();
}

/// <summary>
/// Provisions everything <c>beam local run</c> needs on a machine that has never run it. <c>beam local run</c>
/// checks the same things before it starts a stack and runs the parts of this that are missing, so calling it by
/// hand is optional: it prepares a machine ahead of time or changes a setting.
///
/// Each step exists because the stack silently assumed it:
/// <list type="number">
/// <item>A <b>private, pinned toolchain</b> (JDK 8, Maven, the .NET SDK, Node) downloaded into a directory you
/// choose, so the build no longer follows whatever is on PATH. See <see cref="ToolchainService"/>.</item>
/// <item>The <b>settings file</b>, <c>.beamable/local/stack-settings.json</c>: repo path variables, host and
/// portal URL, and the step catalogue every stack selects from (see <see cref="LocalStackTemplate"/>). Created
/// on the first run; afterwards only its toolchain block is updated, unless <c>--force</c> recreates the
/// catalogue. The workspace's microservices and extensions aren't in it: they're found each time a stack is
/// resolved (see <see cref="WorkspaceServices"/>).</item>
/// <item>The <b>generated BeamableBackend config files</b>, which are gitignored and so absent from every fresh
/// clone. See <see cref="ScalaLocalVarsService"/>.</item>
/// <item>An <b>AWS preflight</b>, because the Scala <c>auth</c> service reads its JWT signing key from Secrets
/// Manager at runtime — so without credentials the whole stack comes up healthy and nothing can log in. See
/// <see cref="AwsPreflightService"/>.</item>
/// </list>
///
/// Docker is deliberately NOT installed: it needs admin rights and a GUI installer on both macOS and Windows.
/// It is checked, and a failure names the installer to run.
///
/// Idempotent — a second run with everything in place does no network I/O.
/// </summary>
public class LocalStackSetupCommand
	: AtomicCommand<LocalStackSetupCommandArgs, LocalStackSetupCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	// It prints its own readable output; the result object still reaches tools through the data channel.
	public override bool AutoLogOutput => false;

	public LocalStackSetupCommand() : base("setup",
		"Prepare this machine for `beam local run`: a private toolchain (JDK 8, Maven, .NET SDK, Node), the settings file with its step catalogue, the backend config files and the AWS prerequisites")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--toolchain-dir",
				$"Directory the pinned dependencies are installed into and reused from (default: ~/{ToolchainService.DefaultDirName}, or ${ToolchainService.EnvVarToolchainDir}). " +
				"Point several workspaces at one directory to share a single install"),
			(args, v) => args.toolchainDir = v);
		AddOption(new Option<string>("--only",
				$"Run only these steps (comma/space separated): {string.Join(", ", ToolchainPins.AllStepIds)}"),
			(args, v) => args.only = v);
		AddOption(new Option<string>("--skip", "Skip these steps (same ids as --only)"),
			(args, v) => args.skip = v);
		AddOption(new Option<int>("--log-retention-days", "How many days the local Loki and Tempo keep logs and traces (default 5); applied on the next start of the docker dependencies"),
			(args, v) => args.logRetentionDays = v);
		AddOption(new Option<bool>("--force", "Re-download and re-install even when a dependency is already present, overwrite generated config files, and recreate the settings' step catalogue (its variables, targets and port offset are kept)"),
			(args, v) => args.force = v);
		AddOption(new Option<bool>("--prefer-system", "Adopt an already-installed dependency when its version matches the pin, instead of downloading a private copy"),
			(args, v) => args.preferSystem = v);
		AddOption(new Option<bool>("--offline", "Never hit the network: install only from archives already in the toolchain's download cache"),
			(args, v) => args.offline = v);
		AddOption(new Option<bool>("--dry-run", "Resolve and report what would be installed, downloading and writing nothing"),
			(args, v) => args.dryRun = v);
		AddOption(new Option<string>("--github-token",
				"Token used to read the BeamableBackend `local` environment variables that the generated config files are rendered from (default: $GITHUB_TOKEN, else `gh auth token`)"),
			(args, v) => args.githubToken = v);
		AddOption(new Option<string>("--github-repo", () => ScalaLocalVarsService.DefaultRepo,
				"Repository whose `local` environment holds the config values"),
			(args, v) => args.githubRepo = v);
		AddOption(new Option<string>("--aws-region", () => AwsPreflightService.DefaultRegion,
				"Region the AWS preflight checks the secret, buckets and queue in"),
			(args, v) => args.awsRegion = v);
		AddOption(new Option<string>("--config", "Path to the settings file (defaults to .beamable/local/stack-settings.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<string>("--scala-dir", "Absolute path to the BeamableBackend (Scala) repo; only needed when the settings don't record it yet"),
			(args, v) => args.scalaDir = v);
		AddOption(new Option<string>("--api-dir", "Absolute path to the BeamableAPI (C# gateway) repo; only needed when the settings don't record it yet"),
			(args, v) => args.apiDir = v);
		AddOption(new Option<string>("--portal-dir", "Absolute path to the portal repo; only needed when the settings don't record it yet"),
			(args, v) => args.portalDir = v);
		AddOption(new Option<bool>("--aws-guide", "Print the AWS setup guide at the end of the run (it is printed automatically whenever an AWS check fails)"),
			(args, v) => args.awsGuide = v);
		AddOption(new Option<string>("--host", "Backend API host written to new settings"),
			(args, v) => args.host = v);
		AddOption(new Option<string>("--portal-url", "Portal frontend URL written to new settings"),
			(args, v) => args.portalUrl = v);
		AddOption(new Option<string>("--scala-services", "Comma/space separated Scala tools/* services for the step catalogue ('default' and 'all' expand)"),
			(args, v) => args.scalaServices = v);
		AddOption(new Option<string>("--scala-jvm-args", () => LocalStackTemplate.DefaultScalaJvmArgs,
				"JVM flags each Scala service is launched with; the heap cap keeps ~18 JDK 8 JVMs from each reserving a quarter of physical RAM"),
			(args, v) => args.scalaJvmArgs = v);
		AddOption(new Option<string>("--web-registry-dir", "Absolute path to the portal-localdev directory holding the web registry compose file (detected next to BeamableProduct otherwise)"),
			(args, v) => args.webRegistryDir = v);

		// Custom steps. --add-step may be given without a name, which an interactive run then asks for; a binder only
		// sees the value, so whether the flag was written at all comes from the parse result.
		var addStep = new Option<string>("--add-step",
			"Add a custom step with this name to the settings' catalogue, from the --step-* options; in a terminal, without a command it asks for everything. Nothing else runs")
		{
			Arity = ArgumentArity.ZeroOrOne
		};
		AddOption(addStep, (args, ctx, v) =>
		{
			if (ctx.ParseResult.FindResultFor(addStep) is { IsImplicit: false })
				args.addStep = v ?? string.Empty;
		});
		AddOption(new Option<string>("--remove-step", "Remove a custom step from the catalogue. Nothing else runs"),
			(args, v) => args.removeStep = v);
		AddOption(new Option<bool>("--list-steps", "List the catalogue: every step a stack can run, with its tags. Nothing else runs"),
			(args, v) => args.listSteps = v);
		AddOption(new Option<string>("--step-command", "Custom step: the program to run (with --step-args)"),
			(args, v) => args.stepCommand = v);
		AddOption(new Option<string>("--step-args", "Custom step: the program's arguments"),
			(args, v) => args.stepArgs = v);
		AddOption(new Option<string>("--step-shell", "Custom step: a shell script to run instead of a program"),
			(args, v) => args.stepShell = v);
		AddOption(new Option<string>("--step-shell-kind", "Custom step: the shell for --step-shell, sh or powershell (default: powershell on Windows, sh elsewhere)"),
			(args, v) => args.stepShellKind = v);
		AddOption(new Option<string>("--step-beam", "Custom step: a beam command to run with this CLI, such as \"content publish\""),
			(args, v) => args.stepBeam = v);
		AddOption(new Option<string>("--step-dir", "Custom step: the folder it runs in; may use variables such as ${PortalPath}"),
			(args, v) => args.stepDir = v);
		AddOption(new Option<string[]>("--step-env", "Custom step: an environment value, KEY=VALUE; repeat for more") { AllowMultipleArgumentsPerToken = true },
			(args, v) => args.stepEnv = v);
		AddOption(new Option<bool>("--step-once", "Custom step: it runs to completion (a seed, a script) instead of staying up; the next step waits for it"),
			(args, v) => args.stepOnce = v);
		AddOption(new Option<string>("--step-ready-url", "Custom step: it is ready once this URL answers"),
			(args, v) => args.stepReadyUrl = v);
		AddOption(new Option<string>("--step-ready-log", "Custom step: it is ready once its output contains this text"),
			(args, v) => args.stepReadyLog = v);
		AddOption(new Option<int>("--step-port", "Custom step: the port it binds, checked before it starts"),
			(args, v) => args.stepPort = v);
		AddOption(new Option<int>("--step-timeout", () => 120, "Custom step: seconds to wait for it to be ready"),
			(args, v) => args.stepTimeout = v);
		AddOption(new Option<string>("--step-tags", "Custom step: comma separated tags besides custom, so stacks can select it by tag"),
			(args, v) => args.stepTags = v);
		AddOption(new Option<string>("--step-label", "Custom step: a readable name for listings (default: from its name)"),
			(args, v) => args.stepLabel = v);
		AddOption(new Option<string>("--step-after", "Custom step: the catalogue step it comes after, which is where a stack runs it (default: the end)"),
			(args, v) => args.stepAfter = v);
	}

	public override async Task<LocalStackSetupCommandResult> GetResult(LocalStackSetupCommandArgs args)
	{
		var only = LocalStackRunCommand.NameSet(args.only);
		var skip = LocalStackRunCommand.NameSet(args.skip);
		bool Included(string id) => (only == null || only.Contains(id)) && (skip == null || !skip.Contains(id));

		// Prompt only when there is an interactive console; otherwise every question takes its default.
		var quiet = args.Quiet || !AnsiConsole.Profile.Capabilities.Interactive;

		// Custom steps are a change to the catalogue alone: nothing is installed or checked.
		if (args.addStep != null || args.removeStep != null || args.listSteps)
			return LocalStackCustomSteps.Manage(args, quiet);

		var manifestPath = LocalStackCommand.ResolveManifestPathForInit(args.ConfigService, args.configPath);
		var config = File.Exists(manifestPath) ? LocalStackConfigIO.Load(manifestPath) : null;

		var result = new LocalStackSetupCommandResult
		{
			toolchainDir = ToolchainService.ResolveDir(args.toolchainDir),
			manifestPath = File.Exists(manifestPath) ? manifestPath : null
		};

		Log.Information($"Toolchain directory: {result.toolchainDir} ({ToolchainPins.PlatformLabel})");

		// Docker first: it gates most of the stack and cannot be provisioned here, so knowing it is missing before
		// spending several minutes downloading a JDK is worth the two seconds.
		if (Included(ToolchainPins.Docker))
			result.steps.Add(CheckDocker());

		var service = new ToolchainService(result.toolchainDir, new ToolchainOptions
		{
			force = args.force,
			offline = args.offline,
			preferSystem = args.preferSystem,
			dryRun = args.dryRun
		});

		foreach (var toolId in ToolchainPins.ToolIds)
		{
			if (!Included(toolId))
			{
				result.steps.Add(new LocalStackSetupStepResult { name = toolId, status = "skipped", detail = "excluded by --only/--skip", ok = true });
				continue;
			}

			var outcome = await service.EnsureAsync(toolId, CancellationToken.None);
			result.steps.Add(ToStep(outcome));
		}

		// Only a tool step changes the toolchain directory; `--only settings` must not create or rewrite it.
		if (ToolchainPins.ToolIds.Any(Included))
			service.SaveManifest();

		// Write or update the settings file. Done before the remaining steps, which read the repo paths from it, and
		// so a later failure (e.g. no GitHub token) still leaves the toolchain wired up.
		if (Included(ToolchainPins.Settings))
		{
			var (settingsStep, written) = await WriteSettings(args, manifestPath, config, quiet);
			result.steps.Add(settingsStep);
			config = written;
		}
		else
			result.steps.Add(new LocalStackSetupStepResult { name = ToolchainPins.Settings, status = "skipped", detail = "excluded by --only/--skip", ok = true });

		result.manifestPath = File.Exists(manifestPath) ? manifestPath : null;

		if (Included(ToolchainPins.Skill))
			result.steps.Add(WriteSkill(args, manifestPath));
		else
			result.steps.Add(new LocalStackSetupStepResult { name = ToolchainPins.Skill, status = "skipped", detail = "excluded by --only/--skip", ok = true });

		// Repo paths: an explicit option wins, then the settings, then a walk up the directory tree for the
		// well-known checkout names, so the steps below work even when the settings step was skipped.
		var startDir = args.ConfigService?.WorkingDirectory ?? Directory.GetCurrentDirectory();
		var scalaDir = FirstUsable(args.scalaDir, config?.repos?.scalaDir,
			LocalStackCommand.FindRepoDir(startDir, LocalStackCommand.ScalaRepoName));
		var apiDir = FirstUsable(args.apiDir, config?.repos?.apiDir,
			LocalStackCommand.FindRepoDir(startDir, LocalStackCommand.ApiRepoName));
		var portalDir = FirstUsable(args.portalDir, config?.repos?.portalDir,
			LocalStackCommand.FindRepoDir(startDir, LocalStackCommand.PortalRepoName));

		if (Included(ToolchainPins.ScalaConfig))
			result.steps.Add(await RenderScalaConfig(args, scalaDir));
		else
			result.steps.Add(new LocalStackSetupStepResult { name = ToolchainPins.ScalaConfig, status = "skipped", detail = "excluded by --only/--skip", ok = true });

		if (Included(ToolchainPins.PortalConfig))
			result.steps.Add(EnsurePortalEnv(args, config, portalDir));
		else
			result.steps.Add(new LocalStackSetupStepResult { name = ToolchainPins.PortalConfig, status = "skipped", detail = "excluded by --only/--skip", ok = true });

		if (Included(ToolchainPins.Aws))
			result.steps.AddRange(RunAwsPreflight(args, scalaDir, apiDir));
		else
			result.steps.Add(new LocalStackSetupStepResult { name = ToolchainPins.Aws, status = "skipped", detail = "excluded by --only/--skip", ok = true });

		result.allOk = result.steps.All(s => s.ok);
		Render(result, args.dryRun);

		// The guide goes AFTER the table, where it is read as "here is how to fix the red rows" rather than as
		// boilerplate scrolling past before the result. Shown whenever an AWS row failed, or on request.
		var awsFailed = result.steps.Any(st => st.name.StartsWith("aws", StringComparison.OrdinalIgnoreCase)
		                                       && st.status is "failed" or "warning");
		if (args.awsGuide || (!args.dryRun && awsFailed))
		{
			// Written straight to the console rather than through a Spectre panel: the guide is pre-wrapped
			// prose with URLs and commands in it, and a panel re-wraps to its inner width — which split the
			// install URL across two lines and broke sentences mid-word.
			Console.WriteLine();
			Console.WriteLine(new AwsPreflightService(args.awsRegion).BuildSetupGuide(scalaDir));
		}

		return result;
	}

	/// <summary>The first value that is neither empty nor an unedited <c>&lt;EDIT: ...&gt;</c> placeholder.</summary>
	private static string FirstUsable(params string[] candidates) =>
		candidates.FirstOrDefault(c =>
			!string.IsNullOrWhiteSpace(c) && !c.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal));

	private static LocalStackSetupStepResult ToStep(ToolchainResult outcome)
	{
		if (!outcome.ok)
			return new LocalStackSetupStepResult { name = outcome.toolId, status = "failed", detail = outcome.error, ok = false };

		var version = outcome.entry?.version ?? "?";
		var home = outcome.entry?.home;
		return new LocalStackSetupStepResult
		{
			name = outcome.toolId,
			status = outcome.action,
			detail = home == null ? version : $"{version} — {home}",
			ok = true
		};
	}

	/// <summary>
	/// Verifies the Docker daemon is reachable (not merely that the CLI exists — a stopped Docker Desktop has a
	/// working <c>docker</c> binary and every compose step still fails).
	/// </summary>
	private static LocalStackSetupStepResult CheckDocker()
	{
		if (!DockerPathOption.TryGetDockerPath(out var dockerPath, out var dockerError))
		{
			return new LocalStackSetupStepResult
			{
				name = "docker",
				status = "failed",
				ok = false,
				detail = dockerError + " — install Docker Desktop (https://docs.docker.com/get-docker/); " +
				         "setup cannot install it for you because it needs administrator rights."
			};
		}

		var version = ToolchainServiceProbe(dockerPath, "info --format {{.ServerVersion}}");
		if (version == null)
		{
			return new LocalStackSetupStepResult
			{
				name = "docker",
				status = "failed",
				ok = false,
				detail = $"{dockerPath} is installed but the daemon is not responding — start Docker and re-run."
			};
		}

		return new LocalStackSetupStepResult { name = "docker", status = "ok", detail = $"daemon {version}", ok = true };
	}

	/// <summary>Runs a command and returns its first output line, or null when it fails.</summary>
	private static string ToolchainServiceProbe(string exe, string arguments)
	{
		try
		{
			var psi = new System.Diagnostics.ProcessStartInfo
			{
				FileName = exe,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = System.Diagnostics.Process.Start(psi);
			if (proc == null) return null;

			var output = proc.StandardOutput.ReadToEnd();
			proc.StandardError.ReadToEnd();
			if (!proc.WaitForExit(30_000)) return null;

			return proc.ExitCode == 0 ? output?.Trim().Split('\n').FirstOrDefault()?.Trim() : null;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Creates the settings file, or updates the one that's there. A new file gets the step catalogue, built from
	/// the repos found next to this workspace and the services picked here. An existing one only gets its toolchain
	/// block refreshed, so a developer's edited steps, variables and JVM flags survive a setup run; <c>--force</c>
	/// recreates the catalogue and <c>--update-services</c> changes just the microservice and extension steps.
	/// </summary>
	private static async Task<(LocalStackSetupStepResult step, LocalStackConfig config)> WriteSettings(
		LocalStackSetupCommandArgs args, string path, LocalStackConfig config, bool quiet)
	{
		var toolchain = ToolchainService.TryReadInstalled(args.toolchainDir);

		if (args.dryRun)
		{
			var plan = config == null ? $"would create {path}"
				: args.force ? $"would recreate the step catalogue in {path}"
				: $"would point {path} at the toolchain";
			return (new LocalStackSetupStepResult { name = ToolchainPins.Settings, status = "would install", ok = true, detail = plan }, config);
		}

		string detail;
		if (config == null || args.force)
		{
			if (config != null && !quiet && !AnsiConsole.Confirm($"Recreate the step catalogue in [yellow]{path}[/]? Its variables, targets and port offset are kept.", defaultValue: false))
				return (new LocalStackSetupStepResult { name = ToolchainPins.Settings, status = "skipped", ok = true, detail = "kept the existing settings" }, config);

			var created = config == null;
			config = await CreateSettings(args, path, config, toolchain, quiet);
			detail = $"{(created ? "created" : "recreated")} with {config.steps.Count} steps";
		}
		else
		{
			if (toolchain != null)
			{
				config.toolchain = toolchain;
				// javaHome is what the ${java} token reads and what `run` checks before auto-detection, so mirror the
				// toolchain's JDK into it — otherwise a system JDK 8 could still win.
				if (!string.IsNullOrWhiteSpace(toolchain.java))
					config.javaHome = toolchain.java;
			}

			LocalStackConfigIO.Save(path, config);
			detail = toolchain != null ? "uses the toolchain" : "kept as is (no toolchain installed)";
		}

		if (args.logRetentionDays > 0 && config.logRetentionDays != args.logRetentionDays)
		{
			config.logRetentionDays = args.logRetentionDays;
			LocalStackConfigIO.Save(path, config);
			detail += $"; logs and traces kept {config.logRetentionDays} days";
		}

		return (new LocalStackSetupStepResult { name = ToolchainPins.Settings, status = "ok", ok = true, detail = $"{path} {detail}" }, config);
	}

	/// <summary>
	/// Builds a new settings file: repo paths found next to the workspace (asked for when interactive), the host and
	/// portal URL, and the step catalogue with the curated Scala services found in BeamableBackend (or
	/// <c>--scala-services</c>). Nothing about what runs is asked: stacks choose that, and the workspace's microservices
	/// and extensions are found each time one is resolved. Recreating keeps what only a person writes: variables, targets, port offset and log retention.
	/// </summary>
	private static async Task<LocalStackConfig> CreateSettings(LocalStackSetupCommandArgs args, string path,
		LocalStackConfig existing, LocalStackToolchain toolchain, bool quiet)
	{
		var defaults = new LocalStackTemplate.Options();

		// Adding a `.beamable` directory to someone's project is a visible change, so an interactive run asks first.
		if (!LocalStackCommand.EnsureManifestDirectory(path,
			    confirm: dir => quiet || AnsiConsole.Confirm($"[green]{dir}[/] does not exist yet. Create it?", defaultValue: true),
			    out var settingsDir))
			throw new CliException($"Aborted — {settingsDir} was not created, so there is nowhere to write the settings.");

		// Repo paths: found by searching parent folders for the well-known repo names, shown as the default; an
		// empty answer writes an <EDIT: ...> placeholder.
		var startDir = args.ConfigService?.WorkingDirectory ?? Directory.GetCurrentDirectory();
		var apiDir = Ask("Absolute path to the [green]BeamableAPI[/] (C# gateway) repo [grey](empty = placeholder)[/]:",
			args.apiDir, LocalStackCommand.FindRepoDir(startDir, LocalStackCommand.ApiRepoName), quiet, allowEmpty: true);
		var scalaDir = Ask("Absolute path to the [green]BeamableBackend[/] (Scala) repo [grey](empty = placeholder)[/]:",
			args.scalaDir, LocalStackCommand.FindRepoDir(startDir, LocalStackCommand.ScalaRepoName), quiet, allowEmpty: true);
		var portalDir = Ask("Absolute path to the [green]portal frontend[/] repo [grey](empty = placeholder)[/]:",
			args.portalDir, LocalStackCommand.FindRepoDir(startDir, LocalStackCommand.PortalRepoName), quiet, allowEmpty: true);
		var webRegistryDir = ResolveWebRegistryDir(args, startDir);

		var host = Ask("Backend API [green]host[/]:", args.host, existing?.host ?? defaults.host, quiet, allowEmpty: false);
		var portalUrl = Ask("[green]Portal[/] frontend URL:", args.portalUrl, existing?.portalUrl ?? defaults.portalUrl, quiet, allowEmpty: false);

		if (toolchain != null)
			Log.Information($"Using the toolchain at {toolchain.dir}.");

		// Scala services: auto-discover the tools/* services from the repo (name + main class) and default to
		// the curated set that's actually present; fall back to the static list when nothing is discovered.
		var discovered = LocalStackTemplate.DiscoverScalaTools(NullIfEmpty(scalaDir));
		var defaultScalaNames = ResolveDefaultScalaNames(discovered);
		if (discovered.Count > 0)
			Log.Information($"Discovered {discovered.Count} Scala tools under {scalaDir}.");

		// The curated set found in the repo: tools/ also holds bots, load tests and retired services.
		var discoveredNames = discovered.Select(t => t.name).ToList();
		var scalaServices = args.scalaServices != null
			? ExpandScalaKeywords(args.scalaServices, discoveredNames, defaultScalaNames)
			: string.Join(" ", defaultScalaNames);

		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			host = host,
			portalUrl = portalUrl,
			apiDir = NullIfEmpty(apiDir),
			scalaDir = NullIfEmpty(scalaDir),
			portalDir = NullIfEmpty(portalDir),
			scalaTools = ToScalaTools(Split(scalaServices), discovered),
			toolchain = toolchain,
			javaHome = toolchain?.java,
			scalaJvmArgs = args.scalaJvmArgs ?? defaults.scalaJvmArgs,
			// Always emit the web steps: a stack runs them through the `web-registry` tag, such as `:web`.
			includeWebRegistry = true,
			webRegistryDir = NullIfEmpty(webRegistryDir),
		});

		if (existing != null)
		{
			config.variables = existing.variables;
			config.targets = existing.targets;
			config.portOffset = existing.portOffset;
			config.logRetentionDays = existing.logRetentionDays;
			config.dockerWatchdogSeconds = existing.dockerWatchdogSeconds;
			LocalStackCustomSteps.Keep(existing, config);
		}

		LocalStackConfigIO.Save(path, config);
		EnsureGitignore(Path.GetDirectoryName(path));
		Log.Information($"Wrote local-stack settings to {path} ({config.steps.Count} steps).");
		return LocalStackConfigIO.Load(path);
	}

	/// <summary>
	/// The <c>portal-localdev</c> directory for the web-registry steps: <c>--web-registry-dir</c> when given,
	/// otherwise the one inside the BeamableProduct checkout found next to this workspace. Null when there is none,
	/// which writes an <c>&lt;EDIT: ...&gt;</c> placeholder like the other repo paths.
	/// </summary>
	private static string ResolveWebRegistryDir(LocalStackSetupCommandArgs args, string startDir)
	{
		if (!string.IsNullOrWhiteSpace(args.webRegistryDir))
			return args.webRegistryDir;

		var productDir = LocalStackCommand.FindRepoDir(startDir, LocalStackCommand.ProductRepoName);
		var detected = productDir == null ? null : Path.Combine(productDir, "portal-localdev");
		return detected != null && Directory.Exists(detected) ? detected : null;
	}

	/// <summary>
	/// Embedded by <c>cli.csproj</c>. Not under <c>Docs/SkillTemplates</c>: those are the MCP server's and
	/// <c>beam install-ai-skill</c>'s skills, which customers get, and <c>beam local</c> is internal.
	/// </summary>
	private const string SkillTemplateResource = "cli.Docs.LocalStack.beam-local-stacks.md.scriban";

	/// <summary>
	/// Writes the <c>beam-local-stacks</c> agent skill to <c>&lt;workspace&gt;/.claude/skills/beam-local-stacks/SKILL.md</c>,
	/// rendered from this CLI's own commands so its option tables match the installed version. Overwritten on every
	/// run, since it's generated. The workspace is the folder holding <c>.beamable</c>; setup is standalone, so outside
	/// a registered workspace it is found from the settings file's location.
	/// </summary>
	private static LocalStackSetupStepResult WriteSkill(LocalStackSetupCommandArgs args, string manifestPath)
	{
		var root = args.ConfigService?.DirectoryExists == true && !string.IsNullOrEmpty(args.ConfigService.BeamableWorkspace)
			? args.ConfigService.BeamableWorkspace
			: LocalStackConfigIO.FindWorkspaceRoot(manifestPath) ?? Directory.GetCurrentDirectory();
		var path = Path.Combine(root, ".claude", "skills", "beam-local-stacks", "SKILL.md");

		if (args.dryRun)
			return new LocalStackSetupStepResult { name = ToolchainPins.Skill, status = "would install", ok = true, detail = path };

		try
		{
			using var stream = typeof(LocalStackSetupCommand).Assembly.GetManifestResourceStream(SkillTemplateResource)
				?? throw new InvalidOperationException($"{SkillTemplateResource} is not embedded");
			using var reader = new StreamReader(stream);
			var template = Template.Parse(reader.ReadToEnd());
			var data = new SkillTemplateData
			{
				Commands = GenerateSkillDocsCommand.BuildCommandsDictionary(args.DependencyProvider.GetService<CliGenerator>())
			};

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, GenerateSkillDocsCommand.Render(template, data));
			return new LocalStackSetupStepResult { name = ToolchainPins.Skill, status = "ok", ok = true, detail = path };
		}
		catch (Exception e)
		{
			return new LocalStackSetupStepResult { name = ToolchainPins.Skill, status = "failed", ok = false, detail = $"{path}: {e.Message}" };
		}
	}

	/// <summary>Ensures the settings directory's <c>.gitignore</c> ignores the generated run-state + logs.</summary>
	private static void EnsureGitignore(string dir)
	{
		try
		{
			if (string.IsNullOrEmpty(dir)) return;
			var gitignorePath = Path.Combine(dir, ".gitignore");
			var wanted = new[] { LocalStackRunStateIO.LogsDirName + "/", LocalStackRunStateIO.RunStateFileName };

			var existing = File.Exists(gitignorePath)
				? File.ReadAllLines(gitignorePath).Select(l => l.Trim()).ToHashSet()
				: new HashSet<string>();

			var toAdd = wanted.Where(e => !existing.Contains(e)).ToList();
			if (toAdd.Count == 0) return;

			var block = new List<string> { "", "# Beamable local-stack generated artifacts" };
			block.AddRange(toAdd);
			File.AppendAllText(gitignorePath, string.Join(Environment.NewLine, block) + Environment.NewLine);
			Log.Information($"Added local-stack artifacts to {gitignorePath}.");
		}
		catch (Exception e)
		{
			Log.Verbose($"could not update .gitignore: {e.Message}");
		}
	}

	private static List<string> Split(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

	private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

	/// <summary>
	/// The default Scala selection: discovered tools whose names are in the curated <see cref="LocalStackTemplate.DefaultScalaServices"/>
	/// (so we keep the known-good set but with resolved main classes); all discovered tools if that intersection
	/// is empty; and the static curated list when nothing was discovered.
	/// </summary>
	private static List<string> ResolveDefaultScalaNames(List<LocalStackTemplate.ScalaToolInfo> discovered)
	{
		if (discovered == null || discovered.Count == 0)
			return LocalStackTemplate.DefaultScalaServices.ToList();

		var curated = new HashSet<string>(LocalStackTemplate.DefaultScalaServices, StringComparer.OrdinalIgnoreCase);
		var inCurated = discovered.Where(t => curated.Contains(t.name)).Select(t => t.name).ToList();
		return inCurated.Count > 0 ? inCurated : discovered.Select(t => t.name).ToList();
	}

	/// <summary>
	/// Discovers the local microservice/portal-extension ids and service groups in the current <c>.beamable</c>
	/// workspace by loading the beamo manifest (no network). Best-effort: returns empty results when run outside
	/// a workspace or if the manifest can't be read, so setup stays usable anywhere.
	/// </summary>
	public static async Task<(List<string> services, List<string> extensions, Dictionary<string, string[]> groups)> DiscoverWorkspaceServices(CommandArgs args)
	{
		var services = new List<string>();
		var extensions = new List<string>();
		var groups = new Dictionary<string, string[]>();
		try
		{
			if (args.ConfigService?.DirectoryExists != true)
				return (services, extensions, groups);

			await args.BeamoLocalSystem.InitManifest(useManifestCache: true, fetchServerManifest: false);
			var manifest = args.BeamoLocalSystem.BeamoManifest;
			var defs = manifest?.ServiceDefinitions ?? new List<BeamoServiceDefinition>();

			services = defs.Where(d => d.Protocol == BeamoProtocolType.HttpMicroservice)
				.Select(d => d.BeamoId).OrderBy(x => x).ToList();
			extensions = defs.Where(d => d.Protocol == BeamoProtocolType.PortalExtension)
				.Select(d => d.BeamoId).OrderBy(x => x).ToList();
			groups = manifest?.ServiceGroupToBeamoIds != null
				? new Dictionary<string, string[]>(manifest.ServiceGroupToBeamoIds, StringComparer.OrdinalIgnoreCase)
				: new Dictionary<string, string[]>();
		}
		catch (Exception e)
		{
			Log.Verbose($"local setup: could not discover workspace services/extensions: {e.Message}");
		}

		return (services, extensions, groups);
	}

	/// <summary>
	/// Merges the extensions found by scanning a portal checkout into the ones the beamo manifest reported.
	///
	/// The beamo manifest only covers the workspace the command RUNS in, so running setup anywhere but the portal
	/// repo discovered nothing and the picker came up empty — which reads as "this project has no extensions"
	/// rather than "you are standing somewhere else". Scanning the checkout that was actually pointed at fixes that.
	/// </summary>
	public static (List<string> extensions, Dictionary<string, string[]> groups) MergePortalExtensions(
		string portalDir, List<string> discoveredExtensions, Dictionary<string, string[]> discoveredGroups)
	{
		var (scannedExtensions, scannedGroups) = LocalStackTemplate.DiscoverPortalExtensions(portalDir);

		var extensions = (discoveredExtensions ?? new List<string>())
			.Concat(scannedExtensions)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();

		var groups = discoveredGroups != null
			? new Dictionary<string, string[]>(discoveredGroups, StringComparer.OrdinalIgnoreCase)
			: new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

		foreach (var (group, members) in scannedGroups)
		{
			// A group the workspace already knows about wins — that membership is authoritative.
			if (!groups.ContainsKey(group)) groups[group] = members;
		}

		return (extensions, groups);
	}

	/// <summary>Maps the selected Scala service names to <see cref="LocalStackTemplate.ScalaToolInfo"/>, attaching
	/// the discovered main class where known (unknown names get a null main class → the launch shell greps pom.xml).</summary>
	private static List<LocalStackTemplate.ScalaToolInfo> ToScalaTools(List<string> names, List<LocalStackTemplate.ScalaToolInfo> discovered)
	{
		if (names == null) return null;
		var byName = (discovered ?? new List<LocalStackTemplate.ScalaToolInfo>())
			.ToDictionary(t => t.name, StringComparer.OrdinalIgnoreCase);
		return names
			.Select(n => byName.TryGetValue(n, out var info)
				? info
				: new LocalStackTemplate.ScalaToolInfo { name = n })
			.ToList();
	}

	/// <summary>
	/// Resolves one value: a passed option wins; otherwise in quiet mode the default is used; otherwise
	/// the user is prompted with the default shown (Enter accepts it).
	/// </summary>
	private static string Ask(string title, string passed, string def, bool quiet, bool allowEmpty)
	{
		// A non-null value was explicitly provided on the command line (even ""), so honor it verbatim —
		// this lets `--extensions ""` clear the list rather than falling back to the default.
		if (passed != null) return passed;
		if (quiet) return def ?? string.Empty;

		var prompt = new TextPrompt<string>(title).PromptStyle("green");
		if (!string.IsNullOrEmpty(def)) prompt.DefaultValue(def);
		if (allowEmpty || string.IsNullOrEmpty(def)) prompt.AllowEmpty();
		return AnsiConsole.Prompt(prompt);
	}

	/// <summary>Expands the "default"/"all" keywords in a passed --scala-services value to their name sets and
	/// de-duplicates the result (so e.g. "default auth" = the curated set plus auth, with no repeats).</summary>
	private static string ExpandScalaKeywords(string passed,
		List<string> discoveredNames, List<string> defaultNames)
	{
		var tokens = Split(passed);
		if (tokens == null) return passed; // empty/whitespace → honor verbatim (clears the list)

		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var ordered = new List<string>();
		void Add(string n)
		{
			if (seen.Add(n))
			{
				ordered.Add(n);
			}
		}

		foreach (var t in tokens)
		{
			if (string.Equals(t, "all", StringComparison.OrdinalIgnoreCase))
			{
				foreach (var n in discoveredNames.Count > 0 ? discoveredNames : defaultNames)
				{
					Add(n);
				}
			}
			else if (string.Equals(t, "default", StringComparison.OrdinalIgnoreCase))
			{
				foreach (var n in defaultNames)
				{
					Add(n);
				}
			}
			else
			{
				Add(t);
			}
		}

		return string.Join(" ", ordered);
	}

	private static async Task<LocalStackSetupStepResult> RenderScalaConfig(LocalStackSetupCommandArgs args, string scalaDir)
	{
		if (scalaDir == null)
		{
			return new LocalStackSetupStepResult
			{
				name = ToolchainPins.ScalaConfig,
				status = "skipped",
				ok = true,
				detail = "no BeamableBackend path known — pass --scala-dir, or let setup create the settings first"
			};
		}

		var service = new ScalaLocalVarsService();
		var token = ScalaLocalVarsService.ResolveToken(args.githubToken);

		if (args.dryRun)
		{
			var missing = ScalaLocalVarsService.MissingConfigFiles(scalaDir);
			return new LocalStackSetupStepResult
			{
				name = ToolchainPins.ScalaConfig,
				status = "would install",
				ok = true,
				detail = missing.Count == 0
					? "all generated config files already present"
					: $"would render {missing.Count} file(s): {string.Join(", ", missing.Select(Path.GetFileName))}"
			};
		}

		var outcome = await service.RenderAsync(scalaDir, args.githubRepo, token, args.force, CancellationToken.None);
		if (!outcome.ok)
		{
			return new LocalStackSetupStepResult
			{
				name = ToolchainPins.ScalaConfig,
				status = "failed",
				ok = false,
				detail = outcome.error
			};
		}

		var parts = new List<string>();
		if (outcome.written.Count > 0) parts.Add($"rendered {outcome.written.Count}");
		if (outcome.skipped.Count > 0) parts.Add($"kept {outcome.skipped.Count} existing (use --force to overwrite)");
		if (outcome.missingVars.Count > 0) parts.Add($"{outcome.missingVars.Count} variable(s) missing and rendered empty");

		return new LocalStackSetupStepResult
		{
			name = ToolchainPins.ScalaConfig,
			status = outcome.missingVars.Count > 0 ? "warning" : "ok",
			ok = true,
			detail = parts.Count == 0 ? "nothing to do" : string.Join("; ", parts)
		};
	}

	/// <summary>
	/// Reports what the portal's gitignored <c>.env.local</c> says about the backend, without changing it. A stack
	/// gives the portal its API host as a process variable, which Vite puts ahead of the file, so the file only
	/// decides where a plain <c>npm run dev</c> goes. Unset, that is production, so it says so.
	/// </summary>
	private static LocalStackSetupStepResult EnsurePortalEnv(LocalStackSetupCommandArgs args, LocalStackConfig config,
		string portalDir)
	{
		if (portalDir == null)
		{
			return new LocalStackSetupStepResult
			{
				name = ToolchainPins.PortalConfig,
				status = "skipped",
				ok = true,
				detail = "no portal path known — pass --portal-dir, or let setup create the settings first"
			};
		}

		var current = PortalEnvService.ReadApiBase(portalDir);
		return new LocalStackSetupStepResult
		{
			name = ToolchainPins.PortalConfig,
			status = current == null ? "warning" : "ok",
			ok = true,
			detail = current == null
				? $"{PortalEnvService.ApiBaseKey} isn't set in {PortalEnvService.EnvFileName}: a plain npm run dev would use " +
				  $"{PortalEnvService.ProductionApiBase}. Stacks give the portal their own host, so this only matters outside a stack."
				: $"{PortalEnvService.ApiBaseKey}={current} in {PortalEnvService.EnvFileName}, for a plain npm run dev; stacks give the portal their own host"
		};
	}

	private static IEnumerable<LocalStackSetupStepResult> RunAwsPreflight(LocalStackSetupCommandArgs args,
		string scalaDir, string apiDir)
	{
		if (args.dryRun)
		{
			yield return new LocalStackSetupStepResult
			{
				name = "aws",
				status = "would install",
				ok = true,
				detail = "would check credentials, assume-role, the JWT signing secret, the buckets and the scheduler queue"
			};
			yield break;
		}

		var preflight = new AwsPreflightService(args.awsRegion).Run(scalaDir, apiDir);
		foreach (var check in preflight.checks)
		{
			yield return new LocalStackSetupStepResult
			{
				name = $"aws: {check.name}",
				status = check.ok ? "ok" : check.warning ? "warning" : "failed",
				// A warning does not fail the run: a bucket this developer's realm never touches should not block
				// a setup that is otherwise complete.
				ok = check.ok || check.warning,
				// Lead with what AWS actually said, then the suggested fix. The remediation is an educated guess at
				// the cause; the raw error is the fact.
				detail = check.ok
					? check.detail
					: string.Join(" — ", new[] { check.detail, check.awsError, check.remediation }
						.Where(s => !string.IsNullOrWhiteSpace(s)))
			};
		}
	}

	private static void Render(LocalStackSetupCommandResult result, bool dryRun)
	{
		LocalStackStyle.H1(dryRun ? "beam local setup: what it would do" : "beam local setup");
		var table = new Table();
		table.Border(TableBorder.Simple);
		table.AddColumn("[bold]step[/]");
		table.AddColumn("[bold]status[/]");
		table.AddColumn("[bold]detail[/]");

		foreach (var step in result.steps)
		{
			var colour = step.status switch
			{
				"failed" => "red",
				"warning" => "yellow",
				"skipped" => "grey",
				_ => "green"
			};

			table.AddRow(
				new Markup(Markup.Escape(step.name ?? "")),
				new Markup($"[{colour}]{Markup.Escape(step.status ?? "")}[/]"),
				new Markup(Markup.Escape(step.detail ?? "")));
		}

		AnsiConsole.Write(table);

		if (dryRun)
		{
			Log.Information("Dry run — nothing was downloaded or written. Re-run without --dry-run to apply.");
			return;
		}

		if (result.allOk)
			Log.Information("Local stack setup complete. Next: `beam local run` lists the stacks; `beam local run full:build` starts everything.");
		else
			Log.Warning("Local stack setup finished with failures — see the table above. Each row names the action that fixes it.");
	}
}


/// <summary>
/// Custom steps: anything a developer or an agent wants a stack to start besides the generated Beamable steps (a seed
/// script, a mock server, a watcher, a <c>beam</c> command), added to the settings' catalogue with
/// <c>beam local setup --add-step</c>. They are steps like any other, marked <see cref="LocalStackStep.custom"/> and
/// tagged <c>custom</c>, so they survive <c>setup --force</c> and are the only steps <c>--remove-step</c> removes.
/// </summary>
public static class LocalStackCustomSteps
{
	public const string Tag = "custom";

	/// <summary>Runs <c>--add-step</c>, <c>--remove-step</c> and <c>--list-steps</c> against the settings, and nothing else.</summary>
	public static LocalStackSetupCommandResult Manage(LocalStackSetupCommandArgs args, bool quiet)
	{
		var path = LocalStackCommand.ResolveManifestPathForInit(args.ConfigService, args.configPath);
		if (!File.Exists(path))
			throw new CliException($"No settings at {path} yet. Run `beam local setup --only settings` first: custom steps go into its catalogue.");

		var config = LocalStackConfigIO.Load(path);
		var result = new LocalStackSetupCommandResult
		{
			manifestPath = path, toolchainDir = ToolchainService.ResolveDir(args.toolchainDir), allOk = true
		};

		if (!string.IsNullOrWhiteSpace(args.removeStep))
		{
			Remove(config, args.removeStep.Trim());
			LocalStackConfigIO.Save(path, config);
			Log.Information($"Removed custom step '{args.removeStep.Trim()}' from {path}.");
		}

		if (args.addStep != null)
		{
			var added = NeedsWizard(args, quiet)
				? AskForStep(config, args.addStep)
				: FromFlags(args);
			if (added.step != null)
			{
				Add(config, added.step, added.after);
				LocalStackConfigIO.Save(path, config);
				Log.Information($"Added custom step '{added.step.name}' to {path}. Use it in a stack by name or by the `{Tag}` tag, for example `beam local create <stack>:<variant> --after \"{added.step.name}\"`.");

				var unknown = LocalStackConfigIO.FindUnknownVariables(config)
					.Where(u => u.where == $"step '{added.step.name}'").Select(u => "${" + u.name + "}").ToList();
				if (unknown.Count > 0)
					Log.Warning($"'{added.step.name}' uses {string.Join(", ", unknown)}, which nothing defines yet. Add them under \"variables\" in {path}.");
			}
			else
			{
				Log.Information("Nothing was added.");
			}
		}

		result.catalogue = Catalogue(config);
		if (args.listSteps)
			Print(result.catalogue);
		return result;
	}

	private static bool NeedsWizard(LocalStackSetupCommandArgs args, bool quiet) =>
		!quiet && args.stepCommand == null && args.stepShell == null && args.stepBeam == null;

	/// <summary>The step the <c>--step-*</c> options describe.</summary>
	public static (LocalStackStep step, string after) FromFlags(LocalStackSetupCommandArgs args)
	{
		var kinds = new[] { args.stepCommand, args.stepShell, args.stepBeam }.Count(v => v != null);
		if (kinds == 0)
			throw new CliException("A custom step needs what to run: --step-command (a program, with --step-args), --step-shell (a script) or --step-beam (a beam command).");
		if (kinds > 1)
			throw new CliException("Pass only one of --step-command, --step-shell and --step-beam.");
		if (string.IsNullOrWhiteSpace(args.addStep))
			throw new CliException("Name the step: `beam local setup --add-step \"<name>\" ...`.");

		var step = NewStep(args.addStep.Trim());
		if (args.stepCommand != null)
		{
			(step.command, step.arguments) = SplitProgram(args.stepCommand, args.stepArgs ?? string.Empty);
		}
		else if (args.stepShell != null)
		{
			step.shell = true;
			step.shellKind = args.stepShellKind ?? DefaultShellKind;
			step.arguments = args.stepShell;
		}
		else
		{
			step.beam = true;
			step.arguments = args.stepBeam;
		}

		step.label = string.IsNullOrWhiteSpace(args.stepLabel) ? null : args.stepLabel.Trim();
		step.workingDirectory = string.IsNullOrWhiteSpace(args.stepDir) ? null : args.stepDir;
		step.waitForExit = args.stepOnce;
		step.readyWhenHttpOk = string.IsNullOrWhiteSpace(args.stepReadyUrl) ? null : args.stepReadyUrl;
		step.readyWhenLogContains = string.IsNullOrWhiteSpace(args.stepReadyLog) ? null : args.stepReadyLog;
		step.port = args.stepPort;
		step.readyTimeoutSeconds = args.stepTimeout > 0 ? args.stepTimeout : 120;
		step.tags = Tags(args.stepTags);
		foreach (var pair in args.stepEnv ?? Array.Empty<string>())
		{
			var at = pair.IndexOf('=');
			if (at <= 0)
				throw new CliException($"--step-env takes KEY=VALUE; '{pair}' has no key.");
			step.environment[pair.Substring(0, at).Trim()] = pair.Substring(at + 1);
		}

		return (step, string.IsNullOrWhiteSpace(args.stepAfter) ? null : args.stepAfter.Trim());
	}

	private static string DefaultShellKind => OperatingSystem.IsWindows() ? "powershell" : "sh";

	/// <summary>
	/// A program typed with its arguments (<c>echo "Hello"</c>, <c>npx vite --port 5173</c>) as the program alone and
	/// the arguments, the typed ones first. Left as it is when it names an existing file (a path with spaces) or has
	/// no arguments. Without this the whole line was quoted as one program name, and Windows answered
	/// <c>'"echo \"Hello\""' is not recognized as an internal or external command</c>.
	/// </summary>
	public static (string program, string arguments) SplitProgram(string program, string arguments)
	{
		var text = program?.Trim() ?? string.Empty;
		if (text.Length == 0 || File.Exists(text.Trim('"'))) return (text, arguments);

		int end;
		string first;
		if (text[0] == '"')
		{
			end = text.IndexOf('"', 1);
			if (end < 0) return (text, arguments);
			first = text.Substring(1, end - 1);
			end++;
		}
		else
		{
			end = text.IndexOfAny(new[] { ' ', '\t' });
			if (end < 0) return (text, arguments);
			first = text.Substring(0, end);
		}

		var rest = text.Substring(end).Trim();
		if (rest.Length == 0) return (first, arguments);
		return (first, string.IsNullOrWhiteSpace(arguments) ? rest : $"{rest} {arguments.Trim()}");
	}

	private static LocalStackStep NewStep(string name) => new LocalStackStep { name = name, custom = true, tags = new List<string> { Tag } };

	private static List<string> Tags(string extra) =>
		new[] { Tag }
			.Concat((extra ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

	/// <summary>Why <paramref name="name"/> can't name a new step, or null when it can.</summary>
	public static string NameProblem(LocalStackConfig config, string name)
	{
		if (string.IsNullOrWhiteSpace(name)) return "A step needs a name.";
		if (config.steps.Any(s => string.Equals(s.name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
			return $"There is already a step named '{name.Trim()}'.";
		foreach (var prefix in new[] { LocalStackTemplate.MicroservicePrefix, LocalStackTemplate.ExtensionPrefix, LocalStackTemplate.GroupPrefix })
		{
			if (name.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				return $"Names starting with '{prefix.Trim()}' belong to the workspace's services, found when a stack runs. Pick another.";
		}

		return null;
	}

	/// <summary>Checks the step and puts it after <paramref name="after"/> in the catalogue, or at the end.</summary>
	public static void Add(LocalStackConfig config, LocalStackStep step, string after)
	{
		var problem = NameProblem(config, step.name);
		if (problem != null) throw new CliException(problem);
		if (step.shell && step.shellKind is not ("sh" or "powershell"))
			throw new CliException($"--step-shell-kind is sh or powershell, not '{step.shellKind}'.");
		if (!step.beam && !step.shell && string.IsNullOrWhiteSpace(step.command))
			throw new CliException("A custom step needs a program to run.");

		var index = config.steps.Count;
		if (after != null)
		{
			var at = config.steps.FindIndex(s => string.Equals(s.name, after, StringComparison.OrdinalIgnoreCase));
			if (at < 0)
				throw new CliException($"--step-after: no step named '{after}'. `beam local setup --list-steps` lists them.");
			index = at + 1;
		}

		config.steps.Insert(index, step);
	}

	/// <summary>Removes a custom step; the generated ones can only be turned off.</summary>
	public static void Remove(LocalStackConfig config, string name)
	{
		var step = config.steps.FirstOrDefault(s => string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase))
		           ?? throw new CliException($"No step named '{name}'. `beam local setup --list-steps` lists them.");
		if (step.custom != true)
			throw new CliException($"'{step.name}' was generated by setup, so it comes back whenever the catalogue is recreated. Turn it off with \"enabled\": false in the settings instead.");

		config.steps.Remove(step);
	}

	/// <summary>
	/// Puts the custom steps of <paramref name="previous"/> into a freshly generated catalogue, each after the step it
	/// followed before (or at the end when that step is gone), so recreating the catalogue never loses them.
	/// </summary>
	public static void Keep(LocalStackConfig previous, LocalStackConfig recreated)
	{
		var steps = previous?.steps ?? new List<LocalStackStep>();
		for (var i = 0; i < steps.Count; i++)
		{
			if (steps[i].custom != true) continue;
			if (recreated.steps.Any(s => string.Equals(s.name, steps[i].name, StringComparison.OrdinalIgnoreCase))) continue;

			var before = i > 0 ? recreated.steps.FindIndex(s => string.Equals(s.name, steps[i - 1].name, StringComparison.OrdinalIgnoreCase)) : -1;
			recreated.steps.Insert(before >= 0 ? before + 1 : recreated.steps.Count, steps[i]);
		}
	}

	public static List<LocalStackCatalogueEntry> Catalogue(LocalStackConfig config) =>
		config.steps.Select(s => new LocalStackCatalogueEntry
		{
			name = s.name,
			label = LocalStackCreateCommand.Label(s),
			tags = s.tags is { Count: > 0 } ? s.tags : LocalStackConfigIO.InferTags(s.name ?? string.Empty),
			kind = s.beam ? "beam" : s.shell ? "shell" : "process",
			once = s.waitForExit,
			custom = s.custom == true,
			enabled = s.enabled,
		}).ToList();

	private static void Print(List<LocalStackCatalogueEntry> catalogue)
	{
		LocalStackStyle.H1("Step catalogue", "every step a stack can run on this machine");
		var table = new Table().Border(TableBorder.Simple);
		table.AddColumn("[bold]step[/]");
		table.AddColumn("[bold]label[/]");
		table.AddColumn("[bold]tags[/]");
		table.AddColumn("[bold]runs[/]");
		foreach (var entry in catalogue)
		{
			var name = Markup.Escape(entry.name);
			table.AddRow(
				entry.custom ? $"[green]{name}[/]" : entry.enabled ? name : $"[grey]{name} (off)[/]",
				Markup.Escape(entry.label ?? string.Empty),
				Markup.Escape(string.Join(", ", entry.tags ?? new List<string>())),
				Markup.Escape($"{entry.kind}, {(entry.once ? "once" : "stays up")}"));
		}

		AnsiConsole.Write(table);
		AnsiConsole.MarkupLine("[grey]custom steps are green; add one with `beam local setup --add-step <name> ...`, remove it with --remove-step[/]");
	}

	private class Draft
	{
		public LocalStackStep step;
		public string kind;
		public string readiness;
		public string after;
	}

	private const string KindProgram = "A program, with arguments (node, npx, dotnet, docker...)";
	private const string KindShell = "A shell script";
	private const string KindBeam = "A beam command, run with this CLI against the stack's host";
	private const string Once = "Runs to completion (a seed, a script); the next step waits for it";
	private const string StaysUp = "Keeps running (a server, a watcher) until the stack stops";
	private const string ReadyNone = "Ready as soon as it starts";
	private const string ReadyUrl = "Ready once a URL answers";
	private const string ReadyLog = "Ready once its output contains some text";
	private const string AtTheEnd = "(at the end)";

	/// <summary>What <see cref="AskForStep"/> returns as the place of a step that goes before every other one.</summary>
	public const string AtTheStart = "(at the start)";

	/// <summary>
	/// The custom step as a wizard, like <c>beam local create</c>: each question replaces the last, Shift+Tab goes back,
	/// and a last screen shows the step to add it, change any answer, or cancel. Returns a null step when cancelled.
	/// <c>beam local create</c> asks the same for a stack's own commands: then <paramref name="placeAmong"/> are the
	/// stack's steps (and <see cref="AtTheStart"/> is offered), <paramref name="nameProblem"/> also checks the stack's
	/// own names, and it is already on the alternate screen.
	/// </summary>
	public static (LocalStackStep step, string after) AskForStep(LocalStackConfig config, string name, string heading = "beam local setup --add-step",
		string addTo = "the catalogue", IReadOnlyList<LocalStackStep> placeAmong = null, Func<string, string> nameProblem = null, bool ownScreen = true)
	{
		nameProblem ??= v => NameProblem(config, v);
		var places = placeAmong ?? config.steps;
		var draft = new Draft { step = NewStep(name?.Trim() ?? string.Empty), kind = KindProgram, readiness = ReadyNone };
		draft.step.workingDirectory = "${" + LocalStackConfigIO.PortalPathVar + "}";
		var wizard = new LocalStackWizard();

		void Add(string group, Func<bool, LocalStackWizardMove> ask, bool answered = false, Func<bool> applies = null) =>
			wizard.questions.Add(new LocalStackWizard.Question { group = group, ask = ask, answered = answered, applies = applies ?? (() => true) });

		LocalStackWizardMove Move(object answer) => answer == null ? LocalStackWizardMove.Back : LocalStackWizardMove.Next;

		Add("Name", back =>
		{
			var answer = LocalStackCreateCommand.Text("Name of the step (stacks and `beam local logs` use it):", back,
				string.IsNullOrEmpty(draft.step.name) ? null : draft.step.name, allowEmpty: false,
				validate: nameProblem);
			if (answer != null) draft.step.name = answer.Trim();
			return Move(answer);
		}, !string.IsNullOrWhiteSpace(name));

		Add("Runs", back =>
		{
			var answer = LocalStackCreateCommand.Select("What does it run?", new[] { KindProgram, KindShell, KindBeam }, back, draft.kind);
			if (answer != null) draft.kind = answer;
			return Move(answer);
		});

		Add("Runs", back =>
		{
			var answer = LocalStackCreateCommand.Text("The program [grey](node, npx, dotnet, a path...; arguments can follow it)[/]:", back,
				draft.step.command, allowEmpty: false, validate: v => string.IsNullOrWhiteSpace(v) ? "Name a program." : null);
			if (answer != null) (draft.step.command, draft.step.arguments) = SplitProgram(answer, draft.step.arguments);
			return Move(answer);
		}, applies: () => draft.kind == KindProgram);

		Add("Runs", back =>
		{
			var title = draft.kind == KindProgram ? "Its arguments [grey](may use ${variables}; empty for none)[/]:"
				: draft.kind == KindShell ? $"The script [grey](run with {DefaultShellKind}; may use ${{variables}})[/]:"
				: "The beam command, without `beam` [grey](content publish, project run --ids X...)[/]:";
			var answer = LocalStackCreateCommand.Text(title, back, string.IsNullOrEmpty(draft.step.arguments) ? null : draft.step.arguments,
				allowEmpty: draft.kind == KindProgram,
				validate: v => draft.kind != KindProgram && string.IsNullOrWhiteSpace(v) ? "It needs something to run." : null);
			if (answer != null) draft.step.arguments = answer;
			return Move(answer);
		});

		Add("Folder", back =>
		{
			var answer = LocalStackCreateCommand.Text("The folder it runs in [grey](${PortalPath}, ${BeamAPIPath}... or a path)[/]:", back,
				draft.step.workingDirectory, allowEmpty: true, validate: _ => null);
			if (answer != null) draft.step.workingDirectory = string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
			return Move(answer);
		});

		Add("Lifetime", back =>
		{
			var answer = LocalStackCreateCommand.Select("Does it finish, or keep running?", new[] { Once, StaysUp }, back,
				draft.step.waitForExit ? Once : StaysUp);
			if (answer != null) draft.step.waitForExit = answer == Once;
			return Move(answer);
		});

		Add("Ready when", back =>
		{
			var answer = LocalStackCreateCommand.Select("When is it ready for the next steps?", new[] { ReadyNone, ReadyUrl, ReadyLog }, back, draft.readiness);
			if (answer != null) draft.readiness = answer;
			return Move(answer);
		}, applies: () => !draft.step.waitForExit);

		Add("Ready when", back =>
		{
			var url = draft.readiness == ReadyUrl;
			var answer = LocalStackCreateCommand.Text(url ? "The URL [grey](http://localhost:7070...)[/]:" : "The text its output prints when ready:", back,
				url ? draft.step.readyWhenHttpOk : draft.step.readyWhenLogContains, allowEmpty: false,
				validate: v => string.IsNullOrWhiteSpace(v) ? "Give one, or go back and pick \"ready as soon as it starts\"." : null);
			if (answer != null)
			{
				draft.step.readyWhenHttpOk = url ? answer.Trim() : null;
				draft.step.readyWhenLogContains = url ? null : answer;
			}

			return Move(answer);
		}, applies: () => !draft.step.waitForExit && draft.readiness != ReadyNone);

		Add("Port", back =>
		{
			var answer = LocalStackCreateCommand.Text("The port it binds, checked before it starts [grey](empty for none)[/]:", back,
				draft.step.port > 0 ? draft.step.port.ToString() : null, allowEmpty: true,
				validate: v => string.IsNullOrWhiteSpace(v) || int.TryParse(v, out var p) && p is > 0 and < 65536 ? null : "A port is a number from 1 to 65535.");
			if (answer != null) draft.step.port = int.TryParse(answer, out var port) ? port : 0;
			return Move(answer);
		}, applies: () => !draft.step.waitForExit);

		Add("Tags", back =>
		{
			var choices = LocalStackStacks.KnownTags.Where(t => t != Tag).ToList();
			var answer = LocalStackCreateCommand.MultiSelect(
				$"Kinds it belongs to besides [green]{Tag}[/]: a stack or [green]ps --restart <kind>[/] can name the kind instead of each step [grey](none keeps just {Tag})[/]", choices,
				draft.step.tags, back,
				t => $"{Markup.Escape(t)} [grey]{Markup.Escape(LocalStackStacks.TagDescriptions.TryGetValue(t, out var d) ? d : string.Empty)}[/]");
			if (answer != null) draft.step.tags = new[] { Tag }.Concat(answer).ToList();
			return Move(answer);
		});

		Add("Place", back =>
		{
			var choices = new[] { AtTheEnd }.Concat(placeAmong != null ? new[] { AtTheStart } : Array.Empty<string>()).Concat(places.Select(s => s.name));
			var answer = LocalStackCreateCommand.Select(placeAmong != null ? "When does it run among the stack's steps?" : "Where does it go in the catalogue? A stack runs it in this order.",
				choices, back, draft.after ?? AtTheEnd,
				n => n is AtTheEnd or AtTheStart ? n : $"after {Markup.Escape(LocalStackCreateCommand.Label(places.First(s => s.name == n)))} [grey]({Markup.Escape(n)})[/]");
			if (answer != null) draft.after = answer == AtTheEnd ? null : answer;
			return Move(answer);
		});

		void Frame()
		{
			AnsiConsole.Clear();
			LocalStackStyle.H1(heading, "Enter to answer, Shift+Tab to go back");
			foreach (var group in wizard.AnsweredGroups())
				AnsiConsole.MarkupLine($"  {LocalStackStyle.H3(group)}  {Markup.Escape(Summary(group, draft))}");
			AnsiConsole.WriteLine();
		}

		wizard.beforeEach = Frame;
		(LocalStackStep, string) result = (null, null);
		void Ask()
		{
			wizard.Run();
			while (true)
			{
				const string add = "Add it";
				const string cancel = "Cancel, add nothing";
				const string change = "Change: ";

				Finish(draft);
				AnsiConsole.Clear();
				LocalStackStyle.H1($"{heading}: review {draft.step.name}");
				foreach (var group in wizard.Groups())
					AnsiConsole.MarkupLine($"  {LocalStackStyle.H3(group)}  {Markup.Escape(Summary(group, draft))}");
				AnsiConsole.WriteLine();

				var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
					.Title($"Add [green]{Markup.Escape(draft.step.name)}[/] to {Markup.Escape(addTo)}?")
					.AddChoices(new[] { add }.Concat(wizard.Groups().Select(g => change + g)).Append(cancel))
					.UseConverter(Markup.Escape)
					.AddBeamHightlight());

				if (choice == add)
				{
					result = (draft.step, draft.after);
					return;
				}

				if (choice == cancel) return;

				var revisit = choice.Substring(change.Length);
				if (wizard.Run(wizard.questions.FindIndex(q => q.group == revisit), revisit))
					wizard.AskUnanswered();
			}
		}

		if (ownScreen) LocalStackCreateCommand.InWizardScreen(Ask);
		else Ask();
		return result;
	}

	/// <summary>What a custom step runs and whether it finishes, on one line: "echo "Hello", runs to completion".</summary>
	public static string Describe(LocalStackStep step)
	{
		var runs = step.beam ? $"beam {step.arguments}" : step.shell ? $"{step.shellKind ?? DefaultShellKind}: {step.arguments}" : $"{step.command} {step.arguments}".Trim();
		return $"{runs}, {(step.waitForExit ? "runs to completion" : "keeps running")}";
	}

	/// <summary>Turns the wizard's answers into the step's fields for the chosen kind.</summary>
	private static void Finish(Draft draft)
	{
		var step = draft.step;
		step.shell = draft.kind == KindShell;
		step.beam = draft.kind == KindBeam;
		step.shellKind = step.shell ? DefaultShellKind : null;
		if (draft.kind != KindProgram) step.command = null;
		if (step.waitForExit || draft.readiness == ReadyNone)
		{
			step.readyWhenHttpOk = null;
			step.readyWhenLogContains = null;
		}

		if (step.waitForExit) step.port = 0;
	}

	private static string Summary(string group, Draft draft)
	{
		var step = draft.step;
		return group switch
		{
			"Name" => step.name,
			"Runs" => draft.kind == KindProgram ? $"{step.command} {step.arguments}".Trim()
				: draft.kind == KindShell ? $"{DefaultShellKind}: {step.arguments}"
				: $"beam {step.arguments}",
			"Folder" => step.workingDirectory ?? "(the workspace)",
			"Lifetime" => step.waitForExit ? "runs to completion" : "keeps running",
			"Ready when" => draft.readiness == ReadyUrl ? $"{step.readyWhenHttpOk} answers"
				: draft.readiness == ReadyLog ? $"its output contains \"{step.readyWhenLogContains}\""
				: "it starts",
			"Port" => step.port > 0 ? step.port.ToString() : "none",
			"Tags" => string.Join(", ", step.tags ?? new List<string> { Tag }),
			"Place" => draft.after == null ? "at the end of the catalogue" : $"after {draft.after}",
			_ => string.Empty
		};
	}
}
