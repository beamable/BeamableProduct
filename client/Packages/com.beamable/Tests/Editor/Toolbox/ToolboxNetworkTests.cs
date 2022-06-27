using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Beamable.Common.Dependencies;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Components;
using Beamable.Platform.Tests;
using Beamable.Editor.Realms;
using Beamable.Api;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Beamable.Platform.Tests.Inventory;
using Beamable.Platform.Tests.Content;
using Beamable.Serialization.SmallerJSON;
using Beamable.Api.Inventory;
using Beamable.Api.Payments;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Tests.Runtime;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Tests
{
    public class ToolboxNetworkTests
    {
		private IDependencyProviderScope provider;

		// A Test behaves as an ordinary method
		[Test]
        public void TestNetwork()
        {
			var _requester = new MockPlatformAPI();

			//BeamEditor line 234
			//RealmsServices constructor
			//mock network
			//requester.MockRequest();
			//
			IDependencyBuilder builder = new DependencyBuilder();
			builder.AddSingleton<IPlatformRequester, MockPlatformAPI>(_requester);
			builder.AddSingleton<RealmsService>();

			provider = builder.Build();

			var realmsService = provider.GetService<RealmsService>();
			
			realmsService.GetCustomerData();
			_requester
				.MockRequest<CustomerView>(Method.GET)
				.WithResponse(new CustomerView
				{
					Cid = "12345",
					Alias = "fakeAlias",
					DisplayName = "fakeDisplayName",
					Projects = new List<RealmView>
					{

					}
				});
		}

		[Test]
		public IEnumerator GetManyItems()
		{
			MockBeamContext _ctx;

			MockPlatformAPI _requester;
			InventoryService _service;
			MockContentService _content;

			_content = new MockContentService();

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

			

			IDependencyBuilder builder = new DependencyBuilder();
			builder.AddSingleton<IPlatformRequester, MockPlatformAPI>(_requester);
			builder.AddSingleton<RealmsService>();

			// mock out a piece of content.
			var contentName = "test";
			var content = new InventoryTestItem { Foo = 123 };
			content.SetContentName(contentName);
			_content.Provide(content);

			// Mock out a network request that get an item. This semi defines the web API itself.
			_requester
			   .MockRequest<InventoryResponse>(Method.POST)
			   .WithURIPrefix("/object/inventory")
			   .WithBodyMatch<ArrayDict>(sent =>
			   {
				   var expected = new ArrayDict() { { "scopes", new[] { "items.inventoryTestItem" } } };

				   var matchKeys = expected.Keys.SequenceEqual(sent.Keys);
				   var matchValuesLength = expected.Values.Count == sent.Values.Count;
				   var matchValues = ((string[])expected.Values.ElementAt(0))[0] == ((string[])sent.Values.ElementAt(0))[0];

				   return matchKeys && matchValuesLength && matchValues;
			   })
			   .WithResponse(new InventoryResponse
			   {
				   currencies = new List<Currency>(),
				   scope = "items.inventoryTestItem.test",
				   items = new List<ItemGroup>
				  {
				  new ItemGroup
				  {
					 id = "items.inventoryTestItem.test",
					 items = new List<Item>
					 {
						new Item
						{
						   id = "1",
						   properties = new List<ItemProperty>
						   {
							  new ItemProperty
							  {
								 name = "foo",
								 value = "bar1"
							  }
						   }
						},
						new Item
						{
						   id = "2",
						   properties = new List<ItemProperty>
						   {
							  new ItemProperty
							  {
								 name="foo",
								 value="bar2"
							  }
						   }
						}
					 }
				  }
				  }
			   });

			// test our sdk code, and verify that the response is what we expect.
			yield return _service.GetItems<InventoryTestItem>().Then(view =>
			{
				Assert.AreEqual(2, view.Count);
				Assert.AreEqual(contentName, view[0].ItemContent.name);
				Assert.AreEqual("bar1", view[0].Properties["foo"]);
				Assert.AreEqual(123, view[0].ItemContent.Foo);

				Assert.AreEqual(contentName, view[1].ItemContent.name);
				Assert.AreEqual("bar2", view[1].Properties["foo"]);
				Assert.AreEqual(123, view[1].ItemContent.Foo);
			}).AsYield();
		}
	}
}
