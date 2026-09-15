using Beamable.Server;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace cli.Services.LocalStack;

/// <summary>What one <see cref="ScalaLocalVarsService"/> render produced.</summary>
public class ScalaLocalVarsResult
{
	/// <summary>Config files written this run.</summary>
	public List<string> written = new List<string>();

	/// <summary>Config files that already existed and were left alone (no <c>--force</c>).</summary>
	public List<string> skipped = new List<string>();

	/// <summary>
	/// Variables the templates reference that the GitHub environment did not supply. These render as empty
	/// strings, which is how the original Python script behaves too — but an empty bucket name or role ARN fails
	/// at runtime in a way that looks nothing like a missing config value, so they are reported.
	/// </summary>
	public List<string> missingVars = new List<string>();

	/// <summary>Set when nothing could be rendered at all.</summary>
	public string error;

	public bool ok => error == null;
}

/// <summary>
/// Renders the BeamableBackend config files that are gitignored and therefore absent from a fresh clone:
/// <c>core/src/main/resources/awsglobal.conf</c> and the two <c>tools/beamo/.../server.conf</c> files. Each is
/// generated from a <c>.liquid</c> template beside it, filled from the repo's GitHub <c>local</c> environment
/// variables.
///
/// This is a port of the repo's own <c>bin/set-local-vars</c>, which is a Python script with a
/// <c>#!.venv/bin/python</c> shebang — so it needs Python plus a committed virtualenv and <b>cannot run on
/// Windows at all</b>. Doing it here removes that dependency, and means <c>beam local setup</c> can perform a
/// step the stack has always required but <c>beam local up</c> never ran, leaving a fresh clone to fail with a
/// missing-config error nobody could act on.
///
/// The templates are pure <c>{{ VAR }}</c> substitution — no Liquid tags, filters or control flow — which is why
/// a regex renderer is a faithful equivalent rather than an approximation.
/// </summary>
public class ScalaLocalVarsService
{
	/// <summary>The repository whose <c>local</c> environment holds the values.</summary>
	public const string DefaultRepo = "beamable/BeamableBackend";

	/// <summary>The GitHub deployment environment the variables live in.</summary>
	private const string GithubEnvironment = "local";

	/// <summary>GitHub's variables API pages at 30 per request, matching the original script.</summary>
	private const int VarsPerPage = 30;

	/// <summary>
	/// The generated config files, relative to the BeamableBackend checkout. All three are gitignored and all
	/// three are required: the repo's README documents only <c>awsglobal.conf</c>, but the beamo service will not
	/// start without its two <c>server.conf</c> files.
	/// </summary>
	public static readonly string[] RelativeConfPaths =
	{
		Path.Combine("core", "src", "main", "resources", "awsglobal.conf"),
		Path.Combine("tools", "beamo", "src", "main", "resources", "server.conf"),
		Path.Combine("tools", "beamo", "src", "test", "resources", "server.conf"),
	};

	/// <summary>A <c>{{ VAR }}</c> placeholder, tolerating any inner whitespace.</summary>
	private static readonly Regex Placeholder =
		new Regex(@"\{\{\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}", RegexOptions.Compiled);

	private readonly HttpClient _http;

	public ScalaLocalVarsService(HttpClient http = null)
	{
		_http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
	}

	/// <summary>
	/// Resolves a GitHub token: the explicit value, then <c>GITHUB_TOKEN</c>/<c>GH_TOKEN</c>, then whatever
	/// <c>gh auth token</c> reports. Returns null when none is available, so the caller can report a single
	/// actionable message instead of a 401 from the API.
	/// </summary>
	public static string ResolveToken(string explicitToken)
	{
		if (!string.IsNullOrWhiteSpace(explicitToken))
			return explicitToken.Trim();

		foreach (var name in new[] { "GITHUB_TOKEN", "GH_TOKEN" })
		{
			var fromEnv = Environment.GetEnvironmentVariable(name);
			if (!string.IsNullOrWhiteSpace(fromEnv))
				return fromEnv.Trim();
		}

		return TryGhCliToken();
	}

	/// <summary>Asks the GitHub CLI for its token; null when <c>gh</c> is absent or not logged in.</summary>
	private static string TryGhCliToken()
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = OperatingSystem.IsWindows() ? "gh.exe" : "gh",
				Arguments = "auth token",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = Process.Start(psi);
			if (proc == null) return null;

			var output = proc.StandardOutput.ReadToEnd();
			proc.StandardError.ReadToEnd();
			proc.WaitForExit();

			var token = output?.Trim();
			return proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(token) ? token : null;
		}
		catch
		{
			return null; // gh isn't installed — a normal case, not an error
		}
	}

	/// <summary>
	/// Renders every file in <see cref="RelativeConfPaths"/> under <paramref name="scalaDir"/>. Existing files are
	/// left alone unless <paramref name="force"/> is set — they may hold hand-edits, and clobbering a working
	/// local config on every setup run would be its own bug.
	/// </summary>
	public async Task<ScalaLocalVarsResult> RenderAsync(string scalaDir, string repo, string token, bool force,
		CancellationToken cancellation)
	{
		var result = new ScalaLocalVarsResult();

		if (string.IsNullOrWhiteSpace(scalaDir) || !Directory.Exists(scalaDir))
		{
			result.error = $"BeamableBackend checkout not found at '{scalaDir}'. " +
			               "Pass --scala-dir (or fix `repos.scalaDir` in the manifest).";
			return result;
		}

		// Figure out which files are actually missing before spending a network round-trip on the variables.
		var pending = new List<string>();
		foreach (var relative in RelativeConfPaths)
		{
			var output = Path.Combine(scalaDir, relative);
			if (!force && File.Exists(output))
				result.skipped.Add(output);
			else
				pending.Add(relative);
		}

		if (pending.Count == 0)
			return result;

		if (string.IsNullOrWhiteSpace(token))
		{
			result.error =
				"No GitHub token available for the `local` environment variables. Pass --github-token, set " +
				"GITHUB_TOKEN, or run `gh auth login` so `gh auth token` works.";
			return result;
		}

		Dictionary<string, string> variables;
		try
		{
			variables = await FetchVariablesAsync(repo ?? DefaultRepo, token, cancellation);
		}
		catch (Exception e)
		{
			result.error = $"Could not read the `{GithubEnvironment}` environment variables of {repo ?? DefaultRepo}: {e.Message}";
			return result;
		}

		var missing = new SortedSet<string>(StringComparer.Ordinal);
		foreach (var relative in pending)
		{
			var template = Path.Combine(scalaDir, relative + ".liquid");
			if (!File.Exists(template))
			{
				result.error = $"Template {template} is missing — this checkout does not look like BeamableBackend.";
				return result;
			}

			var rendered = Render(await File.ReadAllTextAsync(template, cancellation), variables, missing);
			var output = Path.Combine(scalaDir, relative);
			Directory.CreateDirectory(Path.GetDirectoryName(output));
			await File.WriteAllTextAsync(output, rendered, cancellation);
			result.written.Add(output);
		}

		result.missingVars.AddRange(missing);
		if (result.missingVars.Count > 0)
		{
			Log.Warning(
				$"{result.missingVars.Count} variable(s) referenced by the templates were not in the " +
				$"`{GithubEnvironment}` environment and rendered empty: {string.Join(", ", result.missingVars)}. " +
				"BeamableBackend/.github/environment/environment_core lists the full expected set.");
		}

		return result;
	}

	/// <summary>
	/// Substitutes every <c>{{ VAR }}</c> placeholder. An unknown name renders as an empty string (what the
	/// Python/Liquid original does) and is recorded in <paramref name="missing"/> so it can be reported rather
	/// than discovered later as a runtime failure against an empty bucket name.
	/// </summary>
	public static string Render(string template, IReadOnlyDictionary<string, string> variables, ISet<string> missing = null)
	{
		if (string.IsNullOrEmpty(template)) return template;

		return Placeholder.Replace(template, match =>
		{
			var name = match.Groups["name"].Value;
			if (variables != null && variables.TryGetValue(name, out var value))
				return value ?? string.Empty;

			missing?.Add(name);
			return string.Empty;
		});
	}

	/// <summary>Reads every page of the repo's <c>local</c> environment variables into a name → value map.</summary>
	private async Task<Dictionary<string, string>> FetchVariablesAsync(string repo, string token,
		CancellationToken cancellation)
	{
		var all = new Dictionary<string, string>(StringComparer.Ordinal);

		var first = await GetPageAsync(repo, token, page: 1, cancellation);
		Collect(first, all);

		var total = (int?)first["total_count"] ?? all.Count;
		var pages = (int)Math.Ceiling(total / (double)VarsPerPage);
		for (var page = 2; page <= pages; page++)
			Collect(await GetPageAsync(repo, token, page, cancellation), all);

		return all;

		static void Collect(JObject payload, Dictionary<string, string> into)
		{
			foreach (var entry in payload["variables"] ?? new JArray())
			{
				var name = (string)entry["name"];
				if (!string.IsNullOrEmpty(name))
					into[name] = (string)entry["value"] ?? string.Empty;
			}
		}
	}

	private async Task<JObject> GetPageAsync(string repo, string token, int page, CancellationToken cancellation)
	{
		var url = $"https://api.github.com/repos/{repo}/environments/{GithubEnvironment}/variables" +
		          $"?per_page={VarsPerPage}&page={page}";

		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
		request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
		request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
		// The GitHub API rejects requests without a User-Agent with a 403. Python's urllib supplies one by
		// default; HttpClient does not, so it has to be explicit or every call fails as "forbidden".
		request.Headers.TryAddWithoutValidation("User-Agent", "beam-cli-local-setup");

		using var response = await _http.SendAsync(request, cancellation);
		var body = await response.Content.ReadAsStringAsync(cancellation);
		if (!response.IsSuccessStatusCode)
		{
			var hint = response.StatusCode switch
			{
				System.Net.HttpStatusCode.Unauthorized => " — the token is invalid or expired.",
				System.Net.HttpStatusCode.NotFound =>
					$" — either the repo is wrong or the token cannot see its `{GithubEnvironment}` environment " +
					"(it needs read access to the repository's environment variables).",
				_ => string.Empty
			};

			throw new CliException($"GitHub returned {(int)response.StatusCode} {response.ReasonPhrase}{hint}");
		}

		return JObject.Parse(body);
	}

	/// <summary>
	/// The config files that are still missing from a checkout — used by <c>beam local validate</c> to report the
	/// gap without needing a token or a network call.
	/// </summary>
	public static List<string> MissingConfigFiles(string scalaDir)
	{
		var missing = new List<string>();
		if (string.IsNullOrWhiteSpace(scalaDir) || !Directory.Exists(scalaDir))
			return missing;

		foreach (var relative in RelativeConfPaths)
		{
			var path = Path.Combine(scalaDir, relative);
			if (!File.Exists(path))
				missing.Add(path);
		}

		return missing;
	}

	/// <summary>
	/// Reads a HOCON-ish <c>key = "value"</c> assignment out of a rendered conf file by its dotted path. Only
	/// used to recover the AWS role ARNs and bucket names for the preflight checks, so it deliberately does not
	/// attempt to be a HOCON parser: it walks the brace nesting and matches the requested path.
	/// </summary>
	public static string ReadConfValue(string confPath, string dottedKey)
	{
		if (!File.Exists(confPath) || string.IsNullOrWhiteSpace(dottedKey))
			return null;

		var wanted = dottedKey.Split('.', StringSplitOptions.RemoveEmptyEntries);
		var stack = new List<string>();

		foreach (var raw in File.ReadLines(confPath))
		{
			var line = StripComment(raw).Trim();
			if (line.Length == 0) continue;

			// `key {` opens a block; a bare `}` closes one.
			if (line.EndsWith("{", StringComparison.Ordinal))
			{
				stack.Add(line.Substring(0, line.Length - 1).Trim());
				continue;
			}

			if (line.StartsWith("}", StringComparison.Ordinal))
			{
				if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
				continue;
			}

			var equals = line.IndexOf('=');
			if (equals <= 0) continue;

			var key = line.Substring(0, equals).Trim();
			var path = stack.Concat(new[] { key }).Where(p => p.Length > 0).ToArray();
			if (!path.SequenceEqual(wanted, StringComparer.Ordinal)) continue;

			return line.Substring(equals + 1).Trim().Trim('"').Trim();
		}

		return null;

		static string StripComment(string line)
		{
			// Only strip comments outside of quotes — an ARN contains no '#', but a URL can contain '//'.
			var quoted = false;
			var builder = new StringBuilder();
			for (var i = 0; i < line.Length; i++)
			{
				var c = line[i];
				if (c == '"') quoted = !quoted;
				if (!quoted && (c == '#' || (c == '/' && i + 1 < line.Length && line[i + 1] == '/')))
					break;

				builder.Append(c);
			}

			return builder.ToString();
		}
	}
}
