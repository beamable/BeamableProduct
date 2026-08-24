using Newtonsoft.Json;

namespace cli.Services.LocalStack;

/// <summary>
/// A generic, machine-agnostic description of a full local Beamable stack: a list of
/// processes (<see cref="LocalStackStep"/>) to bring up in order, each with an optional
/// readiness gate. This is the C# equivalent of the <c>scripts/run-local-stack.sh</c>
/// orchestrator, but driven entirely by a JSON manifest so it is not tied to any one machine.
///
/// The manifest lives (by default) at <c>&lt;workspace&gt;/.beamable/local-stack.json</c> and is
/// created by <c>beam local init</c>. Edit the paths/commands to match your machine.
/// </summary>
public class LocalStackConfig
{
	/// <summary>
	/// The backend API host every beam step points at (Caddy proxy in the reference setup).
	/// Substituted into step arguments/urls via the <c>${host}</c> token.
	/// </summary>
	public string host = "http://localhost:8080";

	/// <summary>
	/// The portal frontend URL. Substituted via the <c>${portalUrl}</c> token; passed to portal
	/// extensions so their "open in browser" landing URL points at the local portal.
	/// </summary>
	public string portalUrl = "http://localhost:4950";

	/// <summary>
	/// The Java 8 <c>JAVA_HOME</c> the Scala backend runs under. Substituted via the <c>${java}</c> token
	/// (e.g. <c>${java}/bin/java</c>). Resolved by <c>beam local up</c> from <c>--java-path</c> /
	/// <c>BEAM_JAVA_HOME</c> / auto-detection when left null, so a shared manifest stays machine-agnostic.
	/// </summary>
	public string javaHome;

	/// <summary>
	/// The pinned toolchain <c>beam local setup</c> provisioned, if any: private installs of JDK 8, Maven, the
	/// .NET SDK and Node that the steps below resolve their commands from instead of <c>PATH</c>.
	///
	/// Null on a manifest that predates <c>setup</c> (and omitted from the JSON when null), in which case every
	/// token falls back to the bare command name and the stack behaves exactly as it did before — so an existing
	/// manifest keeps working untouched.
	/// </summary>
	public LocalStackToolchain toolchain;

	/// <summary>
	/// How often (seconds) an attached <c>beam local up</c> re-checks that the containers its
	/// <c>docker compose up</c> steps started are still running. 0 disables the check.
	///
	/// Docker steps are run-to-completion, so their containers are invisible to the attached wait: one can die
	/// mid-run and the orchestrator would keep streaming reconnect errors under a "Stack is up" banner. See
	/// <see cref="LocalStackDockerWatchdog"/>.
	/// </summary>
	public int dockerWatchdogSeconds = 15;

	/// <summary>
	/// The standing web-registry choice <c>beam local init</c> recorded: whether <c>beam local up</c> runs the
	/// local web package registry steps (Verdaccio + local-unpkg) without being asked to. The steps themselves
	/// are always written to the manifest, so this is the switch — flipping it is what <c>init</c> does, and
	/// <c>--no-web-registry</c> / <c>--with-web-registry</c> override it for a single run WITHOUT writing back
	/// here. Re-run <c>beam local init</c> to change the standing choice.
	///
	/// Null on a manifest written before this field existed, which reads as TRUE: those manifests only contain
	/// the web steps when their author asked for them, and <c>up</c> ran them by default, so an existing
	/// workspace keeps behaving exactly as it did until it is re-initialised.
	/// </summary>
	public bool? webRegistry;

	/// <summary>
	/// The repository checkouts this manifest was generated against. Documentation-only metadata: the steps
	/// carry their own absolute <see cref="LocalStackStep.workingDirectory"/> values and nothing in the
	/// orchestrator reads this. It exists so the generated <c>beam-local-stack</c> agent skill (see
	/// <see cref="LocalStackSkillTemplate"/>) can name the repos without reverse-engineering step names —
	/// which also keeps it correct for <c>beam local init --update-services</c>, where the repo paths are
	/// never prompted for.
	///
	/// Null on manifests written before this field existed (and omitted from the JSON when null), so readers
	/// must treat it as optional.
	/// </summary>
	public LocalStackRepos repos;

	/// <summary>
	/// The ordered set of processes to launch. Order matters — earlier steps that declare a
	/// readiness gate are fully up before later steps start.
	/// </summary>
	public List<LocalStackStep> steps = new List<LocalStackStep>();
}

/// <summary>
/// The repository checkouts a <see cref="LocalStackConfig"/> was generated against. Every value may hold an
/// unedited <c>&lt;EDIT: ...&gt;</c> placeholder (see <see cref="LocalStackConfigIO.EditPlaceholder"/>) when
/// <c>beam local init</c> could not resolve it, or be null when that part of the stack is not included.
/// </summary>
public class LocalStackRepos
{
	/// <summary>The <c>BeamableAPI</c> checkout: the docker deps + Caddy compose file and the three .NET hosts.</summary>
	public string apiDir;

	/// <summary>The <c>BeamableBackend</c> checkout: the Scala <c>tools/*</c> services and the redis compose file.</summary>
	public string scalaDir;

	/// <summary>The portal frontend checkout: the Vite dev server and the portal extensions it serves.</summary>
	public string portalDir;

	/// <summary>The <c>portal-localdev</c> directory holding the local web registry compose file. Null when the
	/// web-registry steps are not part of this manifest.</summary>
	public string webRegistryDir;

	/// <summary>The <c>BeamableProduct</c> checkout holding the web packages, derived from
	/// <see cref="webRegistryDir"/>. Null when the web-registry steps are not part of this manifest.</summary>
	public string productDir;
}

/// <summary>
/// The private, pinned toolchain a manifest runs against, written by <c>beam local setup</c>. Each value is a
/// tool <em>home</em> (the directory whose <c>bin/</c> holds the executables), matching
/// <see cref="LocalStackConfig.javaHome"/>'s shape — <see cref="LocalStackConfigIO"/> derives the individual
/// executable paths and the <c>PATH</c> prefix from these.
///
/// Any value may be null (that tool was skipped, or an existing system install was adopted), in which case the
/// corresponding token falls back to the bare command name and resolves via <c>PATH</c> as before.
/// </summary>
public class LocalStackToolchain
{
	/// <summary>The toolchain directory these homes live under; recorded so <c>local validate</c> can name it.</summary>
	public string dir;

	/// <summary>JDK 8 home. Mirrored into <see cref="LocalStackConfig.javaHome"/>, which is what <c>${java}</c> reads.</summary>
	public string java;

	/// <summary>Maven home — <c>${maven}</c> resolves to its <c>bin/mvn</c>.</summary>
	public string maven;

	/// <summary>.NET SDK install dir — <c>${dotnet}</c> resolves to the <c>dotnet</c> executable at its root.</summary>
	public string dotnet;

	/// <summary>Node home — <c>${node}</c> and <c>${npm}</c> resolve to its <c>node</c> / <c>npm</c>.</summary>
	public string node;

	/// <summary>
	/// pnpm home. No token needs it: <c>beam web publish</c> shells out to a bare <c>pnpm</c>, so what matters is
	/// that this directory reaches the child's <c>PATH</c> (see <see cref="LocalStackConfigIO.ToolchainPathPrefix"/>).
	/// </summary>
	public string pnpm;
}

/// <summary>
/// A single process the orchestrator launches. A step is either a raw process
/// (<see cref="command"/> + <see cref="arguments"/>) or a beam invocation
/// (<see cref="beam"/> = true, where <see cref="arguments"/> is a beam sub-command and the CLI
/// executable is resolved automatically).
/// </summary>
public class LocalStackStep
{
	/// <summary>Human-readable label shown in progress output and log prefixes.</summary>
	public string name;

	/// <summary>When false, the step is skipped entirely.</summary>
	public bool enabled = true;

	/// <summary>
	/// When true, this is a build/compile step: it only runs when <c>beam local up --build</c> is passed
	/// (skipped otherwise). Build steps are run-to-completion (<see cref="waitForExit"/>) and are not tracked
	/// in the run-state — they compile a component before its run step, they are not a running service.
	/// </summary>
	public bool build = false;

	/// <summary>
	/// For a build step: the file (or directory) the build is expected to produce. When set and missing, the
	/// step runs even <b>without</b> <c>beam local up --build</c> — so a fresh clone (or a project nobody has
	/// built yet) self-heals instead of launching a binary that isn't there, and <c>up</c> fails loudly when a
	/// build "succeeds" without producing it. Mirrors the reference <c>scripts/run-local-stack.sh</c>, which
	/// builds each gateway/worker binary when it is missing. Absolute, or relative to
	/// <see cref="workingDirectory"/>. Tokens are substituted.
	/// </summary>
	public string requiredOutput;

	/// <summary>
	/// For the Scala reactor build step: the module directories it produces output for, relative to
	/// <see cref="workingDirectory"/> (e.g. <c>core</c>, <c>tools/auth</c>).
	///
	/// <c>beam local up</c> runs this build WITHOUT <c>--build</c> when any listed module has never been compiled.
	/// A plain first `up` on a fresh clone otherwise launched JVMs against modules with no classes, which does not
	/// fail cleanly — the service starts, half the stack proxies through it, and the result is a "malfunctioning"
	/// backend rather than an obvious build error. A single <see cref="requiredOutput"/> cannot express this
	/// because one step builds ~18 modules.
	///
	/// Null on manifests written before this existed, which simply keeps the old behaviour.
	/// </summary>
	public List<string> scalaModules;

	/// <summary>
	/// Optional parallel-group label. Consecutive steps that share the same non-empty group are
	/// launched together and their readiness gates awaited concurrently (e.g. all Scala services),
	/// instead of one-at-a-time. Ordering between different groups (and ungrouped steps) is preserved.
	/// </summary>
	public string group;

	/// <summary>
	/// When true, this step runs the current beam CLI: <see cref="arguments"/> is the beam
	/// sub-command line (e.g. <c>project run --ids CampaignService --host ${host}</c>) and the
	/// executable + host prefix are resolved automatically. <see cref="command"/> is ignored.
	/// </summary>
	public bool beam = false;

	/// <summary>
	/// When true (and <see cref="beam"/> is false), <see cref="arguments"/> is run through a shell,
	/// so shell features like command substitution work. Which shell is chosen by <see cref="shellKind"/>
	/// (see below). Useful for complex launches (e.g. a Scala <c>java -cp $(...)</c>).
	/// </summary>
	public bool shell = false;

	/// <summary>
	/// For a shell step, which shell the <see cref="arguments"/> script targets: <c>"sh"</c> (the
	/// default when null/empty — POSIX sh, used on macOS/Linux) or <c>"powershell"</c> (Windows).
	/// Set by <c>beam local init</c> to the generating OS so <c>up</c> runs the script with the right
	/// interpreter. A POSIX-sh script cannot be run by <c>cmd.exe</c>, so this must match the script.
	/// </summary>
	public string shellKind;

	/// <summary>Executable to run. Ignored when <see cref="beam"/> or <see cref="shell"/> is true.</summary>
	public string command;

	/// <summary>Command-line arguments (or the shell/beam command line). Tokens are substituted.</summary>
	public string arguments = "";

	/// <summary>Working directory to launch in. Tokens are substituted. May be absolute or relative to the manifest.</summary>
	public string workingDirectory;

	/// <summary>Extra environment variables for the child process. Values have tokens substituted.</summary>
	public Dictionary<string, string> environment = new Dictionary<string, string>();

	/// <summary>
	/// When true, the orchestrator waits for the process to exit and checks its exit code before
	/// moving on (use for run-to-completion steps like <c>docker compose up -d</c>). When false,
	/// the process is long-running and left running until the whole stack is torn down.
	/// </summary>
	public bool waitForExit = false;

	/// <summary>
	/// The TCP port this step's process binds, when known (0 = unknown, no check). Before launching,
	/// <c>beam local up</c> fails fast when this port is already held by something that is <em>not</em> this
	/// service — otherwise the process loses the bind and the failure surfaces much later as a readiness
	/// timeout, a 502, or a service that "struggles to start". Declared explicitly rather than parsed out of a
	/// readiness URL because the two differ (the Scala gateway binds 9002 but is health-checked through Caddy).
	/// </summary>
	public int port;

	/// <summary>If set, the step is "ready" once this URL responds to a GET with any HTTP status.</summary>
	public string readyWhenHttpOk;

	/// <summary>
	/// If set, the step is "ready" once this URL returns HTTP 200 — a stronger gate than
	/// <see cref="readyWhenHttpOk"/> (which accepts any response). Used for the C# gateway's
	/// <c>/health</c> endpoint and the Scala gateway's <c>${host}/metadata</c> route.
	/// </summary>
	public string readyWhenHttp200;

	/// <summary>If set, the step is "ready" once a line in its log file contains this substring.</summary>
	public string readyWhenLogContains;

	/// <summary>
	/// The fully-qualified Scala <c>main</c> class for a backing service, discovered at <c>init</c> time so
	/// <c>up</c> need not grep <c>pom.xml</c> at runtime. Substituted into the launch shell via <c>${mainClass}</c>.
	/// </summary>
	public string mainClass;

	/// <summary>
	/// Optional arguments used by <c>beam local stop</c> to reverse a run-to-completion step (e.g.
	/// <c>compose stop</c> for a <c>docker compose up -d</c> step); run as <c>command stopArguments</c> in
	/// <see cref="workingDirectory"/>. Keep this NON-destructive: it runs on every plain
	/// <c>beam local stop</c>, so anything that deletes container volumes here wipes the local database
	/// (accounts/customers/realms) on each stop/up cycle. Put the destructive form in
	/// <see cref="purgeStopArguments"/> instead.
	/// </summary>
	public string stopArguments;

	/// <summary>
	/// Optional destructive variant of <see cref="stopArguments"/>, used only when the user passes
	/// <c>beam local stop --purge</c> (e.g. <c>compose down -v</c>, which removes the containers and their
	/// volumes and therefore the local database). When unset, <c>--purge</c> falls back to
	/// <see cref="stopArguments"/>.
	/// </summary>
	public string purgeStopArguments;

	/// <summary>How long to wait for the readiness gate before giving up and continuing anyway.</summary>
	public int readyTimeoutSeconds = 120;

	/// <summary>
	/// If a readiness-gated step exits before becoming ready, relaunch it up to this many times (with a short
	/// delay) before giving up. Use for services that can lose a startup race with a dependency — e.g. the C#
	/// gateway crashing because Mongo hasn't finished initializing its users yet.
	/// </summary>
	public int readyRetries = 0;
}

/// <summary>Loads/saves <see cref="LocalStackConfig"/> and applies <c>${...}</c> token substitution.</summary>
public static class LocalStackConfigIO
{
	public const string DefaultFileName = "local-stack.json";

	/// <summary>
	/// Marker `beam local init` writes in place of a path it could not resolve (<c>&lt;EDIT: absolute path to
	/// ...&gt;</c>). A value still holding it is "not filled in yet", never a real path.
	/// </summary>
	public const string EditPlaceholder = "<EDIT:";

	private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
	{
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Ignore,
		DefaultValueHandling = DefaultValueHandling.Include
	};

	public static LocalStackConfig Load(string path)
	{
		var json = File.ReadAllText(path);
		var config = JsonConvert.DeserializeObject<LocalStackConfig>(json);
		if (config == null)
			throw new InvalidOperationException($"Could not parse local-stack manifest at {path}");
		config.steps ??= new List<LocalStackStep>();
		return config;
	}

	public static void Save(string path, LocalStackConfig config)
	{
		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);
		File.WriteAllText(path, JsonConvert.SerializeObject(config, Settings));
	}

	/// <summary>
	/// Replaces the manifest tokens in a value: <c>${host}</c>, <c>${portalUrl}</c>, <c>${java}</c> (a JAVA_HOME
	/// <em>directory</em>) and the toolchain command tokens <c>${maven}</c>, <c>${npm}</c>, <c>${node}</c>,
	/// <c>${dotnet}</c> (absolute <em>executables</em>).
	///
	/// The command tokens fall back to the bare command name when no toolchain is recorded, so a manifest written
	/// with them still runs on a machine where <c>beam local setup</c> was never used — it just resolves through
	/// <c>PATH</c> as it always did.
	/// </summary>
	public static string Substitute(string value, LocalStackConfig config)
	{
		if (string.IsNullOrEmpty(value)) return value;
		return value
			.Replace("${host}", config.host ?? string.Empty)
			.Replace("${portalUrl}", config.portalUrl ?? string.Empty)
			.Replace("${java}", config.javaHome ?? string.Empty)
			.Replace("${maven}", MavenCommand(config))
			.Replace("${npm}", NpmCommand(config))
			.Replace("${node}", NodeCommand(config))
			.Replace("${dotnet}", DotnetCommand(config));
	}

	/// <summary>The <c>mvn</c> to invoke: the toolchain's when there is one, otherwise the bare command.</summary>
	public static string MavenCommand(LocalStackConfig config) =>
		ToolCommand(config?.toolchain?.maven, "bin", OperatingSystem.IsWindows() ? "mvn.cmd" : "mvn");

	/// <summary>The <c>npm</c> to invoke. On Windows the Node archive puts <c>npm.cmd</c> at its root, not in <c>bin/</c>.</summary>
	public static string NpmCommand(LocalStackConfig config) =>
		OperatingSystem.IsWindows()
			? ToolCommand(config?.toolchain?.node, null, "npm.cmd")
			: ToolCommand(config?.toolchain?.node, "bin", "npm");

	/// <inheritdoc cref="NpmCommand"/>
	public static string NodeCommand(LocalStackConfig config) =>
		OperatingSystem.IsWindows()
			? ToolCommand(config?.toolchain?.node, null, "node.exe")
			: ToolCommand(config?.toolchain?.node, "bin", "node");

	/// <summary>The <c>dotnet</c> to invoke; the SDK's executable sits at the install root rather than in <c>bin/</c>.</summary>
	public static string DotnetCommand(LocalStackConfig config) =>
		ToolCommand(config?.toolchain?.dotnet, null, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");

	/// <summary>
	/// Builds an absolute executable path under a tool home, falling back to <paramref name="bareCommand"/> when
	/// the home is unset or does not actually contain the executable. The existence check matters: a manifest can
	/// name a toolchain directory that has since been deleted, and resolving to a path that isn't there would fail
	/// with "no such file" instead of quietly working via PATH.
	/// </summary>
	private static string ToolCommand(string home, string binSubdir, string bareCommand)
	{
		if (string.IsNullOrWhiteSpace(home) || home.Contains(EditPlaceholder, StringComparison.Ordinal))
			return bareCommand;

		try
		{
			var dir = string.IsNullOrEmpty(binSubdir) ? home : Path.Combine(home, binSubdir);
			var full = Path.Combine(dir, bareCommand);
			return File.Exists(full) ? full : bareCommand;
		}
		catch
		{
			return bareCommand;
		}
	}

	/// <summary>
	/// The directories to prepend to a step's <c>PATH</c> so nested invocations stay inside the toolchain —
	/// <c>npm</c> execs <c>node</c>, <c>mvn</c> execs <c>java</c>, and <c>dotnet build</c> resolves SDKs. Without
	/// this a toolchain <c>mvn</c> still compiles the Scala reactor with whatever JDK is first on the inherited
	/// PATH, which is the exact drift the toolchain exists to prevent. Empty when no toolchain is recorded.
	/// </summary>
	public static IEnumerable<string> ToolchainPathPrefix(LocalStackConfig config)
	{
		var toolchain = config?.toolchain;
		if (toolchain == null) yield break;

		// Java first: it is the one every other tool silently picks up from PATH.
		foreach (var dir in new[]
		{
			Bin(toolchain.java, "bin"),
			Bin(toolchain.maven, "bin"),
			Bin(toolchain.dotnet, null),
			Bin(toolchain.node, OperatingSystem.IsWindows() ? null : "bin"),
			// pnpm is invoked by name from inside `beam web publish`, so it has to be on PATH — there is no token
			// to substitute for it.
			Bin(toolchain.pnpm, OperatingSystem.IsWindows() ? null : "bin"),
		})
		{
			if (dir != null) yield return dir;
		}

		static string Bin(string home, string subdir)
		{
			if (string.IsNullOrWhiteSpace(home) || home.Contains(EditPlaceholder, StringComparison.Ordinal))
				return null;

			try
			{
				var dir = string.IsNullOrEmpty(subdir) ? home : Path.Combine(home, subdir);
				return Directory.Exists(dir) ? dir : null;
			}
			catch
			{
				return null;
			}
		}
	}

	/// <summary>
	/// Resolves a step's <see cref="LocalStackStep.requiredOutput"/> to an absolute path (null when unset).
	/// A relative value is taken against the step's <see cref="LocalStackStep.workingDirectory"/>. Returns null
	/// rather than throwing when the path can't be resolved — an unedited <c>&lt;EDIT: ...&gt;</c> placeholder
	/// must never be read as "the output is missing, go build it".
	/// </summary>
	public static string ResolveRequiredOutput(LocalStackStep step, LocalStackConfig config)
	{
		var raw = Substitute(step?.requiredOutput, config);
		if (string.IsNullOrWhiteSpace(raw) || raw.Contains(EditPlaceholder, StringComparison.Ordinal))
		{
			return null; // the repo path was never filled in — "missing output" would be a lie
		}

		try
		{
			if (Path.IsPathRooted(raw))
			{
				return Path.GetFullPath(raw);
			}

			var workDir = Substitute(step.workingDirectory, config);
			return string.IsNullOrWhiteSpace(workDir)
				? Path.GetFullPath(raw)
				: Path.GetFullPath(Path.Combine(workDir, raw));
		}
		catch
		{
			return null; // unresolvable (placeholder path, invalid chars) — treat as "nothing to check"
		}
	}

	/// <summary>
	/// True when a build step declares an output that does not exist yet. <c>beam local up</c> runs such a step
	/// even without <c>--build</c>, so the run step that follows it has a binary to launch.
	/// </summary>
	public static bool BuildOutputMissing(LocalStackStep step, LocalStackConfig config)
	{
		if (step == null || !step.build)
		{
			return false;
		}

		// A Scala reactor step is "missing output" when ANY of its modules has never been compiled.
		if (FirstUnbuiltScalaModule(step, config) != null)
		{
			return true;
		}

		// An `npm install` step produces node_modules. Inferring that covers manifests written before the step
		// declared it, so an existing stack self-heals without being regenerated — which matters because the
		// symptom (`Cannot find package 'vite'`) looks nothing like "dependencies were never installed".
		var path = ResolveRequiredOutput(step, config) ?? ImplicitNpmInstallOutput(step, config);
		if (path == null)
		{
			return false;
		}

		return !File.Exists(path) && !Directory.Exists(path);
	}

	/// <summary>
	/// <c>&lt;workingDirectory&gt;/node_modules</c> for a build step that runs <c>npm install</c>, or null for
	/// anything else. Only consulted when the step declares no <see cref="LocalStackStep.requiredOutput"/>.
	/// </summary>
	private static string ImplicitNpmInstallOutput(LocalStackStep step, LocalStackConfig config)
	{
		if (step?.build != true || step.beam || step.shell) return null;

		var command = step.command ?? string.Empty;
		var isNpm = command.Contains(LocalStackTemplate.NpmToken, StringComparison.Ordinal)
		            || Path.GetFileName(command.Trim().Trim('"')) is "npm" or "npm.cmd";

		if (!isNpm) return null;

		// `install` only — `npm run dev` and friends produce nothing to check for.
		var arguments = (step.arguments ?? string.Empty).Trim();
		if (!arguments.Equals("install", StringComparison.OrdinalIgnoreCase)
		    && !arguments.StartsWith("install ", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var workDir = Substitute(step.workingDirectory, config);
		if (string.IsNullOrWhiteSpace(workDir)
		    || workDir.Contains(EditPlaceholder, StringComparison.Ordinal))
		{
			return null;
		}

		try
		{
			return Path.Combine(Path.GetFullPath(workDir), "node_modules");
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// The module list from a Maven <c>-pl a,b,c</c> argument, so a manifest written before
	/// <see cref="LocalStackStep.scalaModules"/> existed still gets the never-built check.
	/// </summary>
	private static List<string> ScalaModulesFromMavenArguments(LocalStackStep step)
	{
		var arguments = step?.arguments;
		if (string.IsNullOrWhiteSpace(arguments)) return null;

		var tokens = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < tokens.Length - 1; i++)
		{
			if (!tokens[i].Equals("-pl", StringComparison.Ordinal)) continue;

			return tokens[i + 1]
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();
		}

		return null;
	}

	/// <summary>
	/// The first module in <see cref="LocalStackStep.scalaModules"/> with no compiled output, or null when they
	/// have all been built at least once.
	///
	/// "Built" is deliberately the SAME predicate the launch script uses — a <c>*.class</c> under
	/// <c>target/classes</c>, or a built jar — so the check and the thing it guards can never disagree. Testing
	/// for the <c>target/classes</c> directory alone would not do: Maven creates it even for a build that
	/// produced nothing.
	/// </summary>
	public static string FirstUnbuiltScalaModule(LocalStackStep step, LocalStackConfig config)
	{
		// Fall back to the modules named in the mvn `-pl` list. Manifests written before `scalaModules` existed
		// still carry them there, so an existing stack gets this check without having to be regenerated.
		var modules = step?.scalaModules != null && step.scalaModules.Count > 0
			? step.scalaModules
			: ScalaModulesFromMavenArguments(step);

		if (modules == null || modules.Count == 0) return null;

		var workDir = Substitute(step.workingDirectory, config);
		if (string.IsNullOrWhiteSpace(workDir)
		    || workDir.Contains(EditPlaceholder, StringComparison.Ordinal)
		    || !Directory.Exists(workDir))
		{
			return null; // the repo path was never filled in — "not built" would be a guess
		}

		foreach (var module in modules)
		{
			if (string.IsNullOrWhiteSpace(module)) continue;

			try
			{
				var target = Path.Combine(workDir, module, "target");
				if (!Directory.Exists(target)) return module;

				var classes = Path.Combine(target, "classes");
				var hasClasses = Directory.Exists(classes)
				                 && Directory.EnumerateFiles(classes, "*.class", SearchOption.AllDirectories).Any();

				var hasJar = Directory.EnumerateFiles(target, "*.jar", SearchOption.TopDirectoryOnly)
					.Any(j => !Path.GetFileName(j).Contains("sources", StringComparison.OrdinalIgnoreCase));

				if (!hasClasses && !hasJar) return module;
			}
			catch
			{
				// unreadable module directory — do not claim it is unbuilt
			}
		}

		return null;
	}
}
