using System.Collections.Generic;
using Beamable.Api;
using Beamable.Api.Stats;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Platform.Tests;
using NUnit.Framework;
using Packages.Beamable.Runtime.Tests.Beamable;

namespace Beamable.Tests.Runtime
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
         MockPlatformUser = new User {id = 12};
         MockPlatform.User = MockPlatformUser;
         MockApi.User = MockPlatform.User;
         MockRequester = new MockPlatformAPI();
         API.Instance = Promise<IBeamableAPI>.Successful(MockApi);
         OnSetupBeamable();
      }

      protected virtual void OnSetupBeamable()
      {
         // maybe do something to the beamable instance?
      }
   }
}