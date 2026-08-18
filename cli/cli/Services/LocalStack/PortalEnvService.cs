using System.Text;

namespace cli.Services.LocalStack;

/// <summary>What one <see cref="PortalEnvService"/> pass did.</summary>
public class PortalEnvResult
{
	/// <summary>The <c>.env.local</c> that was inspected.</summary>
	public string path;

	/// <summary>One of <c>created</c>, <c>added-key</c>, <c>kept</c>, <c>rewritten</c>.</summary>
	public string action;

	/// <summary>The value <c>VITE_API_BASE</c> holds after this pass.</summary>
	public string apiBase;

	/// <summary>Set when nothing could be done.</summary>
	public string error;

	public bool ok => error == null;
}

/// <summary>
/// Ensures the portal's <c>.env.local</c> points at the local backend.
///
/// This exists for the same reason <see cref="ScalaLocalVarsService"/> does: the file is <b>gitignored</b>, so it
/// is missing from every fresh clone or copy of the portal — and its absence fails in a way that looks like a
/// broken backend rather than missing config. <c>API_BASE</c> in the portal's auth store falls back to
/// <c>https://api.beamable.com</c> when <c>VITE_API_BASE</c> is unset, so a portal served from localhost sends
/// login requests to <b>production</b>. The local seed account then "does not exist", and the stack looks broken
/// while every service is healthy.
///
/// The check is on the <em>key</em>, not the file: a <c>.env.local</c> that exists for some other setting but has
/// no <c>VITE_API_BASE</c> still silently points the portal at production.
/// </summary>
public class PortalEnvService
{
	public const string EnvFileName = ".env.local";

	/// <summary>The variable the portal reads to decide which backend to talk to.</summary>
	public const string ApiBaseKey = "VITE_API_BASE";

	/// <summary>The value the portal falls back to when <see cref="ApiBaseKey"/> is unset.</summary>
	public const string ProductionApiBase = "https://api.beamable.com";

	private const string Header = "# Local overrides — not committed to git.";

	public static string ResolvePath(string portalDir) =>
		string.IsNullOrWhiteSpace(portalDir) ? null : Path.Combine(portalDir, EnvFileName);

	/// <summary>
	/// The <see cref="ApiBaseKey"/> currently set in the portal's <c>.env.local</c>, or null when the file or the
	/// key is absent (both of which mean "the portal will use production").
	/// </summary>
	public static string ReadApiBase(string portalDir)
	{
		var path = ResolvePath(portalDir);
		if (path == null || !File.Exists(path)) return null;

		foreach (var raw in File.ReadLines(path))
		{
			var line = raw.Trim();
			if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

			var equals = line.IndexOf('=');
			if (equals <= 0) continue;

			if (line.Substring(0, equals).Trim().Equals(ApiBaseKey, StringComparison.Ordinal))
			{
				var value = line.Substring(equals + 1).Trim().Trim('"', '\'');
				return string.IsNullOrWhiteSpace(value) ? null : value;
			}
		}

		return null;
	}

	/// <summary>
	/// True when the portal in <paramref name="portalDir"/> would talk to production rather than
	/// <paramref name="expectedHost"/> — either because the key is missing or because it names a different host.
	/// </summary>
	public static bool PointsAwayFromLocalBackend(string portalDir, string expectedHost)
	{
		var current = ReadApiBase(portalDir);
		if (current == null) return true;
		if (string.IsNullOrWhiteSpace(expectedHost)) return false;

		return !string.Equals(current.TrimEnd('/'), expectedHost.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Makes <c>.env.local</c> point <see cref="ApiBaseKey"/> at <paramref name="host"/>.
	///
	/// Other lines are preserved: this file is where a developer keeps their own overrides
	/// (<c>VITE_WINGMAN_URL</c>, <c>VITE_INJECT_HOST_SDK</c>, Datadog keys), so it is edited surgically rather
	/// than regenerated. An existing, already-correct value is left alone unless <paramref name="force"/> is set.
	/// </summary>
	public PortalEnvResult Ensure(string portalDir, string host, bool force)
	{
		var result = new PortalEnvResult { path = ResolvePath(portalDir) };

		if (string.IsNullOrWhiteSpace(portalDir) || !Directory.Exists(portalDir))
		{
			result.error = $"Portal checkout not found at '{portalDir}'. " +
			               "Pass --portal-dir (or fix `repos.portalDir` in the manifest).";
			return result;
		}

		if (string.IsNullOrWhiteSpace(host))
		{
			result.error = "No backend host to point the portal at — the manifest has no `host`.";
			return result;
		}

		var line = $"{ApiBaseKey}={host.TrimEnd('/')}";

		if (!File.Exists(result.path))
		{
			File.WriteAllText(result.path, Header + Environment.NewLine + line + Environment.NewLine);
			result.action = "created";
			result.apiBase = host;
			return result;
		}

		var lines = File.ReadAllLines(result.path).ToList();
		var index = lines.FindIndex(l =>
		{
			var trimmed = l.TrimStart();
			return !trimmed.StartsWith("#", StringComparison.Ordinal)
			       && trimmed.StartsWith(ApiBaseKey + "=", StringComparison.Ordinal);
		});

		if (index < 0)
		{
			// The file exists for other settings but has no VITE_API_BASE — the silent-production case. Append it
			// rather than rewriting the file, so the developer's other overrides survive untouched.
			lines.Add(line);
			File.WriteAllLines(result.path, lines);
			result.action = "added-key";
			result.apiBase = host;
			return result;
		}

		var existing = ReadApiBase(portalDir);
		if (!force && string.Equals(existing?.TrimEnd('/'), host.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
		{
			result.action = "kept";
			result.apiBase = existing;
			return result;
		}

		if (!force)
		{
			// It names a DIFFERENT host (a developer pointing at dev/staging on purpose). Report it instead of
			// silently retargeting their portal; --force is the way to overwrite.
			result.action = "kept";
			result.apiBase = existing;
			return result;
		}

		lines[index] = line;
		File.WriteAllLines(result.path, lines);
		result.action = "rewritten";
		result.apiBase = host;
		return result;
	}
}
