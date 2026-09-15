using Beamable.AccountManagement;
using NUnit.Framework;

namespace Beamable.Tests.Runtime.Modules.AccountManagement
{
	public class ThirdPartyLoginResponseTests
	{
		[Test]
		public void NoCredentialFound_IsCancelledSoTheLoginFlowNoOps()
		{
			var response = ThirdPartyLoginResponse.NoCredentialFound();

			// ThirdPartyLogin() short-circuits on Cancelled, which is what makes "nobody has granted
			// an account on this device" a no-op rather than an error path.
			Assert.IsTrue(response.Cancelled);
			Assert.IsTrue(response.NoCredential);
			Assert.IsNull(response.AuthToken);
		}

		[Test]
		public void Cancel_IsCancelledButNotNoCredential()
		{
			var response = ThirdPartyLoginResponse.Cancel();

			Assert.IsTrue(response.Cancelled);
			Assert.IsFalse(response.NoCredential);
			Assert.IsNull(response.AuthToken);
		}

		/// <summary>
		/// The factories must hand back a new instance every time. The legacy
		/// <see cref="ThirdPartyLoginResponse.CANCELLED"/> singleton has a public, writable
		/// <c>AuthToken</c>, so anything that assigns to it corrupts every later cancellation
		/// process-wide - which is why new code uses these factories instead.
		/// </summary>
		[Test]
		public void Factories_ReturnFreshInstances()
		{
			Assert.AreNotSame(ThirdPartyLoginResponse.Cancel(), ThirdPartyLoginResponse.Cancel());
			Assert.AreNotSame(ThirdPartyLoginResponse.NoCredentialFound(),
							  ThirdPartyLoginResponse.NoCredentialFound());
			Assert.AreNotSame(ThirdPartyLoginResponse.CANCELLED, ThirdPartyLoginResponse.Cancel());
		}
	}
}
