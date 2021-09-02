using System.Collections;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Platform.SDK;
using Beamable.Platform.SDK.Auth;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Beamable.Platform.Tests.Auth.AuthServiceTests
{
   public class GetUserTests : AuthServiceTestBase
   {
      [UnityTest]
      public IEnumerator MakesWebCall_ReturnsUser()
      {
         var req = _requester.MockRequest<User>(Method.GET, $"{AuthServiceTestBase.ROUTE}/me").WithResponse(_sampleUser);

         yield return _service.GetUser().Then(user => { Assert.AreEqual(_sampleUser, user); }).AsYield();

         Assert.AreEqual(1, req.CallCount);
      }

      [UnityTest]
      public IEnumerator WithCustomToken()
      {
         var token = new TokenResponse() {access_token = "abc"};
         var req = _requester.MockRequest<User>(Method.GET, $"{AuthServiceTestBase.ROUTE}/me")
            .WithToken("abc")
            .WithResponse(_sampleUser);

         yield return _service.GetUser(token).Then(user => { Assert.AreEqual(_sampleUser, user); }).AsYield();
         Assert.AreEqual(1, req.CallCount);
      }
   }
}