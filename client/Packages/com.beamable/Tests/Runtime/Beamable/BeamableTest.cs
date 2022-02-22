using Beamable;
using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using BeamableEditor.Tests.Platform;
using NUnit.Framework;
using BeamableEditor.Tests.Runtime.Api;

namespace BeamableEditor.Tests.Runtime
{
	/// <summary>
	/// A base class for Beamable Tests that go through the boilerplate of setting up the mock beamable instance, requester, and user.
	/// </summary>
	public class BeamableTest
	{
		protected MockPlatformAPI MockRequester;
		protected User MockPlatformUser;
		protected MockBeamableApi MockApi;
		protected MockPlatformService MockPlatform;


		[SetUp]
		public void SetupBeamable()
		{
			MockApi = new MockBeamableApi();
			MockPlatform = new MockPlatformService();
			MockPlatformUser = new User { id = 12 };
			MockPlatform.User = MockPlatformUser;
			MockApi.User = MockPlatform.User;
			MockApi.Token = new AccessToken(null, "testcid", "testpid", "testtoken", "refresh", 0);
			MockRequester = new MockPlatformAPI();
			MockRequester.Token = MockApi.Token;
			MockApi.Requester = MockRequester;
			API.Instance = Promise<IBeamableAPI>.Successful(MockApi);
			OnSetupBeamable();
		}

		protected virtual void OnSetupBeamable()
		{
			// maybe do something to the beamable instance?
		}
	}
}
