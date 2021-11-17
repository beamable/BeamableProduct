using Beamable.Common;
using Beamable.Api.Payments;
using Beamable.Common.Api;

namespace Beamable.Api.Commerce
{
	/// <summary>
	/// This type defines the %Client main entry point for the %Commerce feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/store-feature">Store</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public class CommerceService : PlatformSubscribable<GetOffersResponse, PlayerStoreView>
	{
		public CommerceService(PlatformService platform, PlatformRequester requester) : base(
			platform, requester, "commerce") { }

		protected override void OnRefresh(GetOffersResponse data)
		{
			foreach (var store in data.stores)
			{
				store.Init();

				Notify(store.symbol, store);
				if (store.nextDeltaSeconds > 0)
				{
					ScheduleRefresh(store.nextDeltaSeconds, store.symbol);
				}
			}
		}

		public Promise<Unit> OnPurchase(string storeSymbol, string listingSymbol)
		{
			var store = GetLatest(storeSymbol);
			if (store == null)
			{
				return Promise<Unit>.Successful(PromiseBase.Unit);
			}

			var listing = store.listings.Find(lst => lst.symbol == listingSymbol);
			if (listing == null)
			{
				return Promise<Unit>.Successful(PromiseBase.Unit);
			}

			// Intercept purchase with a refresh if something fundamental about the listing is going to change by purchasing
			if (listing.queryAfterPurchase)
			{
				return Refresh(storeSymbol);
			}

			return Promise<Unit>.Successful(PromiseBase.Unit);
		}

		public Promise<EmptyResponse> Purchase(string storeSymbol, string listingSymbol)
		{
			long gamerTag = platform.User.id;
			string purchaseId = $"{listingSymbol}:{storeSymbol}";
			return requester.Request<EmptyResponse>(
				Method.POST,
				$"/object/commerce/{gamerTag}/purchase?purchaseId={purchaseId}"
			);
		}

		public void ForceRefresh(string storeSymbol)
		{
			Refresh(storeSymbol);
		}
	}
}
