using System;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.Sandbox;

/// <summary>
/// Verifies HMAC-SHA-256 signatures on sandbox requests. Signs the raw request bytes
/// as received on the wire (not a re-serialization of a parsed object) so Portal-side
/// and CLI-side JSON formatters can't silently diverge on key order or whitespace.
/// </summary>
public sealed class HmacVerifier
{
	private readonly IClock _clock;
	private readonly TimeSpan _window;

	public HmacVerifier(IClock clock, TimeSpan window)
	{
		_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		_window = window;
	}

	public HmacVerifyResult Verify(
		string routeName,
		ReadOnlySpan<byte> bodyBytes,
		long timestampUnixMs,
		string nonce,
		string sessionId,
		string headerSignatureBase64,
		byte[] sessionSecret)
	{
		if (routeName == null) return HmacVerifyResult.MalformedRequest;
		if (string.IsNullOrEmpty(nonce)) return HmacVerifyResult.MalformedRequest;
		if (string.IsNullOrEmpty(sessionId)) return HmacVerifyResult.MalformedRequest;
		if (string.IsNullOrEmpty(headerSignatureBase64)) return HmacVerifyResult.MalformedRequest;
		if (sessionSecret == null || sessionSecret.Length == 0) return HmacVerifyResult.MalformedRequest;

		var now = _clock.UnixTimeMs;
		var skew = Math.Abs(now - timestampUnixMs);
		if (skew > (long)_window.TotalMilliseconds) return HmacVerifyResult.StaleTimestamp;

		byte[] expected = ComputeSignature(routeName, bodyBytes, timestampUnixMs, nonce, sessionId, sessionSecret);

		byte[] provided;
		try { provided = Convert.FromBase64String(headerSignatureBase64); }
		catch (FormatException) { return HmacVerifyResult.MalformedRequest; }

		return CryptographicOperations.FixedTimeEquals(expected, provided)
			? HmacVerifyResult.Valid
			: HmacVerifyResult.SignatureMismatch;
	}

	public static byte[] ComputeSignature(
		string routeName,
		ReadOnlySpan<byte> bodyBytes,
		long timestampUnixMs,
		string nonce,
		string sessionId,
		byte[] sessionSecret)
	{
		// Layout:  routeName \n timestamp \n nonce \n sessionId \n bodyBytes
		// bodyBytes is appended raw (no re-encoding) so the signed bytes match exactly
		// what arrived on the wire.
		using var hmac = new HMACSHA256(sessionSecret);

		var preamble = $"{routeName}\n{timestampUnixMs}\n{nonce}\n{sessionId}\n";
		var preambleBytes = Encoding.UTF8.GetBytes(preamble);

		var buffer = new byte[preambleBytes.Length + bodyBytes.Length];
		preambleBytes.CopyTo(buffer, 0);
		bodyBytes.CopyTo(buffer.AsSpan(preambleBytes.Length));
		return hmac.ComputeHash(buffer);
	}
}

public enum HmacVerifyResult
{
	Valid,
	StaleTimestamp,
	SignatureMismatch,
	MalformedRequest,
}
