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
	/// The ordered set of processes to launch. Order matters — earlier steps that declare a
	/// readiness gate are fully up before later steps start.
	/// </summary>
	public List<LocalStackStep> steps = new List<LocalStackStep>();
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
	/// <c>compose down</c> for a <c>docker compose up -d</c> step); run as <c>command stopArguments</c> in
	/// <see cref="workingDirectory"/>.
	/// </summary>
	public string stopArguments;

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

	/// <summary>Replaces <c>${host}</c> / <c>${portalUrl}</c> / <c>${java}</c> tokens in a value.</summary>
	public static string Substitute(string value, LocalStackConfig config)
	{
		if (string.IsNullOrEmpty(value)) return value;
		return value
			.Replace("${host}", config.host ?? string.Empty)
			.Replace("${portalUrl}", config.portalUrl ?? string.Empty)
			.Replace("${java}", config.javaHome ?? string.Empty);
	}
}
