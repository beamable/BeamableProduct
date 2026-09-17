using Beamable.Platform.SDK.Auth;
using NUnit.Framework;

namespace Beamable.Tests.Runtime.Platform.Auth.GoogleSignInTests
{
	/// <summary>
	/// Covers <see cref="GoogleSignInResult.Parse"/>, the whole response vocabulary of the native
	/// Google Sign-In plugins. The parser is deliberately free of UnityEngine dependencies so that
	/// this wire contract can be pinned by plain tests rather than only on a device.
	/// </summary>
	public class GoogleSignInResponseParserTests
	{
		/// <summary>A realistically shaped Google ID token: a base64url JWT, so it starts with "eyJ".</summary>
		private const string ID_TOKEN =
			"eyJhbGciOiJSUzI1NiIsImtpZCI6ImFiYzEyMyJ9.eyJpc3MiOiJhY2NvdW50cy5nb29nbGUuY29tIn0.c2lnbmF0dXJl";

		[Test]
		public void Parse_IdToken_IsSuccess()
		{
			var result = GoogleSignInResult.Parse(ID_TOKEN);

			Assert.AreEqual(GoogleSignInStatus.Success, result.Status);
			Assert.IsTrue(result.IsSuccess);
			Assert.IsTrue(result.HasIdToken);
			Assert.AreEqual(ID_TOKEN, result.IdToken);
		}

		[Test]
		public void Parse_Canceled_IsCancelled()
		{
			var result = GoogleSignInResult.Parse("CANCELED");

			Assert.AreEqual(GoogleSignInStatus.Cancelled, result.Status);
			Assert.IsFalse(result.HasIdToken);
			Assert.IsTrue(result.IsBenign);
		}

		[Test]
		public void Parse_NoCredential_IsNoCredential()
		{
			var result = GoogleSignInResult.Parse("NO_CREDENTIAL");

			Assert.AreEqual(GoogleSignInStatus.NoCredential, result.Status);
			Assert.IsTrue(result.IsBenign);
			Assert.IsFalse(result.HasIdToken);
		}

		[Test]
		public void Parse_NoCredentialWithStatusCode_KeepsCodeAsDetail()
		{
			var result = GoogleSignInResult.Parse("NO_CREDENTIAL - 4");

			Assert.AreEqual(GoogleSignInStatus.NoCredential, result.Status);
			Assert.AreEqual("4", result.Detail);
		}

		/// <summary>
		/// The Java side emits NO_CREDENTIAL, but SIGN_IN_REQUIRED is the name of the underlying
		/// Google status code and is accepted as an alias, so a rename there cannot break this SDK.
		/// </summary>
		[Test]
		public void Parse_SignInRequiredAlias_IsNoCredential()
		{
			var result = GoogleSignInResult.Parse("SIGN_IN_REQUIRED");

			Assert.AreEqual(GoogleSignInStatus.NoCredential, result.Status);
		}

		[Test]
		public void Parse_Unknown_IsError()
		{
			var result = GoogleSignInResult.Parse("UNKNOWN");

			Assert.AreEqual(GoogleSignInStatus.Error, result.Status);
			Assert.IsFalse(result.IsBenign);
			Assert.IsNotEmpty(result.Detail);
		}

		/// <summary>Android's format: "EXCEPTION - detail".</summary>
		[Test]
		public void Parse_AndroidException_StripsPrefixAndDash()
		{
			var result = GoogleSignInResult.Parse("EXCEPTION - DEVELOPER_ERROR(10)");

			Assert.AreEqual(GoogleSignInStatus.Error, result.Status);
			Assert.AreEqual("DEVELOPER_ERROR(10)", result.Detail);
		}

		/// <summary>iOS's format: "EXCEPTION detail", with no dash. Must yield the same detail.</summary>
		[Test]
		public void Parse_IosException_StripsPrefixWithoutDash()
		{
			var result = GoogleSignInResult.Parse("EXCEPTION The user canceled the sign-in flow.");

			Assert.AreEqual(GoogleSignInStatus.Error, result.Status);
			Assert.AreEqual("The user canceled the sign-in flow.", result.Detail);
		}

		[Test]
		public void Parse_BareException_StillHasDetail()
		{
			var result = GoogleSignInResult.Parse("EXCEPTION");

			Assert.AreEqual(GoogleSignInStatus.Error, result.Status);
			Assert.IsNotEmpty(result.Detail);
		}

		[Test]
		public void Parse_Unavailable_IsUnavailable()
		{
			var result = GoogleSignInResult.Parse("UNAVAILABLE - no response within 30s");

			Assert.AreEqual(GoogleSignInStatus.Unavailable, result.Status);
			Assert.AreEqual("no response within 30s", result.Detail);
			Assert.IsTrue(result.IsBenign);
		}

		[Test]
		public void Parse_SignOutAcknowledgements_AreSuccessWithoutToken()
		{
			foreach (var message in new[] {"SIGNED_OUT", "REVOKED"})
			{
				var result = GoogleSignInResult.Parse(message);

				Assert.AreEqual(GoogleSignInStatus.Success, result.Status, message);
				Assert.IsTrue(result.IsSuccess, message);
				Assert.IsFalse(result.HasIdToken, message);
			}
		}

		[TestCase(null)]
		[TestCase("")]
		public void Parse_EmptyResponse_IsErrorAndDoesNotThrow(string message)
		{
			// The previous implementation called StartsWith on this and threw a
			// NullReferenceException.
			var result = GoogleSignInResult.Parse(message);

			Assert.AreEqual(GoogleSignInStatus.Error, result.Status);
			Assert.IsNotEmpty(result.Detail);
		}

		/// <summary>
		/// A sentinel only matches when followed by end of string, a space or a dash. Without that
		/// check, a bare prefix match would misclassify a token that merely started with the same
		/// letters. Real ID tokens start with a lowercase "eyJ" so this cannot happen in practice, but
		/// the guard is what makes that reasoning safe rather than lucky.
		/// </summary>
		[TestCase("UNKNOWNTOKENVALUE")]
		[TestCase("CANCELEDX")]
		[TestCase("NO_CREDENTIALS")]
		public void Parse_SentinelPrefixWithoutSeparator_IsTreatedAsToken(string message)
		{
			var result = GoogleSignInResult.Parse(message);

			Assert.AreEqual(GoogleSignInStatus.Success, result.Status);
			Assert.AreEqual(message, result.IdToken);
		}
	}
}
