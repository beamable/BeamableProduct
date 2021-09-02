using Beamable.Api.Inventory;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Content;
using Beamable.Common.Inventory;
using Beamable.Content;
using Beamable.Platform.Tests.Content;
using NUnit.Framework;

namespace Beamable.Platform.Tests.Inventory.InventoryServiceTests
{
   public class InventoryServiceTestBase
   {
      public const string ROUTE = "/basic/accounts";
      public const string SERVICE_URL = "/object/inventory";

      protected MockPlatformAPI _requester;
      protected MockPlatformService _platform;
      protected InventoryService _service;
      protected MockContentService _content;

      protected string objectUrl;

      [SetUp]
      public void Init()
      {
         _requester = new MockPlatformAPI();
         _platform = new MockPlatformService();
         _content = new MockContentService();
         ContentApi.Instance = Promise<IContentApi>.Successful(_content);

         _platform.User = new User
         {
            id = 1234
         };

         _service = new InventoryService(_platform, _requester);

         objectUrl = $"{SERVICE_URL}/{_platform.UserId}";
      }

      [TearDown]
      public void Cleanup()
      {

      }
   }


}