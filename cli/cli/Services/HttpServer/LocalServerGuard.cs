using System.Security.Cryptography;
using System.Text;

namespace cli.Services.HttpServer;

/// <summary>
/// The checks every localhost API the CLI serves needs, because the commands behind them run with this CLI's
/// credentials: the request must address 127.0.0.1 by IP (a DNS-rebinding guard) and carry the run's session token.
/// </summary>
public static class LocalServerGuard
{
	public const string BEARER_PREFIX = "Bearer ";

	/// <summary>A fresh, URL-safe session secret.</summary>
	public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
		.TrimEnd('=').Replace('+', '-').Replace('/', '_');

	/// <summary>
	/// Whether <paramref name="host"/> (the request's <c>Host</c> header) names 127.0.0.1 on <paramref name="port"/>.
	/// A page that rebinds its own domain to 127.0.0.1 still sends its domain here.
	/// </summary>
	public static bool IsLoopbackHost(string host, int port) => host == $"127.0.0.1:{port}";

	/// <summary>Constant-time check that <paramref name="authorization"/> is <c>Bearer &lt;token&gt;</c>.</summary>
	public static bool HasBearerToken(string authorization, string token)
	{
		var presented = authorization != null && authorization.StartsWith(BEARER_PREFIX, StringComparison.Ordinal)
			? Encoding.UTF8.GetBytes(authorization.Substring(BEARER_PREFIX.Length))
			: Array.Empty<byte>();
		return CryptographicOperations.FixedTimeEquals(presented, Encoding.UTF8.GetBytes(token));
	}
}
