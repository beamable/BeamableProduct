using Beamable.BSAT.Attributes;
using Beamable.BSAT.Core;
using Beamable.Common;
using Beamable.Common.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TestResult.Passed - Marks automatically a test as passed.
/// TestResult.Failed - Marks automatically a test as failed.
/// TestResult.NotSet - Requires user manual check if a test is passed or failed using buttons in scene UI (Passed/Failed). Used especially for UI tests.
/// </summary>

namespace Beamable.BSAT.Test.Inventory
{
	public class InventoryScript : Testable
	{
		private BeamContext _ctx;

		[SerializeField] private ItemRef testItemRef;

		[TestRule(-1)]
		public TestResult ReferencesPreCheck()
		{
			return string.IsNullOrWhiteSpace(testItemRef.Id) ? TestResult.Failed : TestResult.Passed;
		}
		
		[TestRule(0)]
		public async Promise<TestResult> InitDefaultBeamContext()
		{
			try
			{
				_ctx = BeamContext.Default;
				await _ctx.OnReady;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
			return TestResult.Passed;
		}

		[TestRule(1)]
		public async Promise<TestResult> ChangeAuthorizedPlayer()
		{
			try
			{
				var tokenResponse = await _ctx.Api.AuthService.CreateUser();
				_ctx = await BeamContext.Default.ChangeAuthorizedPlayer(tokenResponse);
				await _ctx.OnReady;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
			return TestResult.Passed;
		}
		
		[TestRule(2)]
		public async Promise<TestResult> AddOneItem()
		{
			try
			{
				await _ctx.Api.InventoryService.AddItem(testItemRef.Id, new Dictionary<string, string>());
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
			return TestResult.Passed;
		}
		
		[TestRule(3)]
		public async Promise<TestResult> GetItems()
		{
			try
			{
				var allOwnedItems = await _ctx.Api.InventoryService.GetCurrent();
				if (allOwnedItems.items.Count == 0)
				{
					TestableDebug.LogError("PLAYER GetItems() failed. Player has no items yet. There should be one item.");
					return TestResult.Failed;
				}
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
			return TestResult.Passed;
		}
		
		[TestRule(4)]
		public async Promise<TestResult> DeleteOneItem()
		{
			try
			{
				var allOwnedItems = await _ctx.Api.InventoryService.GetCurrent();
				if (allOwnedItems.items == null || allOwnedItems.items.Count == 0)
				{
					TestableDebug.LogError("PLAYER DeleteOneItem() failed. Player has no items yet. There should be one item.");
					return TestResult.Failed;
				}
   
				long itemIdToDelete = 0;
				foreach (var item in allOwnedItems.items)
				{
					if (item.Key == testItemRef.Id)
					{
						itemIdToDelete = item.Value[0].id;
						break;
					}
				}
         
				if (itemIdToDelete == 0)
				{
					TestableDebug.LogError("PLAYER DeleteOneItem() failed. Player has no items of that type yet. There should be one item.");
					return TestResult.Failed;
				}

				await _ctx.Api.InventoryService.DeleteItem(testItemRef.Id, itemIdToDelete);
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
			return TestResult.Passed;
		}
	}
}
