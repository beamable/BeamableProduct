namespace cli.Services.LocalStack;

/// <summary>
/// Reads the portal's gitignored <c>.env.local</c>. A stack gives the portal its API host as a process variable
/// (which Vite puts ahead of the file), so the file is never written: it only decides where a plain
/// <c>npm run dev</c> goes. <c>API_BASE</c> in the portal's auth store falls back to
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
}
