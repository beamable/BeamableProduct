using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace cli.Services.Sandbox;

/// <summary>
/// Sandbox service-name helpers mirroring <c>BeamoLocalSystem.GetBeamIdAsPortalExtension</c> +
/// <c>PORTAL_EXTENSION_SERVICE_REGEX</c>. Sandbox names are tighter — only
/// <c>BeamSandbox_&lt;accountId&gt;_&lt;guid&gt;</c> is valid.
/// </summary>
public static class SandboxNaming
{
	public const string Prefix = "BeamSandbox";

	public static readonly Regex SandboxServiceRegex = new(
		@"^BeamSandbox_(?<accountId>\d+)_(?<guid>[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})$",
		RegexOptions.Compiled);

	public static string BuildServiceName(long accountId, Guid? guid = null)
		=> $"{Prefix}_{accountId}_{(guid ?? Guid.NewGuid())}";

	public static bool TryParse(string serviceName, out long accountId, out Guid guid)
	{
		accountId = 0;
		guid = Guid.Empty;
		if (string.IsNullOrEmpty(serviceName)) return false;
		var m = SandboxServiceRegex.Match(serviceName);
		if (!m.Success) return false;
		if (!long.TryParse(m.Groups["accountId"].Value, out accountId)) return false;
		if (!Guid.TryParse(m.Groups["guid"].Value, out guid)) return false;
		return true;
	}

	public const string JoinCodeAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

	private static readonly Regex JoinCodeRegex =
		new("^[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	/// <summary>
	/// Generates an 8-character join code from an unambiguous alphabet (no 0/O/1/I/L).
	/// Stable for the lifetime of a sandbox run; the rate limiter is what bounds brute force.
	/// </summary>
	public static string GenerateJoinCode()
	{
		Span<byte> randomBytes = stackalloc byte[8];
		RandomNumberGenerator.Fill(randomBytes);

		Span<char> chars = stackalloc char[9]; // 4-char-4-char with dash
		for (var i = 0; i < 4; i++) chars[i] = JoinCodeAlphabet[randomBytes[i] % JoinCodeAlphabet.Length];
		chars[4] = '-';
		for (var i = 0; i < 4; i++) chars[i + 5] = JoinCodeAlphabet[randomBytes[i + 4] % JoinCodeAlphabet.Length];
		return new string(chars);
	}

	/// <summary>
	/// Validates a caller-supplied join code (from <c>--join-code</c>) against the
	/// same format <see cref="GenerateJoinCode"/> emits. Case-insensitive so the
	/// CLI accepts lowercase pastes; callers should upper-case before persisting.
	/// </summary>
	public static bool IsValidJoinCodeFormat(string code)
	{
		if (string.IsNullOrEmpty(code)) return false;
		return JoinCodeRegex.IsMatch(code);
	}

	public static byte[] GenerateHmacSecret()
	{
		var secret = new byte[32];
		RandomNumberGenerator.Fill(secret);
		return secret;
	}
}
