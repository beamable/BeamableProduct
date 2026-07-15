using System;
using System.Security.Cryptography;
using System.Text;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class HmacVerifierTests
{
	private FakeClock _clock = null!;
	private HmacVerifier _verifier = null!;
	private byte[] _secret = null!;

	[SetUp]
	public void SetUp()
	{
		_clock = new FakeClock(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000));
		_verifier = new HmacVerifier(_clock, TimeSpan.FromMinutes(15));
		_secret = new byte[32];
		RandomNumberGenerator.Fill(_secret);
	}

	private string SignBase64(string route, byte[] body, long ts, string nonce, string sessionId, byte[] secret)
	{
		var sig = HmacVerifier.ComputeSignature(route, body, ts, nonce, sessionId, secret);
		return Convert.ToBase64String(sig);
	}

	[Test]
	public void ValidSignature_AcceptedAtCurrentTime()
	{
		var body = Encoding.UTF8.GetBytes("{\"path\":\"foo.cs\"}");
		var sig = SignBase64("WriteFile", body, _clock.UnixTimeMs, "nonce-1", "session-1", _secret);
		var result = _verifier.Verify("WriteFile", body, _clock.UnixTimeMs, "nonce-1", "session-1", sig, _secret);
		Assert.That(result, Is.EqualTo(HmacVerifyResult.Valid));
	}

	[Test]
	public void RawBytes_AreSigned_NotParsedObject()
	{
		// Two JSON encodings of the same logical body — different bytes, different signatures.
		var bodyA = Encoding.UTF8.GetBytes("{\"a\":1,\"b\":2}");
		var bodyB = Encoding.UTF8.GetBytes("{\"b\":2,\"a\":1}");
		var sigA = SignBase64("WriteFile", bodyA, _clock.UnixTimeMs, "nonce", "session", _secret);

		// Sign bytes A but send bytes B → must reject. This proves the verifier signs the bytes
		// it received, not a canonicalization of a parsed object.
		var result = _verifier.Verify("WriteFile", bodyB, _clock.UnixTimeMs, "nonce", "session", sigA, _secret);
		Assert.That(result, Is.EqualTo(HmacVerifyResult.SignatureMismatch));
	}

	[Test]
	public void StaleTimestamp_PastEdge_Rejected()
	{
		var body = Encoding.UTF8.GetBytes("{}");
		var staleTs = _clock.UnixTimeMs - (long)TimeSpan.FromMinutes(15).TotalMilliseconds - 1;
		var sig = SignBase64("Info", body, staleTs, "n", "s", _secret);
		var result = _verifier.Verify("Info", body, staleTs, "n", "s", sig, _secret);
		Assert.That(result, Is.EqualTo(HmacVerifyResult.StaleTimestamp));
	}

	[Test]
	public void StaleTimestamp_FutureEdge_Rejected()
	{
		// Defend against an attacker who reuses a signature from before the rate-limited window
		// shifts; far-future timestamps should also reject.
		var body = Encoding.UTF8.GetBytes("{}");
		var futureTs = _clock.UnixTimeMs + (long)TimeSpan.FromMinutes(15).TotalMilliseconds + 1;
		var sig = SignBase64("Info", body, futureTs, "n", "s", _secret);
		var result = _verifier.Verify("Info", body, futureTs, "n", "s", sig, _secret);
		Assert.That(result, Is.EqualTo(HmacVerifyResult.StaleTimestamp));
	}

	[Test]
	public void TimestampJustInsideWindow_Accepted()
	{
		var body = Encoding.UTF8.GetBytes("{}");
		var ts = _clock.UnixTimeMs - (long)TimeSpan.FromMinutes(14).TotalMilliseconds;
		var sig = SignBase64("Info", body, ts, "n", "s", _secret);
		Assert.That(_verifier.Verify("Info", body, ts, "n", "s", sig, _secret),
			Is.EqualTo(HmacVerifyResult.Valid));
	}

	[Test]
	public void TamperedBodyByte_Rejected()
	{
		var body = Encoding.UTF8.GetBytes("hello world");
		var sig = SignBase64("WriteFile", body, _clock.UnixTimeMs, "n", "s", _secret);

		var tampered = (byte[])body.Clone();
		tampered[0] ^= 0x01;

		var result = _verifier.Verify("WriteFile", tampered, _clock.UnixTimeMs, "n", "s", sig, _secret);
		Assert.That(result, Is.EqualTo(HmacVerifyResult.SignatureMismatch));
	}

	[Test]
	public void WrongSessionId_Rejected()
	{
		var body = Encoding.UTF8.GetBytes("{}");
		var sig = SignBase64("Info", body, _clock.UnixTimeMs, "n", "session-A", _secret);
		var result = _verifier.Verify("Info", body, _clock.UnixTimeMs, "n", "session-B", sig, _secret);
		Assert.That(result, Is.EqualTo(HmacVerifyResult.SignatureMismatch));
	}

	[Test]
	public void WrongRoute_Rejected()
	{
		var body = Encoding.UTF8.GetBytes("{}");
		var sig = SignBase64("Info", body, _clock.UnixTimeMs, "n", "s", _secret);
		var result = _verifier.Verify("WriteFile", body, _clock.UnixTimeMs, "n", "s", sig, _secret);
		Assert.That(result, Is.EqualTo(HmacVerifyResult.SignatureMismatch));
	}

	[Test]
	public void MalformedInputs_Rejected()
	{
		var body = Encoding.UTF8.GetBytes("{}");
		var anySig = SignBase64("Info", body, _clock.UnixTimeMs, "n", "s", _secret);

		Assert.That(_verifier.Verify("Info", body, _clock.UnixTimeMs, "", "s", anySig, _secret),
			Is.EqualTo(HmacVerifyResult.MalformedRequest));
		Assert.That(_verifier.Verify("Info", body, _clock.UnixTimeMs, "n", "", anySig, _secret),
			Is.EqualTo(HmacVerifyResult.MalformedRequest));
		Assert.That(_verifier.Verify("Info", body, _clock.UnixTimeMs, "n", "s", "not-base64!!", _secret),
			Is.EqualTo(HmacVerifyResult.MalformedRequest));
		Assert.That(_verifier.Verify("Info", body, _clock.UnixTimeMs, "n", "s", anySig, Array.Empty<byte>()),
			Is.EqualTo(HmacVerifyResult.MalformedRequest));
	}
}

internal sealed class FakeClock : IClock
{
	public DateTimeOffset UtcNow { get; set; }
	public long UnixTimeMs => UtcNow.ToUnixTimeMilliseconds();

	public FakeClock(DateTimeOffset start) => UtcNow = start;

	public void Advance(TimeSpan delta) => UtcNow = UtcNow + delta;
}
