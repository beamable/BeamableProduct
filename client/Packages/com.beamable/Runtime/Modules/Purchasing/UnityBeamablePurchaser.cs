#if UNITY_PURCHASING_5_OR_NEWER && USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR) && !BEAMABLE_STEAM_IMPLEMENTATION_DISABLED
// BEAMABLE_STEAM_IN_EDITOR is an opt-in testing hook that forces the Steam store path
// in playmode so a mock ISteamService can drive the full purchase flow.
#define BEAMABLE_STEAM_STORE_V5
#endif
using Beamable.Api;
using Beamable.Api.Payments;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Spew;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !BEAMABLE_PURCHASING_IMPLEMENTATION_DISABLED
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace Beamable.Purchasing
{
#if !BEAMABLE_PURCHASING_IMPLEMENTATION_DISABLED
	/// <summary>
	/// Implementation of Unity IAP for Beamable purchasing.
	/// </summary>
	public class UnityBeamablePurchaser : IBeamablePurchaser, IBeamableDisposable

#if !UNITY_PURCHASING_5_OR_NEWER // v5 removed the listener interfaces in favor of StoreController events.
										, IStoreListener

#if UNITY_PURCHASING_4_6_OR_NEWER // if this is a newer IAP, then include the detailed store listener.
	                                    , IDetailedStoreListener
#endif
#endif

	{
		/// <summary>
		/// Error code the <see cref="StartPurchase"/> promise fails with when the store defers the payment,
		/// e.g. a Google Play pending payment method or Apple Ask to Buy. The purchase is not lost: once the
		/// payment is approved the store delivers the order again and it is fulfilled in the background.
		/// </summary>
		public const int PURCHASE_DEFERRED_ERROR_CODE = 202;

		public PurchasingInitializationStatus InitializationStatus { get; private set; } =
			PurchasingInitializationStatus.NotInitialized;

#if UNITY_PURCHASING_5_OR_NEWER
		// Unity IAP v5 services are domain-wide singletons: every StoreController shares them, and handlers added
		// through any controller stay attached until removed. So all purchasers (one per BeamContext) share one
		// controller with one set of handlers, and the static handlers below dispatch store events to them.
		private static StoreController s_storeController;
		private static readonly List<UnityBeamablePurchaser> s_purchasers = new List<UnityBeamablePurchaser>();
		private static bool s_fetchInProgress;
		private static bool s_refetchRequested;
		// Store transaction ids of pending orders being fulfilled, so an order the store delivers again
		// (e.g. FetchPurchases during another initialization) is not fulfilled twice.
		private static readonly HashSet<string> s_ordersInFulfillment = new HashSet<string>();
		// Beamable txids of store purchases that started but did not complete, by SKU. An order that arrives later
		// (deferred payment approved, or the context that started it is gone) completes its own transaction.
		private static readonly Dictionary<string, long> s_openTxids = new Dictionary<string, long>();

		private StoreController _storeController;
		// The store product definitions, built during Initialize and fetched once the store connects.
		private List<ProductDefinition> _productDefinitions;
		// True while this purchaser's initialization waits for the store to return its products.
		private bool _awaitingProducts;
		private Promise<Unit> _connectPromise;
#else
		private IStoreController _storeController;
#pragma warning disable CS0649
		private IAppleExtensions _appleExtensions;
		private IGooglePlayStoreExtensions _googleExtensions;
#pragma warning restore CS0649
#endif

		private Promise<Unit> _initPromise = new Promise<Unit>();
		private long _txid;
		// The Beamable SKU of the purchase StartPurchase is driving, and whether the store was asked to buy it yet.
		private string _activeSku;
		private bool _storePurchaseStarted;
		private Action<CompletedTransaction> _success;
		private Action<ErrorCode> _fail;
		private Action _cancelled;
		private IDependencyProvider _serviceProvider;

		static readonly int[] RETRY_DELAYS = { 1, 2, 5, 10, 20 }; // TODO: Just double a few times. ~ACM 2021-03-10


		public Promise<Unit> Initialize(IDependencyProvider provider)
		{
			if (InitializationStatus == PurchasingInitializationStatus.InProgress)
			{
				return _initPromise;
			}
			_initPromise = new Promise<Unit>();
			InitializationStatus = PurchasingInitializationStatus.InProgress;
			_serviceProvider = provider;
			var paymentService = GetPaymentService();

			var skuPromise = paymentService.GetSKUs();
			return skuPromise.Recover(e =>
			{
				InitializationStatus = PurchasingInitializationStatus.ErrorFailedToGetSkus;
				_initPromise.CompleteError(e);
				return new GetSKUsResponse();
			}).FlatMap(rsp =>
			{
				if (_initPromise.IsFailed)
				{
					return _initPromise;
				}
				var noSkusAvailable = rsp.skus.definitions.Count == 0;
				if (noSkusAvailable)
				{
					InitializationStatus = PurchasingInitializationStatus.CancelledNoSkusConfigured;
					// If there are no SKUs available, we will short-circuit the rest of the init-flow.
					// Most importantly, we don't call `UnityPurchasing.Initialize`, so that we don't receive the Purchase Finished callbacks.
					_initPromise.CompleteSuccess(PromiseBase.Unit);
					return _initPromise;
				}


#if UNITY_PURCHASING_5_OR_NEWER
				_productDefinitions = BuildProductDefinitions(rsp.skus.definitions);
				if (_productDefinitions.Count == 0)
				{
					InitializationStatus = PurchasingInitializationStatus.CancelledNoSkusConfigured;
					Debug.LogWarning($"Beamable IAP is disabled: no SKU has a product id for {Application.platform}.");
					_initPromise.CompleteSuccess(PromiseBase.Unit);
					return _initPromise;
				}

				// The v5 lifecycle is split and async: Connect -> (HandleStoreConnected) FetchProducts
				// -> (HandleProductsFetched) FetchPurchases. The promise resolves from the event handlers.
				_awaitingProducts = true;
				var controller = EnsureStoreController();
				if (controller.GetConnectionState() == ConnectionState.Connected)
				{
					// Already connected by an earlier initialization, so OnStoreConnected won't fire again.
					RequestProducts();
				}
				else
				{
					controller.Connect();
				}
				return _initPromise;
#else
#if USE_STEAMWORKS && !UNITY_EDITOR
                var builder = ConfigurationBuilder.Instance(new Steam.SteamPurchasingModule(_serviceProvider));
                foreach (var sku in rsp.skus.definitions)
                {
				   if (sku == null) continue;

                   var ids = UnityBeamablePurchaserUtil.CreateIdsFromSku(
						new SkuIdPair {skuProductId = sku.productIds?.steam, storeId = Steam.SteamStore.Name,}
				   );
				   builder.AddProduct(sku.name, ProductType.Consumable, ids);
                }
#else
				var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
				foreach (var sku in rsp.skus.definitions)
				{
					if (sku == null) continue;

					var ids = UnityBeamablePurchaserUtil.CreateIdsFromSku(
						new SkuIdPair {skuProductId = sku.productIds?.itunes, storeId = AppleAppStore.Name,},
						new SkuIdPair {skuProductId = sku.productIds?.googleplay, storeId = GooglePlay.Name,}
					);
					builder.AddProduct(sku.name, ProductType.Consumable, ids);
				}
#endif

				// Kick off the remainder of the set-up with an asynchrounous call,
				// passing the configuration and this class's instance. Expect a
				// response either in OnInitialized or OnInitializeFailed.
				UnityPurchasing.Initialize(this, builder);
				return _initPromise;
#endif
			});

		}

		public Promise OnDispose()
		{
#if UNITY_PURCHASING_5_OR_NEWER
			s_purchasers.Remove(this);
#endif
			return Promise.Success;
		}


		/// <summary>
		/// Clear all the callbacks and zero out the transaction ID.
		/// </summary>
		private void ClearCallbacks()
		{
			_success = null;
			_fail = null;
			_cancelled = null;
			_txid = 0;
			_activeSku = null;
			_storePurchaseStarted = false;
		}

		/// <summary>
		/// Clear the callbacks if they still belong to the purchase that <paramref name="onSuccess"/> completes.
		/// </summary>
		private void ReleaseCallbacks(Action<CompletedTransaction> onSuccess)
		{
			if (onSuccess != null && _success == onSuccess)
			{
				ClearCallbacks();
			}
		}

		/// <summary>
		/// True when <paramref name="sku"/> is the product the store is buying for this purchaser's StartPurchase.
		/// </summary>
		private bool IsActivePurchase(string sku) => _storePurchaseStarted && sku != null && sku == _activeSku;

		private PaymentService GetPaymentService()
		{
			return _serviceProvider.GetService<PaymentService>();
		}

		private CoroutineService GetCoroutineService()
		{
			return _serviceProvider.GetService<CoroutineService>();
		}

		private Product GetStoreProduct(string skuSymbol)
		{
#if UNITY_PURCHASING_5_OR_NEWER
			return _storeController?.GetProductById(skuSymbol);
#else
			return _storeController?.products.WithID(skuSymbol);
#endif
		}

#if UNITY_PURCHASING_5_OR_NEWER
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			// With domain reload disabled, statics survive between play sessions. The controller and its handlers
			// stay (IAP's own singletons survive too), but purchasers and orders of the last session are dropped.
			s_purchasers.Clear();
			s_ordersInFulfillment.Clear();
			s_openTxids.Clear();
			s_fetchInProgress = false;
			s_refetchRequested = false;
		}

		/// <summary>
		/// Create the shared StoreController and subscribe its handlers once per domain, and register this purchaser
		/// to receive store events.
		/// </summary>
		private StoreController EnsureStoreController()
		{
#if BEAMABLE_STEAM_STORE_V5
			// Register the Steam custom store with the SDK, then target it by name.
			RegisterSteamStore();
#endif
			if (s_storeController == null)
			{
#if BEAMABLE_STEAM_STORE_V5
				s_storeController = new StoreController(Steam.SteamStoreV5.Name);
#else
				s_storeController = new StoreController();
#endif

				// Subscribe to the StoreController events before connecting so no callback is missed.
				s_storeController.OnStoreConnected += HandleStoreConnected;
				s_storeController.OnStoreDisconnected += HandleStoreDisconnected;
				s_storeController.OnProductsFetched += HandleProductsFetched;
				s_storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
				s_storeController.OnPurchasePending += HandlePurchasePending;
				s_storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
				s_storeController.OnPurchaseFailed += HandlePurchaseFailed;
				s_storeController.OnPurchaseDeferred += HandlePurchaseDeferred;
				s_storeController.OnPurchasesFetched += HandlePurchasesFetched;
				s_storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
			}

			if (!s_purchasers.Contains(this))
			{
				s_purchasers.Add(this);
			}
			_storeController = s_storeController;
			return s_storeController;
		}

		/// <summary>
		/// Resolve once the store is connected, reconnecting it if needed. Google Play billing can disconnect while
		/// the app is in the background, and IAP does not reconnect on its own.
		/// </summary>
		private Promise<Unit> EnsureConnected()
		{
			if (s_storeController.GetConnectionState() == ConnectionState.Connected)
			{
				return PromiseBase.SuccessfulUnit;
			}

			if (_connectPromise == null || _connectPromise.IsCompleted)
			{
				_connectPromise = new Promise<Unit>();
				InAppPurchaseLogger.Log("Store is not connected. Reconnecting before the purchase.");
				s_storeController.Connect();
			}
			return _connectPromise;
		}

		private void CompleteConnect(string error)
		{
			var promise = _connectPromise;
			if (promise == null || promise.IsCompleted) return;
			_connectPromise = null;

			if (error == null)
			{
				promise.CompleteSuccess(PromiseBase.Unit);
			}
			else
			{
				promise.CompleteError(new ErrorCode((int)PurchaseFailureReason.PurchasingUnavailable, GameSystem.GAME_CLIENT,
				                                    $"{PurchaseFailureReason.PurchasingUnavailable} ({error})"));
			}
		}

		/// <summary>
		/// Fetch the products of every registered purchaser. A fetch requested while another one runs is queued,
		/// because IAP rejects concurrent fetches.
		/// </summary>
		private static void RequestProducts()
		{
			if (s_fetchInProgress)
			{
				s_refetchRequested = true;
				return;
			}

			var definitions = new List<ProductDefinition>();
			var ids = new HashSet<string>();
			foreach (var purchaser in s_purchasers)
			{
				if (purchaser._productDefinitions == null) continue;
				foreach (var definition in purchaser._productDefinitions)
				{
					if (ids.Add(definition.id)) definitions.Add(definition);
				}
			}
			if (definitions.Count == 0) return;

			s_fetchInProgress = true;
			s_storeController.FetchProducts(definitions);
		}

		private static void RequestQueuedProducts()
		{
			if (!s_refetchRequested) return;
			s_refetchRequested = false;
			RequestProducts();
		}

		private static bool AnyProductAvailable()
		{
			foreach (var product in s_storeController.GetProducts())
			{
				if (product.availableToPurchase) return true;
			}
			return false;
		}

		/// <summary>
		/// Build the store product definitions from the Beamable SKUs, skipping SKUs without a product id for the
		/// current store. Such a product can never match the store, and a missing product fails the fetch.
		/// </summary>
		private static List<ProductDefinition> BuildProductDefinitions(IEnumerable<SKU> skus)
		{
			var definitions = new List<ProductDefinition>();
			foreach (var sku in skus)
			{
				if (sku == null) continue;

				var storeSpecificId = GetStoreSpecificId(sku);
				if (string.IsNullOrEmpty(storeSpecificId))
				{
					Debug.LogWarning($"Beamable IAP skips SKU '{sku.name}': it has no product id for {Application.platform}.");
					continue;
				}
				definitions.Add(new ProductDefinition(sku.name, storeSpecificId, ProductType.Consumable));
			}
			return definitions;
		}

		// Pick the store-specific product id for the current runtime platform from the Beamable SKU.
		// v5's ProductDefinition carries a single store id, unlike v4's multi-store IDs.
		private static string GetStoreSpecificId(SKU sku)
		{
#if USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR)
			// An unmapped SKU falls back to sku.name — the net effect is the same as v4 (which omitted SKUs
			// lacking a steam id), because SteamStoreV5.OnRetrieved matches Steam products by the Beamable SKU
			// name and simply yields no ProductDescription.
			var steamId = sku.productIds?.steam;
			return string.IsNullOrEmpty(steamId) ? sku.name : steamId;
#else
			switch (Application.platform)
			{
				case RuntimePlatform.IPhonePlayer:
				case RuntimePlatform.OSXPlayer:
					return sku.productIds?.itunes;
				case RuntimePlatform.Android:
					return sku.productIds?.googleplay;
				default:
					// The editor and other platforms use Unity's fake store, which accepts any product id.
					if (!string.IsNullOrEmpty(sku.productIds?.itunes)) return sku.productIds.itunes;
					if (!string.IsNullOrEmpty(sku.productIds?.googleplay)) return sku.productIds.googleplay;
					return sku.name;
			}
#endif
		}

		/// <summary>
		/// The purchaser whose StartPurchase is buying <paramref name="sku"/>. A null SKU (the store did not say which
		/// product) matches any purchaser with a purchase in the store.
		/// </summary>
		private static UnityBeamablePurchaser FindActivePurchaser(string sku)
		{
			foreach (var purchaser in s_purchasers)
			{
				if (purchaser._storePurchaseStarted && (sku == null || sku == purchaser._activeSku))
				{
					return purchaser;
				}
			}
			return null;
		}

		/// <summary>
		/// The purchaser that handles an order no StartPurchase is waiting for, e.g. one restored at startup.
		/// </summary>
		private static UnityBeamablePurchaser FindAnyPurchaser()
		{
			return s_purchasers.Count > 0 ? s_purchasers[s_purchasers.Count - 1] : null;
		}

		private static long GetOpenTxid(string sku)
		{
			return sku != null && s_openTxids.TryGetValue(sku, out var txid) ? txid : 0;
		}

		private static void ForgetOpenTxid(string sku, long txid)
		{
			if (sku != null && s_openTxids.TryGetValue(sku, out var openTxid) && openTxid == txid)
			{
				s_openTxids.Remove(sku);
			}
		}
#else
		private static long GetOpenTxid(string sku) => 0;

		private static void ForgetOpenTxid(string sku, long txid) { }
#endif

#if BEAMABLE_STEAM_STORE_V5
		// Unity IAP v5 caches store services per store name for the domain lifetime
		// (StoreServiceContainer throws on duplicate registration), so the Steam store instance is a
		// domain-lifetime singleton whose dependency provider must be refreshed on every Initialize.
		private static Steam.SteamStoreV5 _registeredSteamStore;
		private Steam.SteamStoreV5 _steamStore;

		// Register the Beamable Steam custom store with Unity IAP v5 exactly once per domain, and keep a
		// handle so StartPurchase can pass the expected Beamable txid to the store.
		private void RegisterSteamStore()
		{
			if (_registeredSteamStore == null)
			{
				_registeredSteamStore = new Steam.SteamStoreV5(_serviceProvider);
				UnityIAPServices.AddNewCustomStore(new Steam.SteamStoreWrapperV5(_registeredSteamStore));
			}
			else
			{
				_registeredSteamStore.SetProvider(_serviceProvider);
			}
			_steamStore = _registeredSteamStore;
		}
#endif

		#region "IBeamablePurchaser"
		public string GetLocalizedPrice(string skuSymbol)
		{
			var product = GetStoreProduct(skuSymbol);
			return product?.metadata.localizedPriceString ?? "???";
		}

		public bool TryGetLocalizedPrice(string skuSymbol, out string localizedPrice)
		{
			var product = GetStoreProduct(skuSymbol);
			localizedPrice = product?.metadata.localizedPriceString ?? string.Empty;
			return !string.IsNullOrEmpty(localizedPrice);
		}

		/// <summary>
		/// Start a purchase for the given listing using the given SKU.
		/// </summary>
		/// <param name="listingSymbol">Store listing that should be purchased.</param>
		/// <param name="skuSymbol">Platform specific SKU of the item being purchased.</param>
		/// <returns>Promise containing completed transaction.</returns>
		public Promise<CompletedTransaction> StartPurchase(string listingSymbol, string skuSymbol)
		{
			var result = new Promise<CompletedTransaction>();
			if (InitializationStatus != PurchasingInitializationStatus.Success)
			{
				result.CompleteError(InitializationStatus.StatusToErrorCode());
				return result;
			}
			if (_success != null)
			{
				// The callbacks of the purchase in flight would be overwritten, and its promise would never complete.
				result.CompleteError(new ErrorCode((int)PurchaseFailureReason.ExistingPurchasePending, GameSystem.GAME_CLIENT,
				                                   $"{PurchaseFailureReason.ExistingPurchasePending} ({_activeSku})"));
				return result;
			}
			_txid = 0;
			_activeSku = skuSymbol;
			_storePurchaseStarted = false;
			_success = result.CompleteSuccess;
			_fail = result.CompleteError;
			_cancelled = () =>
			{
				result.CompleteError(
				 new ErrorCode(400, GameSystem.GAME_CLIENT, "Purchase Cancelled"));
			};

#if UNITY_PURCHASING_5_OR_NEWER
			EnsureStoreController();
			var storeReady = EnsureConnected();
#else
			var storeReady = PromiseBase.SuccessfulUnit;
#endif
			storeReady.FlatMap(_ =>
			{
				// Check the product before beginning the Beamable transaction, so an unknown SKU doesn't leave one open.
				var product = GetStoreProduct(skuSymbol);
				if (product == null || !product.availableToPurchase)
				{
					return Promise<PurchaseResponse>.Failed(new ErrorCode((int)PurchaseFailureReason.ProductUnavailable, GameSystem.GAME_CLIENT,
					                                                     $"{PurchaseFailureReason.ProductUnavailable} ({skuSymbol})"));
				}
				return GetPaymentService().BeginPurchase(listingSymbol);
			}).FlatMap(rsp =>
			{
				_txid = rsp.txid;
				_storePurchaseStarted = true;
#if UNITY_PURCHASING_5_OR_NEWER
				// v5 has no developer-payload argument; the in-flight purchase is tracked via _txid
				// and, for orders that arrive after this purchase settles, via s_openTxids.
				s_openTxids[skuSymbol] = _txid;
#if BEAMABLE_STEAM_STORE_V5
				// The Beamable Steam backend initiates the Steam microtransaction with the txid as order
				// id; hand it to the store so only the matching authorization resolves this purchase.
				_steamStore?.SetExpectedTransaction(_txid.ToString());
#endif
				_storeController.PurchaseProduct(GetStoreProduct(skuSymbol));
#else
				_storeController.InitiatePurchase(skuSymbol, _txid.ToString());
#endif
				return PromiseBase.SuccessfulUnit;
			}).Error(err =>
			{
				Debug.LogError($"There was an exception making the begin purchase request: {err}");

				// NoConnectivityException and other exceptions map to the network error code 0.
				var errorCode = err as ErrorCode ?? new ErrorCode(0, GameSystem.GAME_CLIENT, err.Message);
				var txid = _txid;
				if (txid != 0)
				{
					// The Beamable transaction began, but the store purchase could not start.
					ForgetOpenTxid(skuSymbol, txid);
					GetPaymentService().FailPurchase(txid, skuSymbol + ":" + err.Message);
				}
				var fail = _fail;
				ClearCallbacks();
				fail?.Invoke(errorCode);
			});

			return result;
		}
		#endregion

		/// <summary>
		/// Initiate transaction restoration if needed.
		/// </summary>
		public void RestorePurchases()
		{
#if USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR)
			// Steam has no restore flow. This guard also matters on macOS, where
			// Application.platform == OSXPlayer is true for Steam builds and the Apple restore path
			// would otherwise run against the Steam store.
			InAppPurchaseLogger.Log("RestorePurchases skipped: not supported on the Steam store.");
#else
			if (Application.platform == RuntimePlatform.IPhonePlayer ||
				Application.platform == RuntimePlatform.OSXPlayer)
			{
				if (_storeController == null)
				{
					InAppPurchaseLogger.Log("RestorePurchases skipped: the store is not initialized.");
					return;
				}

				InAppPurchaseLogger.Log("RestorePurchases started ...");

				// Begin the asynchronous process of restoring purchases. Expect a confirmation response in
				// the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
#if UNITY_PURCHASING_5_OR_NEWER
				_storeController.RestoreTransactions((result, error) =>
#elif UNITY_PURCHASING_4_6_OR_NEWER
				_appleExtensions.RestoreTransactions((result, error) =>
#else
				_appleExtensions.RestoreTransactions(result =>
#endif
				{
					// The first phase of restoration. If no more responses are received on the pending-purchase
					// callback then no purchases are available to be restored.
					InAppPurchaseLogger.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
#if UNITY_PURCHASING_5_OR_NEWER || UNITY_PURCHASING_4_6_OR_NEWER
					if (!string.IsNullOrWhiteSpace(error))
					{
						InAppPurchaseLogger.Log($"Error: {error}");
					}
#endif
				});
			}
			else
			{
				// If we are not running on an Apple device, no work is necessary to restore purchases.
				InAppPurchaseLogger.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
			}
#endif
		}

		#region "IStoreListener"
#if UNITY_PURCHASING_5_OR_NEWER
		/// <summary>
		/// React to a successful store connection by fetching the configured products.
		/// </summary>
		private static void HandleStoreConnected()
		{
			InAppPurchaseLogger.Log("Store connected. Fetching products.");
			foreach (var purchaser in s_purchasers.ToArray())
			{
				purchaser.CompleteConnect(null);
			}
			RequestProducts();
		}

		/// <summary>
		/// React to a store disconnection. Only fails initialization if it happens before init completes.
		/// </summary>
		private static void HandleStoreDisconnected(StoreConnectionFailureDescription description)
		{
			var message = description?.message ?? "Store disconnected";
			foreach (var purchaser in s_purchasers.ToArray())
			{
				purchaser.CompleteConnect(message);
				if (purchaser.InitializationStatus != PurchasingInitializationStatus.InProgress) continue;

				Debug.LogError($"Billing store disconnected! {message}");
				purchaser._awaitingProducts = false;
				purchaser.InitializationStatus = PurchasingInitializationStatus.ErrorPurchasingUnavailable;
				purchaser._initPromise.CompleteError(new BeamableIAPInitializationException(purchaser.InitializationStatus, message));
			}
			// After initialization, StartPurchase reconnects the store.
			InAppPurchaseLogger.Log($"Billing store disconnected: {message}");
		}

		/// <summary>
		/// React to products being fetched. This is the v5 analog of v4's OnInitialized: the store is
		/// now ready. Fetch outstanding purchases (which drives restore/fulfillment) and resolve init.
		/// </summary>
		private static void HandleProductsFetched(List<Product> products)
		{
			s_fetchInProgress = false;
			InAppPurchaseLogger.Log($"Products fetched: {products?.Count}.");
			CompleteAwaitingPurchasers(null);
			RequestQueuedProducts();
		}

		/// <summary>
		/// React to a product fetch failure. IAP also reports a partial failure right after a successful fetch,
		/// for the products the store doesn't know; purchasing the products it returned still works.
		/// </summary>
		private static void HandleProductsFetchFailed(ProductFetchFailed failure)
		{
			var reason = failure?.FailureReason;
			if (reason != null && reason.Contains("already in progress"))
			{
				// Someone else's fetch (e.g. game code using IAP directly) is running; its result reaches the handlers above.
				InAppPurchaseLogger.Log($"Product fetch skipped: {reason}");
				return;
			}
			s_fetchInProgress = false;

			var failedIds = new List<string>();
			if (failure?.FailedFetchProducts != null)
			{
				foreach (var definition in failure.FailedFetchProducts)
				{
					failedIds.Add($"{definition.id} ({definition.storeSpecificId})");
				}
			}
			Debug.LogWarning($"Beamable IAP could not fetch products [{string.Join(", ", failedIds)}]: {reason}. " +
			                 "Check that the SKUs' product ids match the products configured in the store.");

			CompleteAwaitingPurchasers(AnyProductAvailable() ? null : reason ?? "No products available");
			RequestQueuedProducts();
		}

		/// <summary>
		/// Resolve the initialization of every purchaser waiting for products, and fetch outstanding purchases
		/// once any of them succeeds.
		/// </summary>
		private static void CompleteAwaitingPurchasers(string error)
		{
			var anySucceeded = false;
			foreach (var purchaser in s_purchasers.ToArray())
			{
				if (!purchaser._awaitingProducts) continue;
				purchaser._awaitingProducts = false;

				if (error == null)
				{
					purchaser.InitializationStatus = PurchasingInitializationStatus.Success;
					InAppPurchaseLogger.Log("Successfully initialized IAP.");
					anySucceeded = true;
					if (!purchaser._initPromise.IsCompleted)
					{
						purchaser._initPromise.CompleteSuccess(PromiseBase.Unit);
					}
				}
				else
				{
					purchaser.InitializationStatus = PurchasingInitializationStatus.ErrorNoProductsAvailable;
					InAppPurchaseLogger.Log($"No products available for purchase! {error}");
					if (!purchaser._initPromise.IsCompleted)
					{
						purchaser._initPromise.CompleteError(new BeamableIAPInitializationException(purchaser.InitializationStatus, error));
					}
				}
			}

			if (anySucceeded)
			{
				// Outstanding orders are rerouted through OnPurchasePending. RestoreTransactions isn't needed:
				// all Beamable SKUs are consumables, and on iOS it can prompt for the Apple ID at startup.
				s_storeController.FetchPurchases();
			}
		}

		/// <summary>
		/// Process a pending purchase (new or restored) by fulfilling it through the payments service.
		/// This is the v5 analog of v4's ProcessPurchase. Outstanding purchases fetched at startup are
		/// rerouted here automatically by the StoreController for built-in stores only (Apple/Google/etc.);
		/// custom stores such as Steam are excluded, which is moot because SteamStoreV5.FetchPurchases
		/// reports an empty set (v4 parity — Steam consumables are not restored).
		/// </summary>
		private static void HandlePurchasePending(PendingOrder order)
		{
			var product = GetOrderProduct(order);
			var sku = product?.definition?.id;
			var transactionId = order.Info?.TransactionID;
			if (!string.IsNullOrEmpty(transactionId) && !s_ordersInFulfillment.Add(transactionId))
			{
				InAppPurchaseLogger.Log($"Pending order {transactionId} is already being fulfilled.");
				return;
			}

			var purchaser = FindActivePurchaser(sku) ?? FindAnyPurchaser();
			if (purchaser == null)
			{
				// Leave the order pending; the store delivers it again on the next FetchPurchases.
				Debug.LogWarning($"Beamable IAP has no purchaser to fulfill the pending order for '{sku}'.");
				if (!string.IsNullOrEmpty(transactionId)) s_ordersInFulfillment.Remove(transactionId);
				return;
			}
			purchaser.FulfillPendingOrder(order, product);
		}

		private void FulfillPendingOrder(PendingOrder order, Product product)
		{
			var sku = product?.definition?.id;
			var isActive = IsActivePurchase(sku);

			string rawReceipt = string.Empty;
			var receiptString = order.Info?.Receipt;
			if (!string.IsNullOrEmpty(receiptString))
			{
				var receipt = JsonUtility.FromJson<UnityPurchaseReceipt>(receiptString);
				rawReceipt = receipt != null && !string.IsNullOrEmpty(receipt.Payload) ? receipt.Payload : receiptString;
				InAppPurchaseLogger.Log($"UnityIAP Payload: {rawReceipt}");
				InAppPurchaseLogger.Log($"UnityIAP Raw Receipt: {receiptString}");
			}

			var transaction = new CompletedTransaction(
			   isActive ? _txid : GetOpenTxid(sku),
			   rawReceipt,
			   product != null ? product.metadata.localizedPrice.ToString() : string.Empty,
			   product != null ? product.metadata.isoCurrencyCode : string.Empty
			);
			FulfillTransaction(transaction, order, isActive ? _success : null, isActive ? _fail : null);
		}

		/// <summary>
		/// React to a confirmed purchase. Success is already reported from FulfillTransaction.
		/// </summary>
		private static void HandlePurchaseConfirmed(Order order)
		{
			InAppPurchaseLogger.Log("Purchase confirmed.");
		}

		/// <summary>
		/// React to a failed purchase from Unity IAP v5.
		/// </summary>
		private static void HandlePurchaseFailed(FailedOrder failedOrder)
		{
			var product = GetOrderProduct(failedOrder);
			var purchaser = FindActivePurchaser(product?.definition?.id) ?? FindAnyPurchaser();
			if (purchaser == null)
			{
				InAppPurchaseLogger.Log($"Purchase failed with no purchaser to report it: {failedOrder.FailureReason}");
				return;
			}
			var productId = product?.definition?.storeSpecificId;
			purchaser.OnPurchaseFailedInternal(product, failedOrder.FailureReason, new OptionalString(failedOrder.Details), new OptionalString(productId));
		}

		/// <summary>
		/// React to a purchase the store deferred until the payment is approved (Google Play pending payment
		/// methods, Apple Ask to Buy). The Beamable transaction stays open: when the payment is approved the store
		/// delivers the order through OnPurchasePending, which completes it with the txid kept in s_openTxids.
		/// </summary>
		private static void HandlePurchaseDeferred(DeferredOrder order)
		{
			var sku = GetOrderProduct(order)?.definition?.id;
			InAppPurchaseLogger.Log($"Purchase deferred: {sku}");

			var purchaser = FindActivePurchaser(sku);
			if (purchaser == null) return;

			var fail = purchaser._fail;
			purchaser.ClearCallbacks();
			fail?.Invoke(new ErrorCode(PURCHASE_DEFERRED_ERROR_CODE, GameSystem.GAME_CLIENT,
			                           $"Purchase deferred, awaiting payment approval ({sku})"));
		}

		/// <summary>
		/// React to fetched purchases. For built-in stores, outstanding pending orders are automatically
		/// rerouted through OnPurchasePending by the StoreController, so this is informational only.
		/// (Custom stores are not rerouted; the Steam store reports an empty set here.)
		/// </summary>
		private static void HandlePurchasesFetched(Orders orders)
		{
			InAppPurchaseLogger.Log($"Purchases fetched. Pending: {orders?.PendingOrders?.Count}, Confirmed: {orders?.ConfirmedOrders?.Count}.");
		}

		/// <summary>
		/// React to a failure to fetch outstanding purchases. They are fetched again on the next initialization.
		/// </summary>
		private static void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
		{
			InAppPurchaseLogger.Log($"Fetching purchases failed: {failure?.FailureReason} {failure?.message}");
		}

		/// <summary>
		/// Resolve the Product associated with an order via its cart.
		/// </summary>
		private static Product GetOrderProduct(Order order)
		{
			var items = order?.CartOrdered?.Items();
			if (items == null || items.Count == 0) return null;
			return items[0].Product;
		}
#else
		/// <summary>
		/// React to successful Unity IAP initialization.
		/// </summary>
		/// <param name="controller"></param>
		/// <param name="extensions"></param>
		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			InitializationStatus = PurchasingInitializationStatus.Success;
			InAppPurchaseLogger.Log("Successfully initialized IAP.");
			_storeController = controller;
#if !USE_STEAMWORKS || UNITY_EDITOR
			_appleExtensions = extensions.GetExtension<IAppleExtensions>();
			_googleExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();
#endif
			RestorePurchases();
			_initPromise.CompleteSuccess(PromiseBase.Unit);
		}

		/// <summary>
		/// Handle failed initialization by logging the error.
		/// </summary>
		public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, string.Empty);

		public void OnInitializeFailed(InitializationFailureReason error, string message)
		{
			Debug.LogError($"Billing failed to initialize! {message}");
			switch (error)
			{
				case InitializationFailureReason.AppNotKnown:
					InitializationStatus = PurchasingInitializationStatus.ErrorAppNotKnown;
					InAppPurchaseLogger.Log("Is your App correctly uploaded on the relevant publisher console?");
					break;
				case InitializationFailureReason.PurchasingUnavailable:
					InitializationStatus = PurchasingInitializationStatus.ErrorPurchasingUnavailable;
					InAppPurchaseLogger.Log("Billing disabled!");
					break;
				case InitializationFailureReason.NoProductsAvailable:
					InitializationStatus = PurchasingInitializationStatus.ErrorNoProductsAvailable;
					InAppPurchaseLogger.Log("No products available for purchase!");
					break;
				default:
					InitializationStatus = PurchasingInitializationStatus.ErrorUnknown;
					InAppPurchaseLogger.Log($"Unknown billing error: '{error}'");
					break;
			}
			_initPromise.CompleteError(new BeamableIAPInitializationException(error));

		}

		/// <summary>
		/// Process a completed purchase by fulfilling it.
		/// </summary>
		/// <param name="args">Purchase event information from Unity IAP</param>
		/// <returns>Successful or failed result of processing this purchase</returns>
		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
		{
			string rawReceipt;
			if (args.purchasedProduct.hasReceipt)
			{
				var receipt = JsonUtility.FromJson<UnityPurchaseReceipt>(args.purchasedProduct.receipt);
				rawReceipt = receipt.Payload;
				InAppPurchaseLogger.Log($"UnityIAP Payload: {receipt.Payload}");
				InAppPurchaseLogger.Log($"UnityIAP Raw Receipt: {args.purchasedProduct.receipt}");
			}
			else
			{
				rawReceipt = args.purchasedProduct.receipt;
			}

			// Products restored at startup, or delivered while another purchase is in flight, are fulfilled
			// without a txid and must not complete the active purchase's promise.
			var isActive = IsActivePurchase(args.purchasedProduct.definition.id);
			var transaction = new CompletedTransaction(
			   isActive ? _txid : 0,
			   rawReceipt,
			   args.purchasedProduct.metadata.localizedPrice.ToString(),
			   args.purchasedProduct.metadata.isoCurrencyCode
			);
			FulfillTransaction(transaction, args.purchasedProduct, isActive ? _success : null, isActive ? _fail : null);

			return PurchaseProcessingResult.Pending;
		}

		/// <summary>
		/// Handle a purchase failure event from Unity IAP.
		/// This method is used for IAP integrations prior to 4.x.
		///
		/// The new callback is <see cref="OnPurchaseFailed(UnityEngine.Purchasing.Product,UnityEngine.Purchasing.Extension.PurchaseFailureDescription)"/>
		/// </summary>
		/// <param name="product">The product whose purchase was attempted</param>
		/// <param name="failureReason">Information about why the purchase failed</param>
		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			OnPurchaseFailedInternal(product, failureReason, new OptionalString(), new OptionalString());
		}


#if UNITY_PURCHASING_4_6_OR_NEWER
		/// <summary>
		/// Handle a purchase failure event from Unity IAP.
		/// This method is used for IAP integrations using 4.x. and above
		/// </summary>
		/// <param name="product">The product whose purchase was attempted</param>
		/// <param name="failureDescription">Information about why the purchase failed</param>
		public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
		{
			OnPurchaseFailedInternal(product, failureDescription.reason, new OptionalString(failureDescription.message), new OptionalString(failureDescription.productId));
		}
#endif
#endif // UNITY_PURCHASING_5_OR_NEWER

		private void OnPurchaseFailedInternal(Product product, PurchaseFailureReason failureReason, OptionalString message, OptionalString productId)
		{
			var sku = product?.definition?.id;
			var storeProductId = product?.definition?.storeSpecificId ?? productId?.Value ?? "unknown";

			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing
			// this reason with the user to guide their troubleshooting actions.
			InAppPurchaseLogger.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1} Message: {2}, ProductId: {3}",
												  storeProductId, failureReason, message?.Value, productId?.Value));

			// When the store doesn't say which product failed, it is the purchase in flight.
			var isActive = IsActivePurchase(sku) || (sku == null && _storePurchaseStarted);
			var txid = isActive ? _txid : GetOpenTxid(sku);
			ForgetOpenTxid(sku, txid);

			var paymentService = GetPaymentService();
			if (txid != 0)
			{
				if (failureReason == PurchaseFailureReason.UserCancelled)
				{
					paymentService.CancelPurchase(txid);
				}
				else
				{
					paymentService.FailPurchase(txid, storeProductId + ":" + failureReason);
				}
			}

			if (!isActive) return;

			var fail = _fail;
			var cancelled = _cancelled;
			ClearCallbacks();
			if (failureReason == PurchaseFailureReason.UserCancelled)
			{
				cancelled?.Invoke();
			}
			else
			{
				var errorCode = new ErrorCode((int)failureReason, GameSystem.GAME_CLIENT,
				   failureReason.ToString() + $" ({storeProductId})");
				fail?.Invoke(errorCode);
			}
		}
		#endregion

#if UNITY_PURCHASING_5_OR_NEWER
		/// <summary>
		/// Finish the order in the store, so it is not delivered again.
		/// </summary>
		private void ConfirmStoreOrder(PendingOrder order, CompletedTransaction transaction)
		{
			_storeController.ConfirmPurchase(order);
			var transactionId = order.Info?.TransactionID;
			if (!string.IsNullOrEmpty(transactionId)) s_ordersInFulfillment.Remove(transactionId);
			ForgetOpenTxid(GetOrderProduct(order)?.definition?.id, transaction.Txid);
		}

		/// <summary>
		/// Leave the order pending in the store, so it is fulfilled again on the next FetchPurchases.
		/// </summary>
		private static void ReleaseStoreOrder(PendingOrder order)
		{
			var transactionId = order.Info?.TransactionID;
			if (!string.IsNullOrEmpty(transactionId)) s_ordersInFulfillment.Remove(transactionId);
		}
#else
		private void ConfirmStoreOrder(Product product, CompletedTransaction transaction)
		{
			_storeController.ConfirmPendingPurchase(product);
		}

		private static void ReleaseStoreOrder(Product product) { }
#endif

		/// <summary>
		/// Fulfill a completed transaction by completing the purchase in the
		/// payments service and informing Unity IAP of completion.
		/// </summary>
		/// <param name="transaction">Completed IAP transaction</param>
#if UNITY_PURCHASING_5_OR_NEWER
		/// <param name="purchasedOrder">The pending order being fulfilled</param>
#else
		/// <param name="purchasedOrder">The product being purchased</param>
#endif
		/// <param name="onSuccess">Callback of the StartPurchase this order completes, or null for other orders</param>
		/// <param name="onFail">Callback of the StartPurchase this order completes, or null for other orders</param>
#if UNITY_PURCHASING_5_OR_NEWER
		private void FulfillTransaction(CompletedTransaction transaction, PendingOrder purchasedOrder,
#else
		private void FulfillTransaction(CompletedTransaction transaction, Product purchasedOrder,
#endif
		                                Action<CompletedTransaction> onSuccess, Action<ErrorCode> onFail)
		{
			GetPaymentService().CompletePurchase(transaction).Then(_ =>
			{
				ConfirmStoreOrder(purchasedOrder, transaction);
				ReleaseCallbacks(onSuccess);
				onSuccess?.Invoke(transaction);
			}).Error(ex =>
			{
				Debug.LogError($"There was an exception making the complete purchase request: {ex}");
				var err = ex as ErrorCode;

				if (err == null)
				{
					var platformException = ex as PlatformRequesterException;
					if (platformException != null)
					{
						err = new ErrorCode(platformException.Error);
					}
					else
					{
						// Unknown error: keep the order pending so it is fulfilled later, but don't leave the purchase hanging.
						ReleaseStoreOrder(purchasedOrder);
						ReleaseCallbacks(onSuccess);
						onFail?.Invoke(new ErrorCode(0, GameSystem.GAME_CLIENT, ex.Message));
						return;
					}
				}

				var retryable = err.Code >= 500 || err.Code == 429 || err.Code == 0;   // Server error or rate limiting or network error
				if (retryable)
				{
					GetCoroutineService().StartCoroutine(RetryTransaction(transaction, purchasedOrder, onSuccess, onFail));
				}
				else
				{
					ConfirmStoreOrder(purchasedOrder, transaction);
					ReleaseCallbacks(onSuccess);
					onFail?.Invoke(err);
				}
			});
		}

		/// <summary>
		/// If fulfillment failed, retry fulfillment with a backoff, as a coroutine.
		/// </summary>
		/// <param name="transaction">The failed transaction</param>
#if UNITY_PURCHASING_5_OR_NEWER
		/// <param name="purchasedOrder">The pending order that should have been fulfilled</param>
		private IEnumerator RetryTransaction(CompletedTransaction transaction, PendingOrder purchasedOrder,
#else
		/// <param name="purchasedOrder">The product that should have been fulfilled</param>
		private IEnumerator RetryTransaction(CompletedTransaction transaction, Product purchasedOrder,
#endif
		                                     Action<CompletedTransaction> onSuccess, Action<ErrorCode> onFail)
		{
			// This block should only be hit when the error returned from the request is retryable. This lives down here
			// because C# doesn't allow you to yield return from inside a try..catch block.
			var waitTime = RETRY_DELAYS[Math.Min(transaction.Retries, RETRY_DELAYS.Length - 1)];
			InAppPurchaseLogger.Log($"Got a retryable error from platform. Retrying complete purchase request in {waitTime} seconds.");

			// Avoid incrementing the backoff if the device is definitely not connected to the network at all.
			// This is narrow, and would still increment if the device is connected, but the internet has other problems

			if (Application.internetReachability != NetworkReachability.NotReachable)
			{
				transaction.Retries += 1;
			}

			yield return new WaitForSeconds(waitTime);

			FulfillTransaction(transaction, purchasedOrder, onSuccess, onFail);
		}
	}
#endif
	public struct SkuIdPair
	{
		/// <summary>
		/// The product id from Beamable's SKU content. <see cref="SKU.productIds"/>
		/// </summary>
		public string skuProductId;

		/// <summary>
		/// The IAP store id
		/// </summary>
		public string storeId;
	}

	[BeamContextSystem]
	public static class UnityBeamablePurchaserRegister
	{
		[RegisterBeamableDependencies]
		public static void RegisterServices(IDependencyBuilder builder)
		{
			builder.RemoveIfExists<IBeamablePurchaser>();
#if !BEAMABLE_PURCHASING_IMPLEMENTATION_DISABLED
			builder.AddSingleton<IBeamablePurchaser, UnityBeamablePurchaser>();
#endif
		}
	}

#if !UNITY_PURCHASING_5_OR_NEWER // IDs is a v4-only type; v5 builds ProductDefinitions directly.
	public class UnityBeamablePurchaserUtil
	{
		/// <summary>
		/// Create a <see cref="IDs"/> type from a list of Beamable SKU product id and IAP store ids.
		/// This method will only add the beamable SKU product ids that are not null or empty strings.
		/// </summary>
		/// <param name="skuIdPairs"></param>
		/// <returns></returns>
		public static IDs CreateIdsFromSku(params SkuIdPair[] skuIdPairs)
		{
			var ids = new IDs();

			foreach (var pair in skuIdPairs)
			{
				if (string.IsNullOrEmpty(pair.skuProductId)) continue;
				ids.Add(pair.skuProductId, pair.storeId);
			}

			return ids;
		}
	}
#endif

	public class BeamableIAPInitializationException : Exception
	{
#if UNITY_PURCHASING_5_OR_NEWER
		/// <summary>
		/// The Beamable initialization status at the time of failure. Unity IAP v5 surfaces
		/// initialization problems as a status + message rather than the legacy
		/// <c>InitializationFailureReason</c> enum, so v5 throws through this constructor.
		/// </summary>
		public PurchasingInitializationStatus Status { get; }

		public BeamableIAPInitializationException(PurchasingInitializationStatus status, string message) : base(
			$"Beamable IAP failed due to: {status} ({message})")
		{
			Status = status;
		}
#else
		public InitializationFailureReason Reason { get; }

		public BeamableIAPInitializationException(InitializationFailureReason reason) : base(
			$"Beamable IAP failed due to: {reason}")
		{
			Reason = reason;
		}
#endif
	}


	[Serializable]
	public class UnityPurchaseReceipt
	{
		public string Store;
		public string TransactionID;
		public string Payload;
	}
}
