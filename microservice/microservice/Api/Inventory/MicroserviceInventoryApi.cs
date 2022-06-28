using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

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

	}
}
