using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Content;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Beamable.Platform.Tests.Inventory.InventoryUpdateBuilderTests
{
   public class AddItemTests
   {
      [SetUp]
      public void Setup()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>
         {
            typeof(InventoryTestItem)
         });
      }

      [Test]
      public void AddOneItem()
      {
         var updateBuilder = new InventoryUpdateBuilder();

         var props = new Dictionary<string, string>
         {
            {"key", "value"}
         };
         var contentId = "contentId";
         updateBuilder.AddItem(contentId, props);

         Assert.AreEqual(1, updateBuilder.newItems.Count);

         Assert.AreEqual(props, updateBuilder.newItems[0].properties);
         Assert.AreEqual(contentId, updateBuilder.newItems[0].contentId);
      }
   }
}