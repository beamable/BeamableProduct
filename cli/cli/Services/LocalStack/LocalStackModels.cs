using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace cli.Services.LocalStack;

/// <summary>
/// A generic, machine-agnostic description of a full local Beamable stack: a list of
/// processes (<see cref="LocalStackStep"/>) to bring up in order, each with an optional
/// readiness gate. This is the C# equivalent of the <c>scripts/run-local-stack.sh</c>
/// orchestrator, but driven entirely by a JSON manifest so it is not tied to any one machine.
///
/// The manifest lives (by default) at <c>&lt;workspace&gt;/.beamable/local/stack-settings.json</c> and is
/// created by <c>beam local setup</c>. Repo folders live only in <see cref="paths"/>; every step refers to them
/// as <c>${PortalPath}</c>, <c>${BeamAPIPath}</c> and so on.
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
	/// Where each repo checkout lives on this machine, by variable name: <c>PortalPath</c>, <c>BeamProductPath</c>,
	/// <c>BeamAPIPath</c>, <c>BeamBackendPath</c> and <c>WebRegistryPath</c>. Steps refer to them as <c>${Name}</c>,
	/// so this is the only place a repo folder is written. A value may be relative to the workspace root (the
	/// folder holding <c>.beamable</c>) and may refer to another variable.
	///
	/// <see cref="repos"/> is the in-memory view of the same data: <see cref="LocalStackConfigIO.Load"/> fills it
	/// from here, and <see cref="LocalStackConfigIO.Save"/> writes it back here, so code that reads
	/// <c>repos.apiDir</c> keeps working.
	/// </summary>
	public Dictionary<string, string> paths;

	/// <summary>
	/// Named values a team or developer adds, usable as <c>${Name}</c> anywhere a token is substituted. A value may
	/// refer to other variables and to <see cref="paths"/>. Built-in tokens (<c>${host}</c>, <c>${java}</c>, ...)
	/// win over a variable of the same name.
	/// </summary>
	public Dictionary<string, string> variables;

	/// <summary>
	/// Named backends a stack can run against (<c>local</c>, <c>dev</c>, <c>staging</c>, ...). Stored only for now:
	/// nothing reads it yet.
	/// </summary>
	public Dictionary<string, LocalStackTarget> targets;

	/// <summary>Shift applied to every step port and to the host and portal URLs, so several stacks can run side by side.
	/// Stored only for now: nothing applies it yet.</summary>
	public int portOffset;

	/// <summary>How many days local logs and traces are kept. Stored only for now: nothing applies it yet.</summary>
	public int logRetentionDays = 5;

	/// <summary>The folder holding the workspace's <c>.beamable</c> folder; relative <see cref="paths"/> are taken
	/// against it. Set by <see cref="LocalStackConfigIO.Load"/> and <see cref="LocalStackConfigIO.Save"/>.</summary>
	[JsonIgnore]
	public string workspaceRoot;

	/// <summary>
	/// The Java 8 <c>JAVA_HOME</c> the Scala backend runs under. Substituted via the <c>${java}</c> token
	/// (e.g. <c>${java}/bin/java</c>). Resolved by <c>beam local run</c> from <c>--java-path</c> /
	/// <c>BEAM_JAVA_HOME</c> / auto-detection when left null, so a shared manifest stays machine-agnostic.
	/// </summary>
	public string javaHome;

	/// <summary>
	/// The pinned toolchain <c>beam local setup</c> provisioned, if any: private installs of JDK 8, Maven, the
	/// .NET SDK and Node that the steps below resolve their commands from instead of <c>PATH</c>.
	///
	/// Null when setup provisioned none (and omitted from the JSON then): every token falls back to the bare
	/// command name on <c>PATH</c>.
	/// </summary>
	public LocalStackToolchain toolchain;

	/// <summary>
	/// How often (seconds) an attached <c>beam local run</c> re-checks that the containers its
	/// <c>docker compose up</c> steps started are still running. 0 disables the check.
	///
	/// Docker steps are run-to-completion, so their containers are invisible to the attached wait: one can die
	/// mid-run and the orchestrator would keep streaming reconnect errors under a "Stack is up" banner. See
	/// <see cref="LocalStackDockerWatchdog"/>.
	/// </summary>
	public int dockerWatchdogSeconds = 15;

	/// <summary>
	/// The repository checkouts this manifest was generated against. Documentation-only metadata: the steps
	/// carry their own absolute <see cref="LocalStackStep.workingDirectory"/> values and nothing in the
	/// orchestrator reads this. It exists so the generated <c>beam-local-stack</c> agent skill (see
	/// <see cref="LocalStackSkillTemplate"/>) can name the repos without reverse-engineering step names —
	/// which also keeps it correct for <c>beam local setup --update-services</c>, where the repo paths are
	/// never prompted for.
	///
	/// Optional (omitted from the JSON when null), so readers treat it as such.
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
/// <c>beam local setup</c> could not resolve it, or be null when that part of the stack is not included.
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

/// <summary>Thrown when a variable refers back to itself, directly or through other variables.</summary>
public class VariableCycleException : InvalidOperationException
{
	/// <summary>The variables in the cycle, in reference order.</summary>
	public readonly List<string> cycle;

	public VariableCycleException(List<string> cycle) : base(Describe(cycle)) => this.cycle = cycle;

	/// <summary>The message for this cycle started from its alphabetically first name, so A→B→A and B→A→B match.</summary>
	public string CanonicalMessage
	{
		get
		{
			var first = cycle.IndexOf(cycle.Min(StringComparer.Ordinal));
			return Describe(cycle.Skip(first).Concat(cycle.Take(first)).ToList());
		}
	}

	private static string Describe(List<string> cycle) =>
		"Variable cycle: " + string.Join(" -> ", cycle.Append(cycle[0]).Select(n => "${" + n + "}"));
}

/// <summary>A backend a stack can run against: the API host and, for a remote one, the realm to use.</summary>
public class LocalStackTarget
{
	/// <summary>The backend's API host, such as <c>https://dev.api.beamable.com</c>.</summary>
	public string host;

	/// <summary>The org: its alias or cid. Asked for at the first login when unset.</summary>
	public string cid;

	/// <summary>The realm. Picked at the first login when unset.</summary>
	public string pid;

	/// <summary>
	/// True when the target is this workspace's own backend, reached at another address: the stack runs it, and logs
	/// into the local realm instead of asking for a login. <c>lan</c> is one: the same backend at this machine's LAN
	/// address, which phones and other machines can reach and <c>localhost</c> can't.
	/// </summary>
	public bool? local;

	/// <summary>True to ask before every run against it, as <c>prod</c> does: the services started there serve the
	/// realm's live traffic.</summary>
	public bool? confirm;
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

	/// <summary>A readable name for the step ("Scala service auth"). Inferred from <see cref="name"/> when the
	/// manifest is saved and none is set.</summary>
	public string label;

	/// <summary>What kind of step this is (<c>docker</c>, <c>csharp</c>, <c>scala</c>, <c>portal</c>, <c>extensions</c>,
	/// <c>web-registry</c>, <c>build</c>), so stacks can select steps by kind. Inferred from <see cref="name"/> when the
	/// manifest is saved and none are set.</summary>
	public List<string> tags;

	/// <summary>
	/// Steps this one can't work without, by name or tag (a tag means every step that has it): the C# gateway
	/// needs the docker dependencies, a Scala service needs Redis, dbflake and the gateway, an extension needs the
	/// microservices it calls. <c>beam local create</c> adds what a stack is missing, and <c>beam local run</c> warns
	/// about it. Omitted when there are none.
	/// </summary>
	public List<string> dependsOn;

	/// <summary>
	/// For a step a stack defines itself: the step it runs after, by name. Unset, it runs after every other step; see
	/// also <see cref="first"/>. Catalogue steps keep the catalogue's order and don't use it.
	/// </summary>
	public string after;

	/// <summary>For a step a stack defines itself: true to run it before every other step.</summary>
	public bool? first;

	/// <summary>When false, the step is skipped entirely.</summary>
	public bool enabled = true;

	/// <summary>
	/// True for a step added with <c>beam local setup --add-step</c> rather than generated: it is kept when the
	/// generated catalogue is recreated, and it is the only kind <c>--remove-step</c> removes. Omitted otherwise.
	/// </summary>
	public bool? custom;

	/// <summary>
	/// When true, this is a build/compile step: it only runs when <c>beam local run &lt;stack&gt;:build</c> is passed
	/// (skipped otherwise). Build steps are run-to-completion (<see cref="waitForExit"/>) and are not tracked
	/// in the run-state — they compile a component before its run step, they are not a running service.
	/// </summary>
	public bool build = false;

	/// <summary>
	/// For a build step: the file (or directory) the build is expected to produce. When set and missing, the
	/// step runs even <b>without</b> <c>beam local run &lt;stack&gt;:build</c> — so a fresh clone (or a project nobody has
	/// built yet) self-heals instead of launching a binary that isn't there, and <c>up</c> fails loudly when a
	/// build "succeeds" without producing it. Mirrors the reference <c>scripts/run-local-stack.sh</c>, which
	/// builds each gateway/worker binary when it is missing. Absolute, or relative to
	/// <see cref="workingDirectory"/>. Tokens are substituted.
	/// </summary>
	public string requiredOutput;

	/// <summary>
	/// For a build step: the folders and files it compiles, relative to <see cref="workingDirectory"/> (or with path
	/// variables). <c>beam local run</c> builds the step when they changed since its last successful build, and shows
	/// it as up to date otherwise. Without inputs a build step only builds when its output is missing.
	/// </summary>
	public List<string> inputs;

	/// <summary>
	/// For the Scala reactor build step: the module directories it produces output for, relative to
	/// <see cref="workingDirectory"/> (e.g. <c>core</c>, <c>tools/auth</c>).
	///
	/// <c>beam local run</c> runs this build WITHOUT <c>:build</c> when any listed module has never been compiled.
	/// A plain first `up` on a fresh clone otherwise launched JVMs against modules with no classes, which does not
	/// fail cleanly — the service starts, half the stack proxies through it, and the result is a "malfunctioning"
	/// backend rather than an obvious build error. A single <see cref="requiredOutput"/> cannot express this
	/// because one step builds ~18 modules.
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
	/// Set by <c>beam local setup</c> to the generating OS so <c>up</c> runs the script with the right
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
	/// <c>beam local run</c> fails fast when this port is already held by something that is <em>not</em> this
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
	/// <summary>The manifest's file name. It lives in <see cref="SettingsFolder"/> under the workspace's
	/// <c>.beamable</c> folder, which holds per-machine files.</summary>
	public const string SettingsFileName = "stack-settings.json";

	/// <inheritdoc cref="SettingsFileName"/>
	public const string SettingsFolder = "local";

	/// <summary>The path variables a manifest's repo folders are stored under (see <see cref="LocalStackConfig.paths"/>).</summary>
	public const string PortalPathVar = "PortalPath";

	/// <inheritdoc cref="PortalPathVar"/>
	public const string ProductPathVar = "BeamProductPath";

	/// <inheritdoc cref="PortalPathVar"/>
	public const string ApiPathVar = "BeamAPIPath";

	/// <inheritdoc cref="PortalPathVar"/>
	public const string BackendPathVar = "BeamBackendPath";

	/// <inheritdoc cref="PortalPathVar"/>
	public const string WebRegistryPathVar = "WebRegistryPath";

	/// <summary>
	/// Marker `beam local setup` writes in place of a path it could not resolve (<c>&lt;EDIT: absolute path to
	/// ...&gt;</c>). A value still holding it is "not filled in yet", never a real path.
	/// </summary>
	public const string EditPlaceholder = "<EDIT:";

	private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
	{
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Ignore,
		DefaultValueHandling = DefaultValueHandling.Include
	};

	/// <summary>A <c>${Name}</c> reference to a variable.</summary>
	private static readonly Regex VariableToken = new Regex(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

	/// <summary>Tokens with fixed meanings. <c>${mainClass}</c> is substituted per step at launch time.</summary>
	private static readonly HashSet<string> BuiltInTokens = new HashSet<string>(StringComparer.Ordinal)
	{
		"host", "portalUrl", "java", "maven", "npm", "node", "dotnet", "mainClass"
	};

	public static LocalStackConfig Load(string path)
	{
		var json = File.ReadAllText(path);
		var config = JsonConvert.DeserializeObject<LocalStackConfig>(json);
		if (config == null)
			throw new InvalidOperationException($"Could not parse local-stack manifest at {path}");
		config.steps ??= new List<LocalStackStep>();
		config.workspaceRoot = FindWorkspaceRoot(path);
		ApplyPathsToRepos(config);
		return config;
	}

	/// <summary>
	/// Writes the manifest. Repo folders are stored once, as <see cref="LocalStackConfig.paths"/>: every step value
	/// that contains one is rewritten to use its <c>${Name}</c> variable, and steps get an inferred label and tags
	/// when they have none. <see cref="LocalStackConfig.repos"/> is only written by a manifest without paths.
	/// </summary>
	public static void Save(string path, LocalStackConfig config)
	{
		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);

		config.workspaceRoot ??= FindWorkspaceRoot(path);
		UseRepoPathVariables(config);
		InferLabelsAndTags(config);

		var repos = config.repos;
		if (config.paths?.Count > 0)
			config.repos = null;
		try
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(config, Settings));
		}
		finally
		{
			config.repos = repos;
		}
	}

	/// <summary>
	/// The folder holding the <c>.beamable</c> folder the manifest sits in (at any depth), or the manifest's own
	/// folder when it isn't inside one. Relative <see cref="LocalStackConfig.paths"/> are taken against it.
	/// </summary>
	public static string FindWorkspaceRoot(string manifestPath)
	{
		try
		{
			var start = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? ".");
			for (var dir = start; dir != null; dir = dir.Parent)
			{
				if (string.Equals(dir.Name, ConfigService.CFG_FOLDER, StringComparison.OrdinalIgnoreCase))
					return dir.Parent?.FullName ?? dir.FullName;
			}

			return start.FullName;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Replaces the manifest tokens in a value: <c>${host}</c>, <c>${portalUrl}</c>, <c>${java}</c> (a JAVA_HOME
	/// <em>directory</em>), the toolchain command tokens <c>${maven}</c>, <c>${npm}</c>, <c>${node}</c>,
	/// <c>${dotnet}</c> (absolute <em>executables</em>), and every <see cref="LocalStackConfig.variables"/> and
	/// <see cref="LocalStackConfig.paths"/> entry by name. A path variable becomes an absolute folder.
	///
	/// The command tokens fall back to the bare command name when no toolchain is recorded, so a manifest written
	/// with them still runs on a machine where <c>beam local setup</c> was never used — it just resolves through
	/// <c>PATH</c> as it always did. An unknown <c>${Name}</c> is left as it is; <see cref="FindUnknownVariables"/>
	/// reports it. A variable that refers back to itself throws.
	/// </summary>
	public static string Substitute(string value, LocalStackConfig config)
	{
		if (string.IsNullOrEmpty(value)) return value;
		return SubstituteVariables(ReplaceBuiltInTokens(value, config), config, new List<string>());
	}

	private static string ReplaceBuiltInTokens(string value, LocalStackConfig config) =>
		value
			.Replace("${host}", config.host ?? string.Empty)
			.Replace("${portalUrl}", config.portalUrl ?? string.Empty)
			.Replace("${java}", config.javaHome ?? string.Empty)
			.Replace("${maven}", MavenCommand(config))
			.Replace("${npm}", NpmCommand(config))
			.Replace("${node}", NodeCommand(config))
			.Replace("${dotnet}", DotnetCommand(config));

	private static string SubstituteVariables(string value, LocalStackConfig config, List<string> chain)
	{
		if (string.IsNullOrEmpty(value) || !value.Contains("${", StringComparison.Ordinal)) return value;
		if ((config.variables == null || config.variables.Count == 0) && (config.paths == null || config.paths.Count == 0))
			return value;

		// A MatchEvaluator, not a replacement string: "${Name}" in a replacement string means a regex group.
		return VariableToken.Replace(value, m => ResolveVariable(m.Groups[1].Value, config, chain) ?? m.Value);
	}

	/// <summary>The value of one variable or path variable, fully substituted, or null when no such name exists.</summary>
	private static string ResolveVariable(string name, LocalStackConfig config, List<string> chain)
	{
		if (BuiltInTokens.Contains(name)) return null;

		string raw;
		bool isPath;
		if (config.variables != null && config.variables.TryGetValue(name, out raw))
			isPath = false;
		else if (config.paths != null && config.paths.TryGetValue(name, out raw))
			isPath = true;
		else
			return null;

		if (chain.Contains(name))
			throw new VariableCycleException(chain.SkipWhile(n => n != name).ToList());

		chain.Add(name);
		try
		{
			var value = SubstituteVariables(ReplaceBuiltInTokens(raw ?? string.Empty, config), config, chain);
			return isPath ? ToAbsoluteFolder(value, config) : value;
		}
		finally
		{
			chain.RemoveAt(chain.Count - 1);
		}
	}

	private static string ToAbsoluteFolder(string value, LocalStackConfig config)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Contains(EditPlaceholder, StringComparison.Ordinal))
			return value;

		try
		{
			return Path.IsPathRooted(value)
				? Path.GetFullPath(value)
				: Path.GetFullPath(Path.Combine(config.workspaceRoot ?? Directory.GetCurrentDirectory(), value));
		}
		catch
		{
			return value;
		}
	}

	/// <summary>Every variable cycle in the manifest, one message each. Empty when there are none.</summary>
	public static List<string> FindVariableCycles(LocalStackConfig config)
	{
		var cycles = new List<string>();
		var names = (config.variables?.Keys ?? Enumerable.Empty<string>())
			.Concat(config.paths?.Keys ?? Enumerable.Empty<string>());
		foreach (var name in names)
		{
			try
			{
				ResolveVariable(name, config, new List<string>());
			}
			catch (VariableCycleException e)
			{
				// The same cycle is found from each of its members; report it once, from its first name.
				if (!cycles.Contains(e.CanonicalMessage)) cycles.Add(e.CanonicalMessage);
			}
		}

		return cycles;
	}

	/// <summary>
	/// <c>${Name}</c> references that match no built-in token, variable or path variable, as
	/// <c>(where, name)</c>. Shell steps are skipped: a shell script can use <c>${NAME}</c> for its own variables.
	/// </summary>
	public static List<(string where, string name)> FindUnknownVariables(LocalStackConfig config)
	{
		var unknown = new List<(string, string)>();

		bool Known(string name) =>
			BuiltInTokens.Contains(name)
			|| config.variables?.ContainsKey(name) == true
			|| config.paths?.ContainsKey(name) == true;

		void Scan(string where, string value)
		{
			if (string.IsNullOrEmpty(value)) return;
			foreach (Match m in VariableToken.Matches(value))
			{
				var name = m.Groups[1].Value;
				if (!Known(name) && !unknown.Contains((where, name))) unknown.Add((where, name));
			}
		}

		foreach (var (name, value) in config.variables ?? new Dictionary<string, string>())
			Scan($"variable {name}", value);
		foreach (var (name, value) in config.paths ?? new Dictionary<string, string>())
			Scan($"path {name}", value);

		foreach (var step in config.steps.Where(s => !s.shell))
		{
			var where = $"step '{step.name}'";
			foreach (var value in StepValues(step))
				Scan(where, value);
		}

		return unknown;
	}

	/// <summary>
	/// Every <c>${Name}</c> the steps use, in first-use order, with its value and where the value comes from:
	/// <c>built-in</c>, <c>paths</c> or <c>variables</c>. In shell steps only known names count, since a shell
	/// script can use <c>${NAME}</c> for its own variables; an unknown name elsewhere is listed as not set.
	/// </summary>
	public static List<(string name, string value, string source)> DescribeVariables(IEnumerable<LocalStackStep> steps, LocalStackConfig config)
	{
		var described = new List<(string name, string value, string source)>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var step in steps)
		{
			foreach (var value in StepValues(step).Where(v => !string.IsNullOrEmpty(v)))
			{
				foreach (Match m in VariableToken.Matches(value))
				{
					var name = m.Groups[1].Value;
					var source = BuiltInTokens.Contains(name) ? "built-in"
						: config.variables?.ContainsKey(name) == true ? "variables"
						: config.paths?.ContainsKey(name) == true ? "paths"
						: null;
					if ((source == null && step.shell) || !seen.Add(name)) continue;

					string resolved;
					try
					{
						resolved = source == null ? "(not set)" : Substitute("${" + name + "}", config);
					}
					catch (VariableCycleException e)
					{
						resolved = e.CanonicalMessage;
					}

					described.Add((name, resolved, source ?? "not set"));
				}
			}
		}

		return described;
	}

	/// <summary>Fills <see cref="LocalStackConfig.repos"/> from <see cref="LocalStackConfig.paths"/>.</summary>
	private static void ApplyPathsToRepos(LocalStackConfig config)
	{
		if (config.paths == null || config.paths.Count == 0) return;

		string Folder(string name)
		{
			try
			{
				return ResolveVariable(name, config, new List<string>());
			}
			catch (InvalidOperationException)
			{
				return null; // a cycle: `local validate` reports it
			}
		}

		config.repos ??= new LocalStackRepos();
		config.repos.portalDir = Folder(PortalPathVar) ?? config.repos.portalDir;
		config.repos.productDir = Folder(ProductPathVar) ?? config.repos.productDir;
		config.repos.apiDir = Folder(ApiPathVar) ?? config.repos.apiDir;
		config.repos.scalaDir = Folder(BackendPathVar) ?? config.repos.scalaDir;
		config.repos.webRegistryDir = Folder(WebRegistryPathVar) ?? config.repos.webRegistryDir;
	}

	/// <summary>
	/// Records the repo folders as path variables and rewrites every step value that contains one of those folders
	/// to use the variable instead, so the manifest holds each folder exactly once.
	/// </summary>
	private static void UseRepoPathVariables(LocalStackConfig config)
	{
		var repos = config.repos;
		if (repos != null)
		{
			config.paths ??= new Dictionary<string, string>();
			SetPath(config, PortalPathVar, repos.portalDir);
			SetPath(config, ProductPathVar, repos.productDir);
			SetPath(config, ApiPathVar, repos.apiDir);
			SetPath(config, BackendPathVar, repos.scalaDir);
			SetPath(config, WebRegistryPathVar, repos.webRegistryDir);
		}

		if (config.paths == null || config.paths.Count == 0)
		{
			config.paths = null;
			return;
		}

		// Longest folder first, so a folder nested in another (portal-localdev inside BeamableProduct) gets its own
		// variable rather than the outer one's.
		var folders = new List<(string name, string folder)>();
		foreach (var name in config.paths.Keys)
		{
			string folder;
			try
			{
				folder = ResolveVariable(name, config, new List<string>());
			}
			catch (InvalidOperationException)
			{
				continue;
			}

			if (!string.IsNullOrWhiteSpace(folder) && Path.IsPathRooted(folder)
			                                       && !folder.Contains(EditPlaceholder, StringComparison.Ordinal))
				folders.Add((name, folder.TrimEnd('\\', '/')));
		}

		folders = folders.OrderByDescending(f => f.folder.Length).ToList();
		if (folders.Count == 0) return;

		foreach (var step in config.steps)
		{
			step.workingDirectory = ReplaceFolders(step.workingDirectory, folders);
			step.command = ReplaceFolders(step.command, folders);
			step.arguments = ReplaceFolders(step.arguments, folders);
			step.requiredOutput = ReplaceFolders(step.requiredOutput, folders);
			step.inputs = step.inputs?.Select(i => ReplaceFolders(i, folders)).ToList();
			step.stopArguments = ReplaceFolders(step.stopArguments, folders);
			step.purgeStopArguments = ReplaceFolders(step.purgeStopArguments, folders);
			if (step.environment != null)
			{
				foreach (var key in step.environment.Keys.ToList())
					step.environment[key] = ReplaceFolders(step.environment[key], folders);
			}
		}
	}

	private static IEnumerable<string> StepValues(LocalStackStep step)
	{
		yield return step.workingDirectory;
		yield return step.command;
		yield return step.arguments;
		yield return step.requiredOutput;
		foreach (var input in step.inputs ?? Enumerable.Empty<string>())
			yield return input;
		yield return step.stopArguments;
		yield return step.purgeStopArguments;
		yield return step.readyWhenHttpOk;
		yield return step.readyWhenHttp200;
		foreach (var value in step.environment?.Values ?? Enumerable.Empty<string>())
			yield return value;
	}

	/// <summary>
	/// Stores <paramref name="folder"/> as path variable <paramref name="name"/>, relative to the workspace root when
	/// it can be. An existing value that already points at the same folder is kept as written.
	/// </summary>
	private static void SetPath(LocalStackConfig config, string name, string folder)
	{
		if (string.IsNullOrWhiteSpace(folder)) return;

		if (folder.Contains(EditPlaceholder, StringComparison.Ordinal))
		{
			config.paths[name] = folder;
			return;
		}

		string absolute;
		try
		{
			absolute = Path.GetFullPath(folder);
		}
		catch
		{
			config.paths[name] = folder;
			return;
		}

		if (config.paths.ContainsKey(name))
		{
			try
			{
				if (SameFolder(ResolveVariable(name, config, new List<string>()), absolute)) return;
			}
			catch (InvalidOperationException)
			{
				// a cycle: overwrite it with the real folder
			}
		}

		// The web registry folder sits inside BeamableProduct; say so, so it follows that checkout around.
		if (name == WebRegistryPathVar && config.paths.ContainsKey(ProductPathVar))
		{
			var product = ResolveVariable(ProductPathVar, config, new List<string>());
			if (!string.IsNullOrEmpty(product) && Path.IsPathRooted(product))
			{
				var inside = Path.GetRelativePath(product, absolute);
				if (!inside.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(inside))
				{
					config.paths[name] = "${" + ProductPathVar + "}/" + inside.Replace('\\', '/');
					return;
				}
			}
		}

		config.paths[name] = Relativize(absolute, config.workspaceRoot);
	}

	private static string Relativize(string absolute, string root)
	{
		if (string.IsNullOrEmpty(root)) return absolute;
		try
		{
			var relative = Path.GetRelativePath(root, absolute);
			return Path.IsPathRooted(relative) ? absolute : relative.Replace('\\', '/');
		}
		catch
		{
			return absolute;
		}
	}

	private static bool SameFolder(string a, string b)
	{
		if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
		try
		{
			return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'),
				OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
					? StringComparison.OrdinalIgnoreCase
					: StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Replaces each folder in <paramref name="value"/> with its <c>${Name}</c>, in either slash style.</summary>
	private static string ReplaceFolders(string value, List<(string name, string folder)> folders)
	{
		if (string.IsNullOrEmpty(value)) return value;

		var options = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? RegexOptions.IgnoreCase
			: RegexOptions.None;

		foreach (var (name, folder) in folders)
		{
			foreach (var variant in new[] { folder, folder.Replace('\\', '/'), folder.Replace('/', '\\') }.Distinct())
			{
				// Only a whole folder: "BeamableAPI" must not match the start of "BeamableAPI_payments".
				var pattern = Regex.Escape(variant) + @"(?=$|[\\/""'\s;,:)\]}])";
				value = Regex.Replace(value, pattern, _ => "${" + name + "}", options);
			}
		}

		return value;
	}

	/// <summary>Gives every step without them an inferred <see cref="LocalStackStep.label"/> and <see cref="LocalStackStep.tags"/>.</summary>
	private static void InferLabelsAndTags(LocalStackConfig config)
	{
		foreach (var step in config.steps)
		{
			if (string.IsNullOrWhiteSpace(step.name)) continue;
			step.label ??= InferLabel(step.name);
			if (step.tags == null || step.tags.Count == 0)
				step.tags = InferTags(step.name);
		}
	}

	/// <summary>The tags a step gets from its name: <c>build: c# gateway</c> is <c>build</c> + <c>csharp</c>.</summary>
	public static List<string> InferTags(string stepName)
	{
		var tags = new List<string>();
		var rest = stepName.Trim();
		if (rest.StartsWith("build:", StringComparison.OrdinalIgnoreCase))
		{
			tags.Add("build");
			rest = rest.Substring("build:".Length).Trim();
		}

		bool Starts(string prefix) => rest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

		if (Starts("web ") || rest.Equals(LocalStackTemplate.WebRegistryStepName, StringComparison.OrdinalIgnoreCase))
			tags.Add("web-registry");
		else if (Starts("docker:"))
			tags.Add("docker");
		else if (Starts("c#"))
			tags.Add("csharp");
		else if (Starts("scala"))
			tags.Add("scala");
		else if (LocalStackStacks.ServiceTagsFor(rest) is { Count: > 0 } service)
			tags.AddRange(service);
		else if (Starts("portal"))
			tags.Add("portal");

		return tags;
	}

	/// <summary>The readable label a step gets from its name: <c>scala: auth</c> is "Scala service auth".</summary>
	public static string InferLabel(string stepName)
	{
		var name = stepName.Trim();
		var prefixes = new (string prefix, string label)[]
		{
			("build:", "Build "),
			("scala:", "Scala service "),
			(LocalStackTemplate.ExtensionPrefix.Trim(), "Portal extension "),
			(LocalStackTemplate.MicroservicePrefix.Trim(), "Microservice "),
			(LocalStackTemplate.GroupPrefix.Trim(), "Service group "),
			("docker:", "Docker "),
			("portal:", "Portal "),
		};

		foreach (var (prefix, label) in prefixes)
		{
			if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
			var rest = name.Substring(prefix.Length).Trim();
			if (prefix != "build:") return label + rest;

			// "Build web packages", but "Build C# gateway" and "Build Scala": names keep their capital.
			var inner = InferLabel(rest);
			return inner.Length == 0 || inner.StartsWith("C#", StringComparison.Ordinal)
			                         || inner.StartsWith("Scala", StringComparison.Ordinal)
				? label + inner
				: label + char.ToLowerInvariant(inner[0]) + inner.Substring(1);
		}

		return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
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
	/// True when a build step declares an output that does not exist yet. <c>beam local run</c> runs such a step
	/// even without <c>:build</c>, so the run step that follows it has a binary to launch.
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

		var path = ResolveRequiredOutput(step, config);
		if (path == null)
		{
			return false;
		}

		return !File.Exists(path) && !Directory.Exists(path);
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
		var modules = step?.scalaModules;

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
