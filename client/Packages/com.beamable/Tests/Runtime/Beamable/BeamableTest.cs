using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Platform.Tests;
using Beamable.Tests.Runtime.Api;
using NUnit.Framework;

namespace Beamable.Tests.Runtime
{
	/// <summary>
	/// A base class for Beamable Tests that go through the boilerplate of setting up the mock beamable instance, requester, and user.
	/// </summary>
	public class BeamableTest
	{
		public const string DEFAULT_PID = "testPid";
		public const string DEFAULT_CID = "123";
		
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
			MockApi.Token = new AccessToken(null, DEFAULT_CID, DEFAULT_PID, "testtoken", "refresh", 0);
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
