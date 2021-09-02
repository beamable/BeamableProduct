using System.Collections.Generic;
using System.Reflection;
using Beamable.Common.Api.Auth;
using Beamable.Platform.SDK.Auth;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Platform.Tests.Auth.AuthServiceTests
{
   public class AuthServiceTestBase
   {
      public const string ROUTE = "/basic/accounts";
      public const string TOKEN_URL = "/basic/auth/token";

      protected MockPlatformAPI _requester;
      protected AuthApi _service;
      protected User _sampleUser;

      [SetUp]
      public void Init()
      {
         _requester = new MockPlatformAPI();
         _sampleUser = new User();
         _service = new AuthApi(_requester);
      }

      [TearDown]
      public void Cleanup()
      {

      }

   }

}