using Beamable.Server;
using cli.Services.LocalStack;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackSetupCommandArgs : CommandArgs
{
	public string toolchainDir;
	public string only;
	public string skip;
	public bool force;
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
	public bool allOk;
	public List<LocalStackSetupStepResult> steps = new List<LocalStackSetupStepResult>();
}

/// <summary>
/// Provisions everything <c>beam local up</c> needs on a machine that has never run it, then points the manifest
/// at what it installed. <c>beam local validate</c> is the check; this is the fix.
///
/// Four things happen here, and each exists because the stack silently assumed it:
/// <list type="number">
/// <item>A <b>private, pinned toolchain</b> (JDK 8, Maven, the .NET SDK, Node) downloaded into a directory you
/// choose, so the build no longer follows whatever is on PATH. See <see cref="ToolchainService"/>.</item>
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
	public LocalStackSetupCommand() : base("setup",
		"Download and pin the local stack's dependencies (JDK 8, Maven, .NET SDK, Node) into a private toolchain, generate the backend config files, and check the AWS prerequisites")
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
		AddOption(new Option<bool>("--force", "Re-download and re-install even when a dependency is already present, and overwrite generated config files"),
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
		AddOption(new Option<string>("--config", "Path to the local-stack manifest to update (defaults to .beamable/local-stack.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<string>("--scala-dir", "Absolute path to the BeamableBackend (Scala) repo; only needed when the manifest does not record it yet"),
			(args, v) => args.scalaDir = v);
		AddOption(new Option<string>("--api-dir", "Absolute path to the BeamableAPI (C# gateway) repo; only needed when the manifest does not record it yet"),
			(args, v) => args.apiDir = v);
		AddOption(new Option<string>("--portal-dir", "Absolute path to the portal repo; only needed when the manifest does not record it yet"),
			(args, v) => args.portalDir = v);
	}

	public override async Task<LocalStackSetupCommandResult> GetResult(LocalStackSetupCommandArgs args)
	{
		var only = LocalStackUpCommand.NameSet(args.only);
		var skip = LocalStackUpCommand.NameSet(args.skip);
		bool Included(string id) => (only == null || only.Contains(id)) && (skip == null || !skip.Contains(id));

		var manifestPath = LocalStackCommand.ResolveManifestPath(args.ConfigService, args.configPath);
		var config = File.Exists(manifestPath) ? LocalStackConfigIO.Load(manifestPath) : null;

		var result = new LocalStackSetupCommandResult
		{
			toolchainDir = ToolchainService.ResolveDir(args.toolchainDir),
			manifestPath = File.Exists(manifestPath) ? manifestPath : null
		};

		Log.Information($"Toolchain directory: {result.toolchainDir} ({ToolchainPins.PlatformLabel})");

		// Docker first and always: it gates most of the stack and cannot be provisioned here, so knowing it is
		// missing before spending several minutes downloading a JDK is worth the two seconds.
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

		service.SaveManifest();

		// Point the manifest at what was installed. Done before the remaining steps so a later failure (e.g. no
		// GitHub token) still leaves the toolchain wired up.
		result.steps.Add(UpdateManifest(manifestPath, config, service, args.dryRun));

		// The repo paths come from the manifest that `beam local init` already recorded; the options are for the
		// case where setup runs before init.
		var scalaDir = FirstUsable(args.scalaDir, config?.repos?.scalaDir);
		var apiDir = FirstUsable(args.apiDir, config?.repos?.apiDir);
		var portalDir = FirstUsable(args.portalDir, config?.repos?.portalDir);

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
	/// Writes the toolchain block (and the matching <c>javaHome</c>) into the manifest, leaving every step
	/// untouched. Nothing else in the manifest is regenerated: a developer's edited step list, service selection
	/// and JVM flags have to survive a setup run.
	/// </summary>
	private static LocalStackSetupStepResult UpdateManifest(string manifestPath, LocalStackConfig config,
		ToolchainService service, bool dryRun)
	{
		if (config == null)
		{
			return new LocalStackSetupStepResult
			{
				name = "manifest",
				status = "skipped",
				ok = true,
				detail = $"no manifest at {manifestPath} yet — run `beam local init`, then re-run setup to wire the toolchain in"
			};
		}

		var toolchain = service.ToManifestBlock();
		if (dryRun)
		{
			return new LocalStackSetupStepResult
			{
				name = "manifest",
				status = "would install",
				ok = true,
				detail = $"would point {manifestPath} at {service.Dir}"
			};
		}

		config.toolchain = toolchain;
		// javaHome is what the ${java} token reads and what `up`'s resolution chain checks before auto-detection,
		// so mirror the toolchain's JDK into it — otherwise a system JDK 8 could still win.
		if (!string.IsNullOrWhiteSpace(toolchain.java))
			config.javaHome = toolchain.java;

		LocalStackConfigIO.Save(manifestPath, config);
		return new LocalStackSetupStepResult
		{
			name = "manifest",
			status = "ok",
			ok = true,
			detail = $"{manifestPath} now uses the toolchain"
		};
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
				detail = "no BeamableBackend path known — pass --scala-dir, or run `beam local init` first"
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
	/// Points the portal's gitignored <c>.env.local</c> at the local backend.
	///
	/// Without it the portal's <c>API_BASE</c> falls back to <c>https://api.beamable.com</c>, so a portal served
	/// from localhost sends its login to PRODUCTION — where the local seed account does not exist. The stack looks
	/// completely healthy and login just fails, which is indistinguishable from a broken backend.
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
				detail = "no portal path known — pass --portal-dir, or run `beam local init` first"
			};
		}

		var host = config?.host;

		if (args.dryRun)
		{
			var current = PortalEnvService.ReadApiBase(portalDir);
			return new LocalStackSetupStepResult
			{
				name = ToolchainPins.PortalConfig,
				status = "would install",
				ok = true,
				detail = current == null
					? $"would set {PortalEnvService.ApiBaseKey}={host} in {PortalEnvService.EnvFileName} " +
					  "(currently unset, so the portal would log in against production)"
					: $"{PortalEnvService.ApiBaseKey} is already {current}"
			};
		}

		var outcome = new PortalEnvService().Ensure(portalDir, host, args.force);
		if (!outcome.ok)
		{
			return new LocalStackSetupStepResult
			{
				name = ToolchainPins.PortalConfig, status = "failed", ok = false, detail = outcome.error
			};
		}

		// "kept" with a value that is not the manifest host means the developer deliberately points elsewhere
		// (dev/staging). Surface it rather than silently retargeting their portal.
		var pointsElsewhere = outcome.action == "kept"
		                      && !string.Equals(outcome.apiBase?.TrimEnd('/'), host?.TrimEnd('/'),
			                      StringComparison.OrdinalIgnoreCase);

		return new LocalStackSetupStepResult
		{
			name = ToolchainPins.PortalConfig,
			status = pointsElsewhere ? "warning" : "ok",
			ok = true,
			detail = pointsElsewhere
				? $"{PortalEnvService.ApiBaseKey}={outcome.apiBase} does NOT match the stack host {host} — the " +
				  "portal will log in against that host instead. Use --force to repoint it."
				: $"{outcome.action}: {PortalEnvService.ApiBaseKey}={outcome.apiBase}"
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
			Log.Information("Local stack setup complete. Next: `beam local validate`, then `beam local up --build`.");
		else
			Log.Warning("Local stack setup finished with failures — see the table above. Each row names the action that fixes it.");
	}
}
