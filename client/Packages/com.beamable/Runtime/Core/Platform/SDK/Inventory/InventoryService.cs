using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Beamable.Serialization.SmallerJSON;
using System.Collections.Generic;

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

      /// <summary>
      /// Mapping of each requested scope to the body generated for it.
      /// </summary>
      private Dictionary<string, ArrayDict> ScopesToBodyMap;

      /// <summary>
      /// Last value that passed through <see cref="CreateRefreshUrl"/>.
      /// </summary>
      private ArrayDict OutgoingBody;

      public InventorySubscription(IPlatformService platform, IBeamableRequester requester) : base(platform, requester, SERVICE)
      {
	      ScopesToBodyMap = new Dictionary<string, ArrayDict>();
	      
	      UsesHierarchyScopes = true;        
         
         _executeRequestDelegate = RequestData;
         _createRefreshUrlDelegate = CreateRefreshUrl;
         getter = null;
      }

      protected override void Reset()
      {
         view.Clear();
      }
      
      /// <summary>
      /// Makes a request using the last-defined <see cref="OutgoingBody"/>. This relies on the fact that <see cref="PlatformSubscribable{ScopedRsp,Data}.ExecuteRequest"/> is always called
      /// with <see cref="PlatformSubscribable{ScopedRsp,Data}.CreateRefreshUrl"/> as its <paramref name="url"/> parameter. 
      /// </summary>
      protected Promise<InventoryResponse> RequestData(IBeamableRequester requester, string url)
      {
	      return requester.Request<InventoryResponse>(Method.POST, url, OutgoingBody);
      }
      
      /// <summary>
      /// Builds a <see cref="Method.POST"/> request's body for the given scope and caches it in <see cref="ScopesToBodyMap"/>.
      /// </summary>
      /// <param name="scope">A ","-separated string with all item types or ids that we want to get.</param>      
      protected  string CreateRefreshUrl(IUserContext ctx, string serviceName, string scope)
      {
	      if (!ScopesToBodyMap.TryGetValue(scope, out var body))
	      {
		      body = OutgoingBody = new ArrayDict()
		      {
			      { "scopes", scope.Split(',')}
		      };
		      ScopesToBodyMap.Add(scope, body);		      
	      }
	      
	      return $"/object/{serviceName}/{ctx.UserId}";
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
   public class InventoryService : AbsInventoryApi, IHasPlatformSubscriber<InventorySubscription, InventoryResponse, InventoryView>
   {
      public InventorySubscription Subscribable { get; }

      public InventoryService (IPlatformService platform, IBeamableRequester requester) : base(requester, platform)
      {
         Subscribable = new InventorySubscription(platform, requester);
      }

      public override Promise<InventoryView> GetCurrent(string scope = "") => Subscribable.GetCurrent(scope);
   }
}

