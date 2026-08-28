using Beamable.Platform.SDK.Auth;
using NUnit.Framework;

namespace Beamable.Tests.Runtime.Platform.Auth.GoogleSignInTests
{
	/// <summary>
	/// Pins the backwards-compatible behaviour of <see cref="GoogleSignIn.HandleResponse"/>, which is
	/// public API and is now implemented on top of <see cref="GoogleSignInResult.Parse"/>. Existing
	/// integrations must see exactly what they saw before for the pre-existing sentinels.
	/// </summary>
	public class GoogleSignInHandleResponseTests
	{
		private const string ID_TOKEN =
			"eyJhbGciOiJSUzI1NiIsImtpZCI6ImFiYzEyMyJ9.eyJpc3MiOiJhY2NvdW50cy5nb29nbGUuY29tIn0.c2lnbmF0dXJl";

		private string _token;
		private bool _callbackInvoked;
		private GoogleInvalidTokenException _error;

		[SetUp]
		public void SetUp()
		{
			_token = null;
			_callbackInvoked = false;
			_error = null;
		}

		private void Handle(string message) => GoogleSignIn.HandleResponse(
			message,
			token =>
			{
				_callbackInvoked = true;
				_token = token;
			},
			error => _error = error);

		[Test]
		public void HandleResponse_IdToken_InvokesCallbackWithToken()
		{
			Handle(ID_TOKEN);

			Assert.IsTrue(_callbackInvoked);
			Assert.AreEqual(ID_TOKEN, _token);
			Assert.IsNull(_error);
		}

		[Test]
		public void HandleResponse_Canceled_InvokesCallbackWithNullToken()
		{
			Handle("CANCELED");

			Assert.IsTrue(_callbackInvoked);
			Assert.IsNull(_token);
			Assert.IsNull(_error);
		}

		/// <summary>
		/// This signature cannot express "no credential", so a silent miss has to arrive as the same
		/// null token a cancellation does. Callers that need the distinction use
		/// <see cref="GoogleSignInResult"/>.
		/// </summary>
		[Test]
		public void HandleResponse_NoCredential_InvokesCallbackWithNullToken()
		{
			Handle("NO_CREDENTIAL - 4");

			Assert.IsTrue(_callbackInvoked);
			Assert.IsNull(_token);
			Assert.IsNull(_error);
		}

		[TestCase("EXCEPTION - something broke")]
		[TestCase("EXCEPTION something broke")]
		[TestCase("UNKNOWN")]
		public void HandleResponse_Failures_InvokeErrback(string message)
		{
			Handle(message);

			Assert.IsFalse(_callbackInvoked);
			Assert.IsNotNull(_error);

			// The exception carries the raw message, as it always has.
			Assert.AreEqual(message, _error.Message);
		}

		[TestCase(null)]
		[TestCase("")]
		public void HandleResponse_EmptyMessage_InvokesErrbackInsteadOfThrowing(string message)
		{
			Assert.DoesNotThrow(() => Handle(message));

			Assert.IsFalse(_callbackInvoked);
			Assert.IsNotNull(_error);
		}
	}
}
