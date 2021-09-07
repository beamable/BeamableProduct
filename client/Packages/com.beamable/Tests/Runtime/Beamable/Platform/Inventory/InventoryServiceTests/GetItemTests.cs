using System.Collections;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Service;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Platform.Tests.Inventory.InventoryServiceTests
{
   public class GetItemTests : InventoryServiceTestBase
   {
      [UnityTest]
      public IEnumerator DebugTest_PromiseSequestion()
      {
         var p = new Promise<int>();
         var p2 = Promise.Sequence(Promise<int>.Successful(1), Promise<int>.Successful(2), p);

         p.CompleteSuccess(3);
         yield return p2.AsYield();

         Assert.IsTrue(p2.IsCompleted);
      }

      void EnableCI()
      {
         GameObject coroutineServiceGo = new GameObject();
         var coroutineService = MonoBehaviourServiceContainer<CoroutineService>.CreateComponent(coroutineServiceGo);
         ServiceManager.Provide(coroutineService);
         ServiceManager.ProvideWithDefaultContainer(new ContentParameterProvider
         {
            manifestID = "global"
         });
         ServiceManager.AllowInTests();
         ContentService.AllowInTests();
      }


      [UnityTest]
      public IEnumerator GetItemGroupSubset()
      {
         Debug.Log("Testing stuff in GetItemGroupSubset");
         #if UNITY_EDITOR
         Debug.Log("The Unity Editor Flag exists");
         #else
         Debug.Log("The Unity Editor flag is absent")
         #endif

         var srv = ServiceManager.ResolveIfAvailable<CoroutineService>();

         IEnumerator Sub()
         {
            Debug.Log("1");
            yield return null;
            Debug.Log("2");

         }

         srv.StartCoroutine(Sub());
         Debug.Log("Service manager is available? " + (srv?.ToString() ?? "nope"));

         // mock out a piece of content.
         var contentName = "test";
         _content.Provide(InventoryTestItem.New("junk", 1).SetContentName("junk"));
         _content.Provide(InventoryTestItem.New(contentName, 123).SetContentName(contentName));
         _content.Provide(InventoryTestItem.New("rando", 2).SetContentName("rando"));

         // Mock out a network request that get an item. This semi defines the web API itself.
         _requester
            .MockRequest<InventoryResponse>(Method.GET, $"{objectUrl}?scope=items.inventoryTestItem.{contentName}")
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
                        }
                     }
                  },
                  new ItemGroup
                  {
                     id ="items.inventoryTestItem.junk",
                     items = new List<Item>()
                  },
                  new ItemGroup
                  {
                     id="items.inventoryTestItem.rando",
                     items = new List<Item>
                     {
                        new Item
                        {
                           id="1",
                           properties = new List<ItemProperty>()
                        }
                     }
                  }
               }
            });


         // test our sdk code, and verify that the response is what we expect.
         yield return _service.GetItems<InventoryTestItem>(new InventoryTestItemRef($"items.inventoryTestItem.{contentName}")).Then(view =>
         {
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(contentName, view[0].ItemContent.name);
            Assert.AreEqual("bar1", view[0].Properties["foo"]);
            Assert.AreEqual(123, view[0].ItemContent.Foo);

         }).AsYield();
      }

      [UnityTest]
      public IEnumerator GetManyItems()
      {
         // mock out a piece of content.
         var contentName = "test";
         var content = new InventoryTestItem {Foo = 123};
         content.SetContentName(contentName);
         _content.Provide(content);

         // Mock out a network request that get an item. This semi defines the web API itself.
         _requester
            .MockRequest<InventoryResponse>(Method.GET, $"{objectUrl}?scope=items.inventoryTestItem")
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

      [UnityTest]
      public IEnumerator GetAnItemThatExists()
      {
         // mock out a piece of content.
         var contentName = "test";
         var content = new InventoryTestItem {Foo = 123};
         content.SetContentName(contentName);
         _content.Provide(content);

         // Mock out a network request that get an item. This semi defines the web API itself.
         _requester
            .MockRequest<InventoryResponse>(Method.GET, $"{objectUrl}?scope=items.inventoryTestItem")
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
                                 value = "bar"
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
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(contentName, view[0].ItemContent.name);
            Assert.AreEqual("bar", view[0].Properties["foo"]);
            Assert.AreEqual(123, view[0].ItemContent.Foo);
         }).AsYield();

      }
   }
}