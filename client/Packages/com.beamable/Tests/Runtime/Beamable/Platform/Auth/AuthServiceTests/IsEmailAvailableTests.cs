using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

namespace Beamable.Platform.Tests.Auth.AuthServiceTests
{
	public class IsEmailAvailableTests : AuthServiceTestBase
	{
		[UnityTest]
		public IEnumerator MakesWebCall()
		{
			var email = "test+ext@test.com";

			_requester.MockRequest<AvailabilityResponse>(Method.GET, $"{ROUTE}/available?email=test%2bext%40test.com")
			   .WithNoAuthHeader()
			   .WithResponse(new AvailabilityResponse() { available = true });

			yield return _service.GetCredentialStatus(email).AsYield();

			Assert.AreEqual(true, _requester.AllMocksCalled);
		}

		[UnityTest]
		public IEnumerator HandleErrorCase()
		{
			var email = "test+ext@test.com";
			var exception = new RequesterException("", "", "", 400, "{}");

			_requester.MockRequest<AvailabilityResponse>(Method.GET, $"{ROUTE}/available?email=test%2bext%40test.com")
			          .WithNoAuthHeader()
			          .WithResponse(exception);

			var res = _service.GetCredentialStatus(email);

			yield return res.AsYield();

			Assert.AreEqual(CredentialUsageStatus.INVALID_CREDENTIAL, res.GetResult());
		}

		[UnityTest]
		public IEnumerator HandleAlreadyAssignedEmail()
		{
			var email = "test+ext@test.com";

			_requester.MockRequest<AvailabilityResponse>(Method.GET, $"{ROUTE}/available?email=test%2bext%40test.com")
			          .WithNoAuthHeader()
			          .WithResponse(new AvailabilityResponse() { available = false });

			var res = _service.GetCredentialStatus(email);
			yield return _service.GetCredentialStatus(email).AsYield();

			Assert.AreEqual(CredentialUsageStatus.ASSIGNED_TO_AN_ACCOUNT, res.GetResult());
		}

	}
}
