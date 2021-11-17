using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;

namespace Beamable.Api.Inventory
{
	/// <summary>
	/// This type defines the reset/refresh of related data.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature documentation
	/// - See Beamable.Api.Inventory.InventoryService script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class InventorySubscription : PlatformSubscribable<InventoryResponse, InventoryView>
	{
		private const string SERVICE = "inventory";

		private readonly InventoryView view = new InventoryView();

		public InventorySubscription(IPlatformService platform, IBeamableRequester requester) : base(
			platform, requester, SERVICE)
		{
			UsesHierarchyScopes = true;
		}

		protected override void Reset()
		{
			view.Clear();
		}

		protected override void OnRefresh(InventoryResponse data)
		{
			data.MergeView(view);
			foreach (var scope in data.GetNotifyScopes())
			{
				Notify(scope, view);
			}
		}
	}

	/// <summary>
	/// This type defines the %Client main entry point for the %Inventory feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ### Example
	/// This demonstrates example usage.
	///
	/// ```
	///
	/// private async void SetupBeamable()
	/// {
	///
	///   var beamableAPI = await Beamable.API.Instance;
	///
	///   beamableAPI.InventoryService.Subscribe("items", view =>
	///   {
	///
	///     foreach (KeyValuePair<string, List<ItemView>> kvp in view.items)
	///     {
	///
	///       string inventoryItemName = $"{kvp.Key} x {kvp.Value.Count}"; // "Big Sword x 1"
	///
	///     }
	///
	///   });
	///
	/// }
	///
	/// ```
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class InventoryService : AbsInventoryApi,
									IHasPlatformSubscriber<InventorySubscription, InventoryResponse, InventoryView>
	{
		public InventorySubscription Subscribable
		{
			get;
		}

		public InventoryService(IPlatformService platform, IBeamableRequester requester) : base(requester, platform)
		{
			Subscribable = new InventorySubscription(platform, requester);
		}

		public override Promise<InventoryView> GetCurrent(string scope = "") => Subscribable.GetCurrent(scope);
	}
}
