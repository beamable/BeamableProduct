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
	/// When true (and <see cref="beam"/> is false), <see cref="arguments"/> is run through the
	/// platform shell (<c>/bin/sh -c</c> on unix, <c>cmd /c</c> on Windows), so shell features
	/// like command substitution work. Useful for complex launches (e.g. a Scala <c>java -cp $(...)</c>).
	/// </summary>
	public bool shell = false;

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

	/// <summary>If set, the step is "ready" once a stdout/stderr line contains this substring.</summary>
	public string readyWhenLogContains;

	/// <summary>How long to wait for the readiness gate before giving up and continuing anyway.</summary>
	public int readyTimeoutSeconds = 120;
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

	/// <summary>Replaces <c>${host}</c> / <c>${portalUrl}</c> tokens in a value.</summary>
	public static string Substitute(string value, LocalStackConfig config)
	{
		if (string.IsNullOrEmpty(value)) return value;
		return value
			.Replace("${host}", config.host ?? string.Empty)
			.Replace("${portalUrl}", config.portalUrl ?? string.Empty);
	}
}
