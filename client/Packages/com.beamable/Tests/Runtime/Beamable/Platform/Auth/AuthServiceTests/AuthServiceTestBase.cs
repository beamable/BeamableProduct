using Beamable.Api.Auth;
using Beamable.Common.Api.Auth;
using NUnit.Framework;

namespace BeamableEditor.Tests.Platform.Auth.AuthServiceTests
{
	public class AuthServiceTestBase
	{
		public const string ROUTE = "/basic/accounts";
		public const string TOKEN_URL = "/basic/auth/token";

		protected MockPlatformAPI _requester;
		protected AuthService _service;
		protected User _sampleUser;

		[SetUp]
		public void Init()
		{
			_requester = new MockPlatformAPI();
			_sampleUser = new User();
			_service = new AuthService(_requester);
		}

		[TearDown]
		public void Cleanup()
		{

		}

	}

}
