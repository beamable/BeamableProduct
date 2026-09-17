using cli;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.IO;
using tests.Examples;

namespace tests.Examples.Auth;

/// <summary>
/// Regression coverage for the token-resolution path in <see cref="cli.DefaultAppContext"/>.
/// A token supplied explicitly with <c>--access-token</c> must win over the stored login, even when
/// that stored login is expired. Previously the expired-stored-token refresh ran unconditionally and
/// silently clobbered the override, so requests went out as the stored user instead of the one whose
/// token was passed in.
/// </summary>
[NonParallelizable]
public class AccessTokenOverrideTests : CLITest
{
	private const string Cid = "123";
	private const string Pid = "456";
	private const string StoredAccessToken = "stored-access-token";
	private const string StoredRefreshToken = "stored-refresh-token";

	private static string AuthFilePath => Path.Combine(".beamable", "temp", "auth.beam.json");

	/// <summary>
	/// Lays down a valid workspace whose stored login is already expired, so the expired-token
	/// refresh path is exercised.
	/// </summary>
	private void WriteWorkspaceWithExpiredStoredToken()
	{
		Directory.CreateDirectory(Path.Combine(".beamable", "temp"));

		File.WriteAllText(Path.Combine(".beamable", "config.beam.json"),
			$$"""
			{
			  "additionalProjectPaths" : [ ],
			  "ignoredProjectPaths" : [ ],
			  "host" : "https://api.beamable.com",
			  "cid" : "{{Cid}}",
			  "pid" : "{{Pid}}",
			  "cliVersion" : "0.0.123"
			}
			""");

		File.WriteAllText(AuthFilePath,
			$$"""
			{
			  "cid" : "{{Cid}}",
			  "pid" : "{{Pid}}",
			  "access_token" : "{{StoredAccessToken}}",
			  "refresh_token" : "{{StoredRefreshToken}}",
			  "expires_at" : "2000-01-01T00:00:00",
			  "expires_in" : 0,
			  "issued_at" : "2000-01-01T00:00:00"
			}
			""");
	}

	private string ReadStoredAccessToken() =>
		JToken.Parse(File.ReadAllText(AuthFilePath))["access_token"]!.Value<string>();

	[Test]
	public void AccessTokenOverride_SurvivesExpiredStoredToken()
	{
		WriteWorkspaceWithExpiredStoredToken();

		const string overrideToken = "super-admin-override-token";
		RunFull(new[] { "config", "-q", "--access-token", overrideToken });

		// The expired stored token must NOT be refreshed when an explicit override is supplied...
		_mockAuth.Verify(x => x.LoginRefreshToken(It.IsAny<string>()), Times.Never);

		// ...and the override must not have been overwritten onto (or discarded in favor of) the
		// stored login. The stored auth file is left untouched.
		Assert.AreEqual(StoredAccessToken, ReadStoredAccessToken(),
			"Passing --access-token must not trigger a refresh/save of the stored login.");
	}

	[Test]
	public void ExpiredStoredToken_WithoutOverride_IsRefreshed()
	{
		// Control: without an override, the expired stored token IS refreshed and persisted. This
		// proves the setup genuinely exercises the expired-token path that the fix guards.
		WriteWorkspaceWithExpiredStoredToken();

		RunFull(new[] { "config", "-q" });

		_mockAuth.Verify(x => x.LoginRefreshToken(StoredRefreshToken), Times.Once);

		// The default mock refresh (see CLITest) mints access_token = "access", which is saved back.
		Assert.AreEqual("access", ReadStoredAccessToken(),
			"Without an override, the expired stored token should be refreshed and saved.");
	}
}
