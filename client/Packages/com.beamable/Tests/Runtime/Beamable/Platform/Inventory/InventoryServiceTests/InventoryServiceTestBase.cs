using Beamable.Api;
using Beamable.Api.Connectivity;
using Beamable.Api.Inventory;
using Beamable.Api.Payments;
using Beamable.Api.Sessions;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Dependencies;
using Beamable.Common.Inventory;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Platform.Tests.Content;
using Beamable.Service;
using Beamable.Tests.Runtime;
using NUnit.Framework;

namespace Beamable.Platform.Tests.Inventory.InventoryServiceTests
{
   public class InventoryServiceTestBase
   {
      public const string ROUTE = "/basic/accounts";
      public const string SERVICE_URL = "/object/inventory";

      protected MockPlatformAPI _requester;
      // protected MockPlatformService _platform;
      protected InventoryService _service;
      protected MockContentService _content;



      protected string objectUrl;

      [SetUp]
      public void Init()
      {

	      // create a beam context itself, and sub in a few things

	      _content = new MockContentService();
	      ContentApi.Instance = Promise<IContentApi>.Successful(_content);


	      var ctx = MockBeamContext.Create(onInit: c =>
	      {
		      c.AddStandardGuestLoginRequests()
		       .AddPubnubRequests()
		       .AddSessionRequests()
		       ;
	      }, mutateDependencies: b =>
	      {
		      b.RemoveIfExists<IBeamablePurchaser>();
		      b.RemoveIfExists<IContentApi>();
		      b.AddSingleton<IContentApi>(_content);
	      });

	      _requester = ctx.Requester;

	      _service = ctx.ServiceProvider.GetService<InventoryService>();
	      // _platform = ctx.ServiceProvider.GetService<IPlatformService>();
	      // _platform = new MockPlatformService();

         // var builder = new DependencyBuilder()
         //               .AddSingleton<IPlatformService>(_platform)
         //               .AddSingleton<IUserContext>(_platform)
         //               .AddSingleton<IContentApi>(_content)
         //               .AddSingleton<IConnectivityService>(_platform.ConnectivityService)
         //               .AddSingleton<INotificationService>(_platform.Notification)
         //               .AddSingleton<CoroutineService>(_platform.CoroutineService)
         //               .AddSingleton<IPubnubNotificationService>(_platform.PubnubNotificationService)
         //               .AddSingleton<IHeartbeatService>(_platform.Heartbeat)
         //               .AddSingleton<IBeamableRequester>(_requester);

         // var provider = builder.Build();
         //
         // _platform.User = new User
         // {
         //    id = 1234
         // };

         // _service = new InventoryService(provider);

         objectUrl = $"{SERVICE_URL}/{ctx.AuthorizedUser.Value}";
      }

      [TearDown]
      public void Cleanup()
      {

      }
   }


}
