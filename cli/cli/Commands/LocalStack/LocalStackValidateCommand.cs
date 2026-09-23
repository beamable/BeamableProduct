using Beamable.Server;
using cli.Services.LocalStack;
using CliWrap;
using Spectre.Console;
using System.CommandLine;
using System.Text;

namespace cli.Commands.LocalStack;

public class LocalStackValidateCommandArgs : CommandArgs
{
	public string toolchainDir;
	public string configPath;

	/// <summary>Also run the AWS preflight, which makes several network calls; off by default to keep `validate` fast.</summary>
	public bool withAws;
}

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
}

public class LocalStackValidateCommandResult
{
	public bool allOk;
	public List<LocalStackDependencyCheck> checks = new List<LocalStackDependencyCheck>();

	/// <summary>The toolchain directory that was inspected.</summary>
	public string toolchainDir;
}

/// <summary>
/// Checks that everything the local stack needs is in place: the pinned toolchain (JDK 8, Maven, the .NET SDK,
/// Node), the Docker daemon, the generated BeamableBackend config files, and — with <c>--with-aws</c> — the AWS
/// prerequisites.
///
/// Check-only. Each failing row names what to run, which is almost always <c>beam local setup</c>. The important
/// column is <b>source</b>: a dependency satisfied by the <c>toolchain</c> is pinned and reproducible, while one
/// satisfied by the <c>system</c> is whatever that machine happens to have — which is how a Maven running under a
/// JDK 21 from an IDE bundle, or a Node major the portal was never built against, gets used without anyone
/// noticing.
/// </summary>
public class LocalStackValidateCommand
	: AtomicCommand<LocalStackValidateCommandArgs, LocalStackValidateCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public LocalStackValidateCommand() : base("validate", "Check that the local stack's dependencies are installed")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--toolchain-dir",
				$"Toolchain directory to inspect (default: ~/{ToolchainService.DefaultDirName}, or ${ToolchainService.EnvVarToolchainDir})"),
			(args, v) => args.toolchainDir = v);
		AddOption(new Option<string>("--config", "Path to the local-stack manifest to validate against (defaults to .beamable/local-stack.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<bool>("--with-aws", "Also run the AWS preflight (credentials, assume-role, the JWT signing secret, buckets, scheduler queue)"),
			(args, v) => args.withAws = v);
	}

	public override async Task<LocalStackValidateCommandResult> GetResult(LocalStackValidateCommandArgs args)
	{
		var result = new LocalStackValidateCommandResult
		{
			toolchainDir = ToolchainService.ResolveDir(args.toolchainDir)
		};

		var manifestPath = LocalStackCommand.ResolveManifestPath(args.ConfigService, args.configPath);
		var config = File.Exists(manifestPath) ? LocalStackConfigIO.Load(manifestPath) : null;
		var toolchain = ToolchainService.LoadManifest(Path.Combine(result.toolchainDir, ToolchainService.ManifestFileName));

		// docker (via the CLI's resolver) — the daemon, not just the binary.
		if (DockerPathOption.TryGetDockerPath(out var dockerPath, out var dockerErr))
			result.checks.Add(await CheckProgram("docker", dockerPath, "--version",
				hint: "install Docker Desktop from https://docs.docker.com/get-docker/"));
		else
			result.checks.Add(new LocalStackDependencyCheck { name = "docker", ok = false, detail = dockerErr });

		// The four pinned tools. Each is reported with where it came from and how its version compares to the pin.
		foreach (var toolId in ToolchainPins.ToolIds)
			result.checks.Add(CheckTool(toolId, toolchain, config));

		// The generated BeamableBackend config files, which are gitignored and therefore missing on a fresh clone.
		result.checks.Add(CheckScalaConfig(config?.repos?.scalaDir));

		// The portal's .env.local — same class of gap, and its absence is far more confusing.
		result.checks.Add(CheckPortalEnv(config?.repos?.portalDir, config?.host));

		if (args.withAws)
			result.checks.AddRange(CheckAws(config?.repos?.scalaDir, config?.repos?.apiDir));

		result.allOk = result.checks.All(c => c.ok || c.warning);
		Render(result, args.withAws);
		return result;
	}

	/// <summary>
	/// Resolves one pinned tool the same way <c>up</c> will: the toolchain first, then the manifest, then the
	/// system. Reporting the source is the point — "ok" against a system tool of the wrong version is exactly the
	/// state this command exists to make visible.
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
					: $"{version} does not match the pin — {entry.home} (re-run `beam local setup --only {toolId} --force`)";
				return check;
			}
		}

		// 2. Recorded on the manifest but not in the toolchain (a hand-edited or shared manifest).
		var manifestHome = toolId switch
		{
			ToolchainPins.Jdk => config?.toolchain?.java ?? config?.javaHome,
			ToolchainPins.Maven => config?.toolchain?.maven,
			ToolchainPins.Dotnet => config?.toolchain?.dotnet,
			ToolchainPins.Node => config?.toolchain?.node,
			_ => null
		};

		if (!string.IsNullOrWhiteSpace(manifestHome)
		    && !manifestHome.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal)
		    && File.Exists(ToolchainService.ExecutablePath(toolId, manifestHome) ?? string.Empty))
		{
			check.ok = true;
			check.source = "manifest";
			check.version = ToolchainService.ReadVersion(toolId, manifestHome);
			check.detail = $"{check.version ?? "?"} — {manifestHome}";
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
		check.detail = $"not found — run `beam local setup --only {toolId}` to install {PinOf(toolId)}";
		return check;
	}

	/// <summary>The pinned version, for the "what you should have" half of a mismatch message.</summary>
	private static string PinOf(string toolId) => toolId switch
	{
		ToolchainPins.Jdk => $"JDK {ToolchainPins.JavaFeatureVersion}",
		ToolchainPins.Maven => $"Maven {ToolchainPins.MavenVersion}",
		ToolchainPins.Dotnet => $".NET SDK {ToolchainPins.DotnetVersion}",
		ToolchainPins.Node => $"Node {ToolchainPins.NodeMajor}",
		_ => toolId
	};

	/// <summary>
	/// The version of a tool as installed on the machine, plus the home it was found at. For the JDK this reuses
	/// the CLI's existing Java 8 discovery so the answer matches what <c>up</c> would resolve.
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
	/// and <c>beam local up</c> does not create them — a stack missing them starts the Scala services and then
	/// fails on config nobody told the developer to generate.
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
			check.detail = "no BeamableBackend path on the manifest — cannot check the generated conf files";
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
		check.detail = $"missing {string.Join(", ", missing.Select(Path.GetFileName))} — " +
		               "run `beam local setup --only scala-config`";
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
			check.detail = "no portal path on the manifest — cannot check .env.local";
			return check;
		}

		var apiBase = PortalEnvService.ReadApiBase(portalDir);
		if (apiBase == null)
		{
			check.ok = false;
			check.detail =
				$"{PortalEnvService.ApiBaseKey} is not set in {PortalEnvService.EnvFileName} — the portal will use " +
				$"{PortalEnvService.ProductionApiBase} and your local account will not exist there. " +
				"Run `beam local setup --only portal-config`.";
			return check;
		}

		var matchesStack = string.IsNullOrWhiteSpace(host)
		                   || string.Equals(apiBase.TrimEnd('/'), host.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

		check.ok = true;
		check.warning = !matchesStack;
		check.detail = matchesStack
			? apiBase
			: $"{apiBase} — does NOT match the stack host {host}; the portal will log in against that instead";
		return check;
	}

	private static IEnumerable<LocalStackDependencyCheck> CheckAws(string scalaDir, string apiDir)
	{
		var preflight = new AwsPreflightService().Run(scalaDir, apiDir);
		foreach (var check in preflight.checks)
		{
			yield return new LocalStackDependencyCheck
			{
				name = $"aws: {check.name}",
				ok = check.ok,
				warning = check.warning,
				detail = check.ok
					? check.detail
					: string.Join(" — ", new[] { check.detail, check.awsError, check.remediation }
						.Where(s => !string.IsNullOrWhiteSpace(s)))
			};
		}
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

	private static void Render(LocalStackValidateCommandResult result, bool withAws)
	{
		var table = new Table();
		table.Border(TableBorder.Simple);
		table.AddColumn("[bold]dependency[/]");
		table.AddColumn("[bold]status[/]");
		table.AddColumn("[bold]source[/]");
		table.AddColumn("[bold]detail[/]");

		foreach (var c in result.checks)
		{
			var status = c.ok
				? (c.warning ? "[yellow]ok[/]" : "[green]ok[/]")
				: "[red]missing[/]";

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
		Log.Information($"Toolchain directory: {result.toolchainDir}");

		if (!withAws)
			Log.Information("AWS prerequisites were not checked — add --with-aws (the local stack cannot mint tokens without them).");

		if (result.allOk)
			Log.Information("All local-stack dependencies are available.");
		else
			Log.Warning("Some local-stack dependencies are missing — run `beam local setup`.");
	}
}
