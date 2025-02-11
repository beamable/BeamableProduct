using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

namespace Beamable.Tests.Runtime.Player.Accounts
{
	public class ExternalIdentityQueryArgsTests : BeamContextTest
	{
		[UnityTest]
		public IEnumerator QueryArgs_IsExternalIdentityAvailable()
		{
			var provider = "PhoneAuthFederation";
			var token = "+18574882877";
			var providerNamespace = "PhoneNumber";

			var expectedUrl =
				"/basic/accounts/available/external_identity?provider_service=PhoneAuthFederation&user_id=%2b18574882877&provider_namespace=PhoneNumber";

			TriggerContextInit();
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);

			Requester.MockRequest<AvailabilityResponse>(Method.GET,
														expectedUrl
			).WithNoAuthHeader()
					 .WithResponse(new AvailabilityResponse
					 {
						 available = true
					 });
			var api = Context.Api.AuthService;

			var req = api.IsExternalIdentityAvailable(provider, token, providerNamespace);
			yield return req.ToYielder();
			Assert.IsTrue(req.GetResult());
		}


		[UnityTest]
		public IEnumerator QueryArgs_IsThirdPartyAvailable()
		{
			var expectedUrl =
				"/basic/accounts/available/third-party?thirdParty=google&token=a%2bbc";

			TriggerContextInit();
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);

			Requester.MockRequest<AvailabilityResponse>(Method.GET,
														expectedUrl
					 ).WithNoAuthHeader()
					 .WithResponse(new AvailabilityResponse
					 {
						 available = true
					 });
			var api = Context.Api.AuthService;

			var req = api.GetCredentialStatus(AuthThirdParty.Google, "a+bc");
			yield return req.ToYielder();
			Assert.IsTrue(req.GetResult() == CredentialUsageStatus.NEVER_USED);
		}

		[UnityTest]
		public IEnumerator QueryArgs_ThirdPartyNotAvailable()
		{
			var expectedUrl =
				"/basic/accounts/available/third-party?thirdParty=google&token=a%2bbc";

			TriggerContextInit();
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);

			Requester.MockRequest<AvailabilityResponse>(Method.GET,
			                                            expectedUrl
			         ).WithNoAuthHeader()
			         .WithResponse(new AvailabilityResponse
			         {
				         available = false
			         });
			var api = Context.Api.AuthService;

			var req = api.GetCredentialStatus(AuthThirdParty.Google, "a+bc");
			yield return req.ToYielder();
			Assert.IsTrue(req.GetResult() == CredentialUsageStatus.ASSIGNED_TO_AN_ACCOUNT);
		}

		[UnityTest]
		public IEnumerator QueryArgs_ThirdPartyRequestError()
		{
			var expectedUrl =
				"/basic/accounts/available/third-party?thirdParty=google&token=a%2bbc";
			var exception = new RequesterException("", "", "", 400, "{}");

			TriggerContextInit();
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);

			Requester.MockRequest<AvailabilityResponse>(Method.GET,
			                                            expectedUrl
			         ).WithNoAuthHeader()
			         .WithResponse(exception);
			var api = Context.Api.AuthService;

			var req = api.GetCredentialStatus(AuthThirdParty.Google, "a+bc");
			yield return req.ToYielder();
			Assert.IsTrue(req.GetResult() == CredentialUsageStatus.INVALID_CREDENTIAL);
		}


		[UnityTest]
		public IEnumerator QueryArgs_RemoveThirdPartyAssociation()
		{
			var expectedUrl =
				"/basic/accounts/me/third-party?thirdParty=google&token=a%2bbc";

			TriggerContextInit();
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);

			Requester.MockRequest<User>(Method.DELETE,
										expectedUrl
					 )
					 .WithResponse(new User { });
			var api = Context.Api.AuthService;

			var req = api.RemoveThirdPartyAssociation(AuthThirdParty.Google, "a+bc");
			yield return req.ToYielder();

		}
	}
}
