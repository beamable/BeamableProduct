using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Beamable.Serialization.SmallerJSON;

namespace Beamable.Server.Api.Inventory
{
   public class MicroserviceInventoryApi : AbsInventoryApi, IMicroserviceInventoryApi
   {
      public const string SERVICE_OBJECT = "/object/inventory";
      public IBeamableRequester Requester { get; }
      public IUserContext UserContext { get; }

      private BeamableGetApiResource<InventoryResponse> _getter;

      public MicroserviceInventoryApi(IBeamableRequester requester, IUserContext userContext) : base(requester, userContext)
      {
         Requester = requester;
         UserContext = userContext;

         _getter = new BeamableGetApiResource<InventoryResponse>();
      }

      public override Promise<InventoryView> GetCurrent(string scope = "")
      {
         return _getter.RequestData(Requester, UserContext, "inventory", scope).Map(res =>
         {
            var view = new InventoryView();
            res.MergeView(view);
            return view;
         });
      }
      
      public Promise<Unit> SendCurrency(Dictionary<string, long> currencies, long recipientPlayer, string transaction = null)
      {
	      var bodyRequest = new ArrayDict
	      {
		      { nameof(transaction), transaction},
		      { nameof(recipientPlayer), recipientPlayer},
		      { nameof(currencies), currencies}
	      };
	      var url = $"{SERVICE_OBJECT}/{UserContext.UserId}/transfer";

	      return Requester.Request<Unit>(Method.PUT, url, bodyRequest);
      }
   }
}
