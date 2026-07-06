// Unity IAP v5 removed the v4 IStore / IStoreCallback surface this store is built on.
// The v5 Steam store lives in SteamStoreV5.cs, registered via UnityIAPServices.
#if !UNITY_PURCHASING_5_OR_NEWER
using Beamable.Common.Dependencies;
using Beamable.Common.Steam;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Beamable.Purchasing.Steam
{
	public class SteamStore : IStore
	{
		private readonly IDependencyProvider _provider;
		public const string Name = "SteamStore";

		public ISteamService steam;
		public IStoreCallback callback;
		public List<SteamProduct> steamProducts;

		private Dictionary<string, InProgressPurchase> _inProgress = new Dictionary<string, InProgressPurchase>();

		public SteamStore(IDependencyProvider provider)
		{
			_provider = provider;
		}

		public void Initialize(IStoreCallback callback)
		{
			this.callback = callback;

			this.steam = _provider.GetService<ISteamService>();

			if (this.steam == null)
			{
				OnInitializeFailed("Steam service unavailable. Provide ServiceManager an ISteamService instance.");
			}
			else
			{
				this.steam.RegisterTransactionCallback(OnTransactionAuthorized);
			}
		}

		public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> productDefinitions)
		{
			steam.RegisterAuthTicket()
				.FlatMap(_ => steam.GetProducts())
				.Then(rsp => OnRetrieved(productDefinitions, rsp.products))
				.Error(ex => OnInitializeFailed("Failed to retrieve steam products.", ex));
		}

		private void OnRetrieved(ReadOnlyCollection<ProductDefinition> productDefinitions, List<SteamProduct> steamProducts)
		{
			this.steamProducts = steamProducts;

			var productDescriptions = new List<ProductDescription>();
			foreach (var definition in productDefinitions)
			{
				var steamProduct = steamProducts.Find(r => r.sku == definition.id);
				if (steamProduct != null)
				{
					var price = System.Convert.ToDecimal(steamProduct.localizedPrice);
					var metadata = new ProductMetadata(
						steamProduct.localizedPriceString,
						steamProduct.description,
						steamProduct.description,
						steamProduct.isoCurrencyCode,
						price);

					productDescriptions.Add(new ProductDescription(definition.storeSpecificId, metadata));
				}
			}

			callback.OnProductsRetrieved(productDescriptions);
		}

		private void OnInitializeFailed(string message, System.Exception ex = null)
		{
			Debug.LogError(message);
			if (ex != null)
			{
				Debug.LogError(ex);
			}
#if UNITY_PURCHASING_4_6_OR_NEWER
			callback.OnSetupFailed(InitializationFailureReason.PurchasingUnavailable, message);
#else
			callback.OnSetupFailed(InitializationFailureReason.PurchasingUnavailable);
#endif
		}

		public void OnTransactionAuthorized(SteamTransaction transaction)
		{
			InProgressPurchase currentPurchase;
			if (!_inProgress.TryGetValue(transaction.transactionId, out currentPurchase))
			{
				return;
			}

			if (transaction.authorized)
			{
				callback.OnPurchaseSucceeded(
					currentPurchase.product.storeSpecificId,
					currentPurchase.transactionId,
					currentPurchase.transactionId);
			}
			else
			{
				callback.OnPurchaseFailed(new PurchaseFailureDescription(
					currentPurchase.product.id,
					PurchaseFailureReason.UserCancelled,
					"Steam purchase cancelled."));
			}
		}

		public void Purchase(ProductDefinition product, string developerPayload)
		{
			if (this.steam == null)
			{
				callback.OnPurchaseFailed(new PurchaseFailureDescription(
					product.id,
					PurchaseFailureReason.PurchasingUnavailable,
					"Steam service unavailable. Provide ServiceManager an ISteamService instance."));

				return;
			}

			this._inProgress[developerPayload] = new InProgressPurchase(product, developerPayload);
		}

		public void FinishTransaction(ProductDefinition product, string transactionId)
		{

		}
	}

	public class InProgressPurchase
	{
		public ProductDefinition product;
		public string transactionId;

		public InProgressPurchase(ProductDefinition product, string transactionId)
		{
			this.product = product;
			this.transactionId = transactionId;
		}
	}
}
#endif

#if UNITY_PURCHASING_5_OR_NEWER && !BEAMABLE_STEAM_IMPLEMENTATION_DISABLED
using Beamable.Common.Dependencies;
using Beamable.Common.Steam;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Beamable.Purchasing.Steam
{
	/// <summary>
	/// Unity IAP v5 custom store for Steam. Bridges Beamable's <see cref="ISteamService"/> onto the v5
	/// <see cref="Store"/> abstraction. Registered with the SDK via
	/// <c>UnityIAPServices.AddNewCustomStore</c> and selected by name through <c>new StoreController(Name)</c>.
	/// </summary>
	public class SteamStoreV5 : Store
	{
		public const string Name = "SteamStore";

		private IDependencyProvider _provider;
		private ISteamService _steam;
		private List<SteamProduct> _steamProducts;

		// Steam purchases are authorized out-of-band via the transaction callback. Beamable is single-in-flight,
		// so we track the cart for the current purchase and resolve it when the authorization arrives.
		private ICart _currentCart;

		// The Beamable txid of the in-flight purchase. The Beamable Steam backend initiates the Steam
		// microtransaction using the txid as the order id, so authorization callbacks whose transaction id
		// does not match are foreign/stale and must be ignored (v4 keyed its in-progress purchases the same way).
		private string _expectedTransactionId;

		// The public Store base does not track connection state (that lives on the internal InternalStore),
		// so the custom store maintains it for IStoreWrapper.GetStoreConnectionState.
		public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;

		public SteamStoreV5(IDependencyProvider provider)
		{
			_provider = provider;
		}

		/// <summary>
		/// Refresh the dependency provider. Unity IAP v5 caches store services per store name for the
		/// domain lifetime, so this store instance outlives any single BeamContext initialization; each
		/// <c>Initialize</c> must hand it the current provider so <see cref="Connect"/> resolves the live
		/// <see cref="ISteamService"/>.
		/// </summary>
		public void SetProvider(IDependencyProvider provider)
		{
			_provider = provider;
		}

		/// <summary>
		/// Record the Beamable txid for the purchase about to start, so only the matching Steam
		/// authorization resolves it. Cleared when the purchase resolves; overwritten by the next purchase.
		/// </summary>
		public void SetExpectedTransaction(string transactionId)
		{
			_expectedTransactionId = transactionId;
		}

		public override void Connect()
		{
			ConnectionState = ConnectionState.Connecting;
			var steam = _provider.GetService<ISteamService>();
			if (steam == null)
			{
				ConnectionState = ConnectionState.Unavailable;
				Debug.LogError("Steam service unavailable. Provide ServiceManager an ISteamService instance.");
				ConnectCallback?.OnStoreConnectionFailed(
					new StoreConnectionFailureDescription("Steam service unavailable. Provide ServiceManager an ISteamService instance."));
				return;
			}

			// Connect() can re-enter on the same domain-lifetime store instance after a BeamContext
			// re-initializes with a new provider. Subscribe once per resolved service instance; there is
			// no unsubscribe on ISteamService, but stray callbacks from a stale service are filtered out
			// by the expected-transaction check in OnTransactionAuthorized.
			if (!ReferenceEquals(steam, _steam))
			{
				steam.RegisterTransactionCallback(OnTransactionAuthorized);
			}
			_steam = steam;
			ConnectionState = ConnectionState.Connected;
			ConnectCallback?.OnStoreConnectionSucceeded();
		}

		public override void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
		{
			if (_steam == null)
			{
				ProductsCallback?.OnProductsFetchFailed(
					new ProductFetchFailureDescription(ProductFetchFailureReason.ProviderUnavailable, "Steam service unavailable."));
				return;
			}

			_steam.RegisterAuthTicket()
				.FlatMap(_ => _steam.GetProducts())
				.Then(rsp => OnRetrieved(products, rsp.products))
				.Error(ex =>
				{
					Debug.LogError("Failed to retrieve steam products.");
					Debug.LogError(ex);
					ProductsCallback?.OnProductsFetchFailed(
						new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown, "Failed to retrieve steam products."));
				});
		}

		private void OnRetrieved(IReadOnlyCollection<ProductDefinition> productDefinitions, List<SteamProduct> steamProducts)
		{
			_steamProducts = steamProducts;

			var productDescriptions = new List<ProductDescription>();
			foreach (var definition in productDefinitions)
			{
				var steamProduct = steamProducts.Find(r => r.sku == definition.id);
				if (steamProduct == null) continue;

				var price = System.Convert.ToDecimal(steamProduct.localizedPrice);
				var metadata = new ProductMetadata(
					steamProduct.localizedPriceString,
					steamProduct.description,
					steamProduct.description,
					steamProduct.isoCurrencyCode,
					price);

				productDescriptions.Add(new ProductDescription(definition.storeSpecificId, metadata));
			}

			ProductsCallback?.OnProductsFetched(productDescriptions);
		}

		public override void FetchPurchases()
		{
			// Steam consumables are not restored between sessions in this integration; report an empty set.
			PurchaseFetchCallback?.OnAllPurchasesRetrieved(new List<Order>());
		}

		public override void Purchase(ICart cart)
		{
			// The Steam purchase is authorized out-of-band; OnTransactionAuthorized resolves it.
			// Do NOT fail the old cart here: by the time Purchase runs, the purchaser's txid already
			// refers to the new transaction, and a late FailedOrder would fail the wrong txid.
			if (_currentCart != null)
			{
				Debug.LogWarning("A previous Steam purchase was still pending and is being abandoned.");
			}
			_currentCart = cart;
		}

		private void OnTransactionAuthorized(SteamTransaction transaction)
		{
			if (_currentCart == null)
			{
				return;
			}

			// Ignore foreign/stale authorizations without consuming the in-flight cart — the equivalent
			// of v4's in-progress lookup keyed by the Beamable txid.
			if (_expectedTransactionId != null && transaction.transactionId != _expectedTransactionId)
			{
				return;
			}

			var cart = _currentCart;
			_currentCart = null;
			_expectedTransactionId = null;

			if (transaction.authorized)
			{
				PurchaseCallback?.OnPurchaseSucceeded(new PendingOrder(cart, new SteamOrderInfo(transaction.transactionId)));
			}
			else
			{
				PurchaseCallback?.OnPurchaseFailed(
					new FailedOrder(cart, PurchaseFailureReason.UserCancelled, "Steam purchase cancelled."));
			}
		}

		public override void FinishTransaction(PendingOrder pendingOrder)
		{
			// Steam has no platform-side consume/acknowledgement step, but v5's ConfirmOrderUseCase waits
			// for this ack after calling FinishTransaction — without it ConfirmPurchase never completes and
			// OnPurchaseConfirmed never fires.
			ConfirmCallback?.OnConfirmOrderSucceeded(pendingOrder.Info.TransactionID);
		}

		public override void CheckEntitlement(ProductDefinition product)
		{
			// Beamable does not use entitlement checks, but the request must be answered: v5's
			// CheckEntitlementUseCase keeps unanswered requests in its ongoing list forever and rejects
			// all later checks for the same product as duplicates.
			EntitlementCallback?.OnCheckEntitlement(product, EntitlementStatus.Unknown,
				"Entitlement checks are not supported by the Steam store.");
		}
	}

	/// <summary>
	/// Minimal <see cref="IOrderInfo"/> implementation for the Steam custom store. The concrete Unity
	/// OrderInfo type is internal to the IAP package, so custom stores supply their own.
	/// </summary>
	internal class SteamOrderInfo : IOrderInfo
	{
		public SteamOrderInfo(string transactionId)
		{
			TransactionID = transactionId;
			// Unity's built-in stores wrap platform receipts into the unified receipt JSON
			// ({Store, TransactionID, Payload}) via the internal UnifiedReceiptFormatter. Custom stores
			// must do it themselves: UnityBeamablePurchaser parses Order.Info.Receipt with
			// JsonUtility.FromJson<UnityPurchaseReceipt>, which throws on non-JSON input. As in v4,
			// the Steam transaction id doubles as the receipt payload for the Beamable backend.
			Receipt = JsonUtility.ToJson(new UnityPurchaseReceipt
			{
				Store = SteamStoreV5.Name,
				TransactionID = transactionId,
				Payload = transactionId
			});
			PurchasedProductInfo = new List<IPurchasedProductInfo>();
		}

		public IAppleOrderInfo Apple => null;
		public IGoogleOrderInfo Google => null;
		public List<IPurchasedProductInfo> PurchasedProductInfo { get; set; }
		public string Receipt { get; }
		public string TransactionID { get; }
	}

	/// <summary>
	/// Wraps <see cref="SteamStoreV5"/> for registration with <c>UnityIAPServices.AddNewCustomStore</c>.
	/// </summary>
	public class SteamStoreWrapperV5 : IStoreWrapper
	{
		private readonly SteamStoreV5 _store;

		public SteamStoreWrapperV5(SteamStoreV5 store)
		{
			_store = store;
			instance = store;
		}

		public Store instance { get; }
		public string name => SteamStoreV5.Name;
		public ConnectionState GetStoreConnectionState() => _store.ConnectionState;
	}
}
#endif

