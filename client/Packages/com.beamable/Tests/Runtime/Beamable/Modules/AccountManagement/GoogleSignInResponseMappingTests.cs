using Beamable.AccountManagement;
using Beamable.Common.Api.Auth;
using Beamable.Platform.SDK.Auth;
using NUnit.Framework;
using System;

namespace Beamable.Tests.Runtime.Modules.AccountManagement
{
	/// <summary>
	/// Pins the Google-result-to-login-flow mapping. This matrix decides whether a Beamable account
	/// switch is offered, so a regression here is a wrong-account bug rather than a cosmetic one.
	/// </summary>
	public class GoogleSignInResponseMappingTests
	{
		/// <summary>
		/// Shaped like a real ID token - a base64url JWT - so nothing in the chain could mistake it for
		/// one of the plugins' sentinel responses.
		/// </summary>
		private const string ID_TOKEN =
			"eyJhbGciOiJSUzI1NiIsImtpZCI6ImFiYzEyMyJ9.eyJpc3MiOiJhY2NvdW50cy5nb29nbGUuY29tIn0.c2ln";

		private static ThirdPartyLoginPromise Promise() =>
			new ThirdPartyLoginPromise(AuthThirdParty.Google);

		[Test]
		public void Success_CompletesWithTheIdToken()
		{
			var promise = Promise();

			GoogleSignInResponseMapping.Apply(promise, GoogleSignInResult.Success(ID_TOKEN));

			Assert.IsTrue(promise.IsCompleted);
			Assert.AreEqual(ID_TOKEN, promise.GetResult().AuthToken);
			Assert.IsFalse(promise.GetResult().Cancelled);
		}

		/// <summary>
		/// Every no-token outcome except Error resolves the promise rather than failing it, so the flow
		/// treats it as a no-op and the player is never interrupted.
		/// </summary>
		[Test]
		public void BenignFailures_ResolveThePromiseWithNoToken(
			[ValueSource(nameof(NoTokenResults))] GoogleSignInResult result)
		{
			var promise = Promise();

			GoogleSignInResponseMapping.Apply(promise, result);

			Assert.IsFalse(promise.IsFailed);
			Assert.IsTrue(promise.GetResult().Cancelled);
			Assert.IsNull(promise.GetResult().AuthToken);
		}

		private static GoogleSignInResult[] NoTokenResults() => new[]
		{
			GoogleSignInResult.Cancelled(),
			GoogleSignInResult.NoCredential(),
			GoogleSignInResult.Unavailable("no plugin"),
			GoogleSignInResult.Busy("in flight")
		};

		[Test]
		public void Cancelled_IsAPlainCancellation()
		{
			var promise = Promise();

			GoogleSignInResponseMapping.Apply(promise, GoogleSignInResult.Cancelled());

			Assert.IsTrue(promise.GetResult().Cancelled);
			Assert.IsFalse(promise.GetResult().NoCredential);
		}

		/// <summary>
		/// Unavailable is what the Editor and every unsupported platform produce. It must resolve the
		/// promise rather than leave it pending, which is what used to wedge the loading overlay.
		/// </summary>
		[Test]
		public void Unavailable_IsACancellation()
		{
			var promise = Promise();

			GoogleSignInResponseMapping.Apply(promise, GoogleSignInResult.Unavailable("editor"));

			Assert.IsFalse(promise.IsFailed);
			Assert.IsTrue(promise.GetResult().Cancelled);
			Assert.IsFalse(promise.GetResult().NoCredential);
		}

		/// <summary>
		/// Busy never comes out of the response parser - only out of
		/// <see cref="GoogleSignInService.SignIn"/> when a request is already in flight - so it needs
		/// its own case, or it would fall through to the Error branch and raise a spurious error.
		/// </summary>
		[Test]
		public void Busy_IsACancellationSoTheIncumbentRequestStaysAuthoritative()
		{
			var promise = Promise();

			GoogleSignInResponseMapping.Apply(promise, GoogleSignInResult.Busy("in flight"));

			Assert.IsFalse(promise.IsFailed);
			Assert.IsTrue(promise.GetResult().Cancelled);
		}

		/// <summary>
		/// NoCredential is the outcome that tells a game "nobody has granted an account on this device",
		/// which is actionable - show the Google button - rather than an error to report.
		/// </summary>
		[Test]
		public void NoCredential_IsReportedAsNoCredentialNotAnError()
		{
			var promise = Promise();

			GoogleSignInResponseMapping.Apply(promise, GoogleSignInResult.NoCredential());

			Assert.IsFalse(promise.IsFailed);
			Assert.IsTrue(promise.GetResult().NoCredential);
		}

		[Test]
		public void Error_FailsThePromise()
		{
			var promise = Promise();
			Exception captured = null;
			promise.Error(ex => captured = ex);

			GoogleSignInResponseMapping.Apply(promise, GoogleSignInResult.Error("boom"));

			Assert.IsTrue(promise.IsFailed);
			Assert.IsInstanceOf<GoogleInvalidTokenException>(captured);
			Assert.IsTrue(captured.Message.Contains("boom"));
		}

		/// <summary>
		/// The property that makes refusing an overlapping attempt safe, and the regression test for
		/// "overlapping requests can apply the wrong Google token": a refused attempt is settled the
		/// moment it is refused, and the native response that arrives afterwards - possibly carrying a
		/// different Google account's ID token - cannot overwrite it.
		/// </summary>
		[Test]
		public void Apply_DoesNotOverwriteAnAlreadyResolvedPromise()
		{
			var promise = Promise();
			promise.CompleteSuccess(ThirdPartyLoginResponse.NoCredentialFound());

			GoogleSignInResponseMapping.Apply(promise, GoogleSignInResult.Success(ID_TOKEN));

			Assert.IsNull(promise.GetResult().AuthToken);
			Assert.IsTrue(promise.GetResult().NoCredential);
		}
	}
}
