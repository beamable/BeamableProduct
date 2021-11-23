using System.Collections.Generic;
using Beamable.Api;
using Beamable.Api.Stats;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Platform.Tests;
using NUnit.Framework;
using Packages.Beamable.Runtime.Tests.Beamable;
using Utf8Json;
using Utf8Json.Formatters;

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

      public BeamableTest()
      {
	      Utf8Json.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
		      new IJsonFormatter[] {PrimitiveObjectFormatter.Default},
		      new[]
		      {
			      Utf8Json.Resolvers.BuiltinResolver.Instance,
			      Utf8Json.Resolvers.CompositeResolver.Instance,
			      Utf8Json.Resolvers.DynamicGenericResolver.Instance,
			      Utf8Json.Resolvers.AttributeFormatterResolver.Instance,
		      }
	      );
      }

      [SetUp]
      public void SetupBeamable()
      {
         MockApi = new MockBeamableApi();
         MockPlatform = new MockPlatformService();
         MockPlatformUser = new User {id = 12};
         MockPlatform.User = MockPlatformUser;
         MockApi.User = MockPlatform.User;
         MockApi.Token = new AccessToken(null, "testcid", "testpid", "testtoken", "refresh", 0);
         MockRequester = new MockPlatformAPI();
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
