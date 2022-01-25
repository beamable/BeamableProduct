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
		protected MockPlatformAPI _requester;
		protected InventoryService _service;
		protected MockContentService _content;

		private MockBeamContext _ctx;

		[SetUp]
		public void Init()
		{

			// create a beam context itself, and sub in a few things
			_content = new MockContentService();
			ContentApi.Instance = Promise<IContentApi>.Successful(_content);


			_ctx = MockBeamContext.Create(onInit: c =>
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

			_requester = _ctx.Requester;

			_service = _ctx.ServiceProvider.GetService<InventoryService>();
		}

		[TearDown]
		public void Cleanup()
		{
			_ctx.ClearPlayerAndStop();
		}
	}
}
