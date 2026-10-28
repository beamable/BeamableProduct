#if USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR)
// BEAMABLE_STEAM_IN_EDITOR is an opt-in testing hook that forces the Steam store path
// in playmode so a mock ISteamService can drive the full purchase flow.
#define BEAMABLE_STEAM_STORE
#endif
#if BEAMABLE_STEAM_STORE && UNITY_PURCHASING_5_OR_NEWER && !BEAMABLE_STEAM_IMPLEMENTATION_DISABLED
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
using System.Globalization;
using UnityEngine;
#if !BEAMABLE_PURCHASING_IMPLEMENTATION_DISABLED
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#if UNITY_PURCHASING_5_OR_NEWER
using UnityEngine.Purchasing.Security;
// The store's handle on a purchase awaiting fulfillment: a pending order in v5, the purchased product in v4.
using StoreOrder = UnityEngine.Purchasing.PendingOrder;
#else
using StoreOrder = UnityEngine.Purchasing.Product;
#endif
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

		/// <summary>
		/// The purchase a <see cref="StartPurchase"/> call is driving.
		/// </summary>
		private sealed class ActivePurchase
		{
			public string Sku;
			public long Txid;
			// Whether the store was asked to buy the SKU yet.
			public bool StoreStarted;
			public readonly Promise<CompletedTransaction> Result = new Promise<CompletedTransaction>();
		}

		static readonly int[] RETRY_DELAYS = { 1, 2, 5, 10, 20 }; // TODO: Just double a few times. ~ACM 2021-03-10

		private Promise<Unit> _initPromise = new Promise<Unit>();
		// Incremented by every Initialize, so work started for an earlier login can tell it is stale.
		private int _generation;
		private bool _disposed;
		private ActivePurchase _purchase;
		private IDependencyProvider _serviceProvider;

		#region "IBeamablePurchaser"
		public Promise<Unit> Initialize(IDependencyProvider provider)
		{
			if (InitializationStatus == PurchasingInitializationStatus.InProgress)
			{
				return _initPromise;
			}
			_generation++;
			// Settle a purchase left in flight, so it can't block new purchases. If its store order arrives later,
			// it is fulfilled in the background.
			Fail(_purchase, ClientError(409, "Purchasing was initialized again; the purchase in flight continues in the background."));
			_initPromise = new Promise<Unit>();
			InitializationStatus = PurchasingInitializationStatus.InProgress;
			_serviceProvider = provider;

			return GetPaymentService().GetSKUs().Recover(e =>
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
				if (rsp.skus.definitions.Count == 0)
				{
					InitializationStatus = PurchasingInitializationStatus.CancelledNoSkusConfigured;
					// If there are no SKUs available, we will short-circuit the rest of the init-flow.
					// Most importantly, we don't initialize the store, so that we don't receive the Purchase Finished callbacks.
					_initPromise.CompleteSuccess(PromiseBase.Unit);
					return _initPromise;
				}
				return InitializeStore(rsp.skus.definitions);
			});
		}

		public Promise OnDispose()
		{
			_disposed = true;
			UnregisterFromStore();
			Fail(_purchase, ClientError(409, "The purchasing context was disposed during the purchase."));
			return Promise.Success;
		}

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
			if (InitializationStatus != PurchasingInitializationStatus.Success)
			{
				return FailedPurchase(InitializationStatus.StatusToErrorCode());
			}
			if (_purchase != null)
			{
				// Starting another purchase would orphan the one in flight, whose promise would never complete.
				return FailedPurchase(StoreError(PurchaseFailureReason.ExistingPurchasePending, _purchase.Sku));
			}

			var purchase = new ActivePurchase { Sku = skuSymbol };
			if (TryResumeRetainedOrder(purchase))
			{
				return purchase.Result;
			}
			_purchase = purchase;

			PrepareStoreForPurchase().FlatMap(_ =>
			{
				// Check the product before beginning the Beamable transaction, so an unknown SKU doesn't leave one open.
				var product = GetStoreProduct(skuSymbol);
				if (product == null || !product.availableToPurchase)
				{
					return Promise<PurchaseResponse>.Failed(StoreError(PurchaseFailureReason.ProductUnavailable, skuSymbol));
				}
				return GetPaymentService().BeginPurchase(listingSymbol);
			}).FlatMap(rsp =>
			{
				purchase.Txid = rsp.txid;
				purchase.StoreStarted = true;
				StartStorePurchase(purchase);
				return PromiseBase.SuccessfulUnit;
			}).Error(err =>
			{
				Debug.LogError($"There was an exception making the begin purchase request: {err}");
				if (purchase.Txid != 0)
				{
					// The Beamable transaction began, but the store purchase could not start.
					ForgetOpenTxid(skuSymbol, purchase.Txid);
					GetPaymentService().FailPurchase(purchase.Txid, skuSymbol + ":" + err.Message);
				}
				// NoConnectivityException and other exceptions map to the network error code 0.
				Fail(purchase, err as ErrorCode ?? ClientError(0, err.Message));
			});

			return purchase.Result;
		}

		/// <summary>
		/// Initiate transaction restoration if needed. On Apple devices this first retries the fulfillment of
		/// retained orders, which may refresh the app receipt and prompt the player for their Apple ID.
		/// </summary>
		public void RestorePurchases()
		{
#if BEAMABLE_STEAM_STORE
			// Steam has no restore flow. This guard also matters on macOS, where
			// Application.platform == OSXPlayer is true for Steam builds and the Apple restore path
			// would otherwise run against the Steam store.
			InAppPurchaseLogger.Log("RestorePurchases skipped: not supported on the Steam store.");
#else
			if (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.OSXPlayer)
			{
				// If we are not running on an Apple device, no work is necessary to restore purchases.
				InAppPurchaseLogger.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
				return;
			}
			if (_storeController == null)
			{
				InAppPurchaseLogger.Log("RestorePurchases skipped: the store is not initialized.");
				return;
			}
			if (RetryRetainedOrders())
			{
				return;
			}

			// Begin the asynchronous process of restoring purchases. Expect a confirmation response in
			// OnTransactionsRestored, and a pending purchase for every previously purchased product to restore.
			InAppPurchaseLogger.Log("RestorePurchases started ...");
			RestoreStoreTransactions();
#endif
		}
		#endregion

		private static void OnTransactionsRestored(bool result, string error)
		{
			// The first phase of restoration. If no more responses are received on the pending-purchase
			// callback then no purchases are available to be restored.
			InAppPurchaseLogger.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
			if (!string.IsNullOrWhiteSpace(error))
			{
				InAppPurchaseLogger.Log($"Error: {error}");
			}
		}

		/// <summary>
		/// Complete the purchase's promise, and stop tracking it as the purchase in flight. Does nothing for orders
		/// no StartPurchase is waiting for, whose purchase is null.
		/// </summary>
		private void Succeed(ActivePurchase purchase, CompletedTransaction transaction)
		{
			if (purchase == null) return;
			if (_purchase == purchase) _purchase = null;
			if (!purchase.Result.IsCompleted) purchase.Result.CompleteSuccess(transaction);
		}

		/// <inheritdoc cref="Succeed"/>
		private void Fail(ActivePurchase purchase, ErrorCode error)
		{
			if (purchase == null) return;
			if (_purchase == purchase) _purchase = null;
			if (!purchase.Result.IsCompleted) purchase.Result.CompleteError(error);
		}

		private static Promise<CompletedTransaction> FailedPurchase(ErrorCode error)
		{
			var result = new Promise<CompletedTransaction>();
			result.CompleteError(error);
			return result;
		}

		private static ErrorCode ClientError(long code, string message) => new ErrorCode(code, GameSystem.GAME_CLIENT, message);

		private static ErrorCode StoreError(PurchaseFailureReason reason, string detail) => ClientError((int)reason, $"{reason} ({detail})");

		/// <summary>
		/// True when <paramref name="sku"/> is the product the store is buying for this purchaser's StartPurchase.
		/// </summary>
		private bool IsActivePurchase(string sku) => _purchase != null && _purchase.StoreStarted && sku != null && sku == _purchase.Sku;

		/// <summary>
		/// True while this purchaser still serves the login that started work in <paramref name="generation"/>.
		/// </summary>
		private bool IsCurrent(int generation) => generation == _generation && !_disposed && IsRegisteredWithStore;

		private PaymentService GetPaymentService()
		{
			return _serviceProvider.GetService<PaymentService>();
		}

		private CoroutineService GetCoroutineService()
		{
			return _serviceProvider.GetService<CoroutineService>();
		}

		// Prices go to the backend as text, so they must not depend on the device's culture (e.g. "4,99").
		private static string FormatPrice(Product product) =>
			product?.metadata == null ? string.Empty : product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture);

		private void OnPurchaseFailedInternal(Product product, PurchaseFailureReason failureReason, OptionalString message, OptionalString productId)
		{
			var sku = product?.definition?.id;
			var storeProductId = product?.definition?.storeSpecificId ?? productId?.Value ?? "unknown";

			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing
			// this reason with the user to guide their troubleshooting actions.
			InAppPurchaseLogger.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1} Message: {2}, ProductId: {3}",
												  storeProductId, failureReason, message?.Value, productId?.Value));

			// When the store doesn't say which product failed, it is the purchase in flight.
			var purchase = IsActivePurchase(sku) || (sku == null && _purchase?.StoreStarted == true) ? _purchase : null;
			var txid = purchase?.Txid ?? GetOpenTxid(sku);
			ForgetOpenTxid(sku, txid);

			if (txid != 0)
			{
				if (failureReason == PurchaseFailureReason.UserCancelled)
				{
					GetPaymentService().CancelPurchase(txid);
				}
				else
				{
					GetPaymentService().FailPurchase(txid, storeProductId + ":" + failureReason);
				}
			}

			Fail(purchase, failureReason == PurchaseFailureReason.UserCancelled
				? ClientError(400, "Purchase Cancelled")
				: StoreError(failureReason, storeProductId));
		}

		/// <summary>
		/// Stop fulfilling an order without confirming it with the store. The store delivers it again later, so the
		/// player keeps the purchase they paid for; confirming an unfulfilled order would consume it without a reward.
		/// </summary>
		private void StopFulfillment(StoreOrder order, ActivePurchase purchase, ErrorCode error)
		{
			ReleaseStoreOrder(order);
			Debug.LogWarning($"Beamable IAP fulfillment pending: storeTx={GetStoreTransactionId(order)}, code={error.Code}. The order was not confirmed with the store.");
			Fail(purchase, error);
		}

		/// <summary>
		/// Fulfill a completed transaction by completing the purchase in the
		/// payments service and informing Unity IAP of completion.
		/// </summary>
		/// <param name="transaction">Completed IAP transaction</param>
		/// <param name="order">The store order being fulfilled</param>
		/// <param name="purchase">The StartPurchase this order completes, or null for other orders</param>
		private void FulfillTransaction(CompletedTransaction transaction, StoreOrder order, ActivePurchase purchase)
		{
			var generation = _generation;
			Action<Exception> failed = ex =>
			{
				if (!IsCurrent(generation))
				{
					StopFulfillment(order, purchase, ClientError(409, "Login changed during fulfillment; order left pending."));
					return;
				}

				var platformException = ex as PlatformRequesterException;
				var err = ex as ErrorCode;
				if (err == null && platformException?.Error != null) err = new ErrorCode(platformException.Error);
				var retryable = ex is NoConnectivityException ||
				                (err != null && (err.Code >= 500 || err.Code == 429 || err.Code == 0)); // Server error or rate limiting or network error
				// Never log the exception itself: payment errors can contain the receipt.
				Debug.LogWarning($"Beamable IAP completing the purchase failed: storeTx={GetStoreTransactionId(order)}, " +
				                 $"beamTx={transaction.Txid}, code={err?.Code ?? 0}, type={ex.GetType().Name}.");

				if (platformException?.Error?.error == "MalformedReceiptError" && TryRefreshMalformedReceipt(order))
				{
					return;
				}
				if (retryable && transaction.Retries < RETRY_DELAYS.Length)
				{
					GetCoroutineService().StartCoroutine(RetryTransaction(transaction, order, purchase, generation));
					return;
				}
				StopFulfillment(order, purchase, err ?? ClientError(0, "Purchase fulfillment failed; order left pending."));
			};

			try
			{
				GetPaymentService().CompletePurchase(transaction).Then(_ =>
				{
					ConfirmStoreOrder(order, transaction);
					if (!IsCurrent(generation))
					{
						Fail(purchase, ClientError(409, "Purchase fulfilled for the previous login; refresh that account's inventory."));
						return;
					}
					Succeed(purchase, transaction);
				}).Error(failed);
			}
			catch (Exception ex)
			{
				failed(ex);
			}
		}

		/// <summary>
		/// If fulfillment failed, retry fulfillment with a backoff, as a coroutine. Retries are bounded: after the
		/// last one the order is left pending in the store for a later fulfillment.
		/// </summary>
		private IEnumerator RetryTransaction(CompletedTransaction transaction, StoreOrder order, ActivePurchase purchase, int generation)
		{
			var waitTime = RETRY_DELAYS[Math.Min(transaction.Retries, RETRY_DELAYS.Length - 1)];
			InAppPurchaseLogger.Log($"Got a retryable error from platform. Retrying complete purchase request in {waitTime} seconds.");

			transaction.Retries += 1;
			yield return new WaitForSecondsRealtime(waitTime);

			if (!IsCurrent(generation))
			{
				StopFulfillment(order, purchase, ClientError(409, "Login changed during fulfillment; order left pending."));
				yield break;
			}
			FulfillTransaction(transaction, order, purchase);
		}

#if UNITY_PURCHASING_5_OR_NEWER
		// Unity IAP v5 services are domain-wide singletons: every StoreController shares them, and handlers added
		// through any controller stay attached until removed. So all purchasers (one per BeamContext) share one
		// controller with one set of handlers, and the static handlers below dispatch store events to them.
		private static StoreController s_storeController;
		private static readonly List<UnityBeamablePurchaser> s_purchasers = new List<UnityBeamablePurchaser>();
		private static ProductFetchBatch s_productFetchBatch;
		private static bool s_refetchRequested;
		// Orders being fulfilled, by store transaction id, so an order the store delivers again (e.g. FetchPurchases
		// during another initialization) is not fulfilled twice. Apple orders stay here after a failed fulfillment,
		// so StartPurchase of the same SKU or RestorePurchases can retry them without another charge.
		private static readonly Dictionary<string, OrderFulfillment> s_orders = new Dictionary<string, OrderFulfillment>();
		// Beamable txids of store purchases that started but did not complete, by SKU. An order that arrives later
		// (deferred payment approved, or the context that started it is gone) completes its own transaction.
		private static readonly Dictionary<string, long> s_openTxids = new Dictionary<string, long>();
		private static Promise<string> s_receiptRefresh;

		/// <summary>
		/// One FetchProducts call. IAP reports the products it found, then a failure for the rest, so the batch is
		/// complete once every requested id has one of those outcomes.
		/// </summary>
		private sealed class ProductFetchBatch
		{
			public readonly HashSet<string> RemainingIds = new HashSet<string>();
			// The purchasers whose initialization waits for this batch, with the initialization they started.
			public readonly List<(UnityBeamablePurchaser Purchaser, Promise<Unit> Init)> Waiters =
				new List<(UnityBeamablePurchaser, Promise<Unit>)>();
			public string FailureReason;
			public bool Completing;
		}

		/// <summary>
		/// A store order being fulfilled. The Apple fields drive the receipt recovery.
		/// </summary>
		private sealed class OrderFulfillment
		{
			public PendingOrder Order;
			public Product Product;
			public UnityBeamablePurchaser Owner;
			public int Generation;
			public CompletedTransaction Transaction;
			public ActivePurchase Purchase;
			public bool Busy;
			// Apple only: rewards were granted, the player may be prompted to refresh the receipt, and it was refreshed.
			public bool Fulfilled, AllowRefresh, RefreshAttempted;
		}

		private StoreController _storeController;
		// The store product definitions, built during Initialize and fetched once the store connects.
		private List<ProductDefinition> _productDefinitions;
		// True while this purchaser's initialization waits for the store to return its products.
		private bool _awaitingProducts;
		private Promise<Unit> _connectPromise;

#if BEAMABLE_STEAM_STORE
		// Steam builds report OSXPlayer on macOS, but their orders come from the Steam store.
		private static bool IsApplePlayer => false;
#else
		private static bool IsApplePlayer => Application.platform == RuntimePlatform.IPhonePlayer ||
		                                     Application.platform == RuntimePlatform.OSXPlayer;
#endif

		private bool IsRegisteredWithStore => s_purchasers.Contains(this);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			// With domain reload disabled, statics survive between play sessions, but Unity IAP 5 resets its own
			// service containers at SubsystemRegistration. Release the old controller's handlers so the next
			// session creates a controller on the new services.
			if (s_storeController != null)
			{
				s_storeController.OnStoreConnected -= HandleStoreConnected;
				s_storeController.OnStoreDisconnected -= HandleStoreDisconnected;
				s_storeController.OnProductsFetched -= HandleProductsFetched;
				s_storeController.OnProductsFetchFailed -= HandleProductsFetchFailed;
				s_storeController.OnPurchasePending -= HandlePurchasePending;
				s_storeController.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
				s_storeController.OnPurchaseFailed -= HandlePurchaseFailed;
				s_storeController.OnPurchaseDeferred -= HandlePurchaseDeferred;
				s_storeController.OnPurchasesFetched -= HandlePurchasesFetched;
				s_storeController.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
			}
			s_storeController = null;
			s_purchasers.Clear();
			s_productFetchBatch = null;
			s_refetchRequested = false;
			s_orders.Clear();
			s_openTxids.Clear();
			s_receiptRefresh = null;
#if BEAMABLE_STEAM_STORE_V5
			// The custom store registration is dropped with IAP's containers, so it must be registered again.
			_registeredSteamStore = null;
#endif
		}

		private Promise<Unit> InitializeStore(List<SKU> skus)
		{
			_productDefinitions = BuildProductDefinitions(skus);
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
		}

		private void UnregisterFromStore()
		{
			s_purchasers.Remove(this);
			// Retry coroutines stop with the context, so release this purchaser's orders; the store delivers them
			// again to the next context.
			foreach (var fulfillment in new List<OrderFulfillment>(s_orders.Values))
			{
				if (fulfillment.Owner == this && fulfillment.Busy) ReleaseStoreOrder(fulfillment.Order);
			}
		}

		private Product GetStoreProduct(string skuSymbol) => _storeController?.GetProductById(skuSymbol);

		private static string GetStoreTransactionId(PendingOrder order) => order?.Info?.TransactionID;

		private Promise<Unit> PrepareStoreForPurchase()
		{
			EnsureStoreController();
			return EnsureConnected();
		}

		private void StartStorePurchase(ActivePurchase purchase)
		{
			// v5 has no developer-payload argument; the in-flight purchase is tracked by _purchase
			// and, for orders that arrive after this purchase settles, by s_openTxids.
			s_openTxids[purchase.Sku] = purchase.Txid;
#if BEAMABLE_STEAM_STORE_V5
			// The Beamable Steam backend initiates the Steam microtransaction with the txid as order
			// id; hand it to the store so only the matching authorization resolves this purchase.
			_steamStore?.SetExpectedTransaction(purchase.Txid.ToString());
#endif
			_storeController.PurchaseProduct(GetStoreProduct(purchase.Sku));
		}

		private void RestoreStoreTransactions() => _storeController.RestoreTransactions(OnTransactionsRestored);

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
				var promise = _connectPromise = new Promise<Unit>();
				InAppPurchaseLogger.Log("Store is not connected. Reconnecting before the purchase.");
				s_storeController.Connect();
				// A synchronous store callback can complete the promise and clear the field inside Connect().
				return promise;
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
				promise.CompleteError(StoreError(PurchaseFailureReason.PurchasingUnavailable, error));
			}
		}

		/// <summary>
		/// Fetch the products of every registered purchaser. A fetch requested while another one runs is queued,
		/// because IAP rejects concurrent fetches.
		/// </summary>
		private static void RequestProducts()
		{
			if (s_productFetchBatch != null)
			{
				s_refetchRequested = true;
				return;
			}

			var definitions = new List<ProductDefinition>();
			var batch = new ProductFetchBatch();
			foreach (var purchaser in s_purchasers)
			{
				if (purchaser._productDefinitions == null) continue;
				foreach (var definition in purchaser._productDefinitions)
				{
					if (batch.RemainingIds.Add(definition.id)) definitions.Add(definition);
				}
				if (purchaser._awaitingProducts)
				{
					batch.Waiters.Add((purchaser, purchaser._initPromise));
				}
			}
			if (definitions.Count == 0) return;

			s_productFetchBatch = batch;
			s_storeController.FetchProducts(definitions);
		}

		private static bool AnyProductAvailable(IEnumerable<ProductDefinition> definitions)
		{
			foreach (var definition in definitions)
			{
				var product = s_storeController.GetProductById(definition.id);
				if (product != null && product.availableToPurchase) return true;
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
#if BEAMABLE_STEAM_STORE
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
				var purchase = purchaser._purchase;
				if (purchase != null && purchase.StoreStarted && (sku == null || sku == purchase.Sku))
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

		/// <summary>
		/// Resolve the Product associated with an order via its cart.
		/// </summary>
		private static Product GetOrderProduct(Order order)
		{
			var items = order?.CartOrdered?.Items();
			if (items == null || items.Count == 0) return null;
			return items[0].Product;
		}

		#region "Store events"
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
			var batch = s_productFetchBatch;
			if (batch == null || batch.Completing) return;
			InAppPurchaseLogger.Log($"Products fetched: {products?.Count}.");
			if (products != null)
			{
				foreach (var product in products)
				{
					if (product?.definition != null) batch.RemainingIds.Remove(product.definition.id);
				}
			}
			// IAP reports the missing subset with a failure right after this, so the batch may not be complete yet.
			CompleteProductBatchIfReady(batch);
		}

		/// <summary>
		/// React to a product fetch failure. IAP also reports a partial failure right after a successful fetch,
		/// for the products the store doesn't know; purchasing the products it returned still works.
		/// </summary>
		private static void HandleProductsFetchFailed(ProductFetchFailed failure)
		{
			var batch = s_productFetchBatch;
			if (batch == null || batch.Completing) return;
			var reason = failure?.FailureReason;

			var failedIds = new List<string>();
			var matchedBatch = false;
			if (failure?.FailedFetchProducts != null)
			{
				foreach (var definition in failure.FailedFetchProducts)
				{
					if (definition == null) continue;
					matchedBatch |= batch.RemainingIds.Remove(definition.id);
					failedIds.Add($"{definition.id} ({definition.storeSpecificId})");
				}
			}
			if (failedIds.Count == 0)
			{
				// A failure without product details ends the current batch.
				batch.RemainingIds.Clear();
				matchedBatch = true;
			}
			// A failure for products outside this batch belongs to a fetch Beamable didn't start.
			if (!matchedBatch) return;

			batch.FailureReason = reason ?? "No products available";
			Debug.LogWarning($"Beamable IAP could not fetch products [{string.Join(", ", failedIds)}]: {reason}. " +
			                 "Check that the SKUs' product ids match the products configured in the store.");

			CompleteProductBatchIfReady(batch);
		}

		/// <summary>
		/// Once every product of the batch has an outcome, resolve the initialization of the purchasers waiting for
		/// it, then start the fetch that was queued meanwhile. A purchaser succeeds when any of its own products is
		/// available.
		/// </summary>
		private static void CompleteProductBatchIfReady(ProductFetchBatch batch)
		{
			if (!ReferenceEquals(batch, s_productFetchBatch) || batch.Completing || batch.RemainingIds.Count != 0) return;
			batch.Completing = true;
			// Keep the batch active while promise callbacks run, so a reentrant Initialize queues its fetch.
			try
			{
				var anySucceeded = false;
				foreach (var (purchaser, init) in batch.Waiters)
				{
					if (!s_purchasers.Contains(purchaser) || !purchaser._awaitingProducts ||
					    !ReferenceEquals(purchaser._initPromise, init)) continue;
					purchaser._awaitingProducts = false;

					if (AnyProductAvailable(purchaser._productDefinitions))
					{
						purchaser.InitializationStatus = PurchasingInitializationStatus.Success;
						InAppPurchaseLogger.Log("Successfully initialized IAP.");
						anySucceeded = true;
						if (!init.IsCompleted) init.CompleteSuccess(PromiseBase.Unit);
					}
					else
					{
						var error = batch.FailureReason ?? "No products available";
						purchaser.InitializationStatus = PurchasingInitializationStatus.ErrorNoProductsAvailable;
						InAppPurchaseLogger.Log($"No products available for purchase! {error}");
						if (!init.IsCompleted) init.CompleteError(new BeamableIAPInitializationException(purchaser.InitializationStatus, error));
					}
				}

				if (anySucceeded)
				{
					// Outstanding orders are rerouted through OnPurchasePending. RestoreTransactions isn't needed:
					// all Beamable SKUs are consumables, and on iOS it can prompt for the Apple ID at startup.
					s_storeController.FetchPurchases();
				}
			}
			finally
			{
				s_productFetchBatch = null;
				if (s_refetchRequested)
				{
					s_refetchRequested = false;
					RequestProducts();
				}
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
			OrderFulfillment fulfillment = null;
			if (!string.IsNullOrEmpty(transactionId) && s_orders.TryGetValue(transactionId, out fulfillment) && fulfillment.Busy)
			{
				InAppPurchaseLogger.Log($"Pending order {transactionId} is already being fulfilled.");
				return;
			}

			var purchaser = FindActivePurchaser(sku) ?? FindAnyPurchaser();
			if (purchaser == null)
			{
				// Leave the order pending; the store delivers it again on the next FetchPurchases.
				Debug.LogWarning($"Beamable IAP has no purchaser to fulfill the pending order for '{sku}'.");
				return;
			}
			purchaser.FulfillPendingOrder(order, product, fulfillment);
		}

		/// <param name="fulfillment">The retained Apple fulfillment of this order, if any.</param>
		private void FulfillPendingOrder(PendingOrder order, Product product, OrderFulfillment fulfillment)
		{
			var sku = product?.definition?.id;
			var purchase = IsActivePurchase(sku) ? _purchase : null;
			var txid = purchase?.Txid ?? GetOpenTxid(sku);
			var transactionId = order.Info?.TransactionID;
			var price = FormatPrice(product);
			var currency = product?.metadata?.isoCurrencyCode ?? string.Empty;

			if (IsApplePlayer)
			{
				if (fulfillment == null)
				{
					if (string.IsNullOrEmpty(transactionId) || product?.definition == null)
					{
						StopFulfillment(order, purchase,
						                ClientError(400, "Apple order is missing its transaction or product id; order left pending."));
						return;
					}
					fulfillment = new OrderFulfillment
					{
						Order = order, Product = product, Owner = this, Generation = _generation, Purchase = purchase,
						Transaction = new CompletedTransaction(txid, string.Empty, price, currency)
					};
					s_orders.Add(transactionId, fulfillment);
				}
				if (fulfillment.Owner != this || fulfillment.Generation != _generation)
				{
					Debug.LogWarning("Beamable IAP: a pending Apple order belongs to an earlier login; restart the app to recover it safely.");
					return;
				}
				ProcessAppleOrder(fulfillment, purchase != null);
				return;
			}

			string receipt;
			try
			{
				var envelope = order.Info?.Receipt;
				var payload = string.IsNullOrEmpty(envelope) ? null : JsonUtility.FromJson<UnityPurchaseReceipt>(envelope)?.Payload;
				receipt = (!string.IsNullOrEmpty(payload) ? payload : envelope) ?? string.Empty;
			}
			catch (Exception)
			{
				StopFulfillment(order, purchase, ClientError(400, "Unable to read the store receipt; order left pending."));
				return;
			}
			if (!string.IsNullOrEmpty(transactionId))
			{
				s_orders[transactionId] = new OrderFulfillment { Order = order, Product = product, Owner = this, Generation = _generation, Busy = true };
			}
			FulfillTransaction(new CompletedTransaction(txid, receipt, price, currency), order, purchase);
		}

		/// <summary>
		/// React to a confirmed purchase. Success is already reported from FulfillTransaction. IAP also reports a
		/// failed confirmation here; the Apple order is then retained so a retry confirms it again.
		/// </summary>
		private static void HandlePurchaseConfirmed(Order order)
		{
			var id = order.Info?.TransactionID;
			if (id != null && s_orders.TryGetValue(id, out var fulfillment))
			{
				if (order is FailedOrder)
				{
					fulfillment.Busy = false;
					Debug.LogWarning($"Beamable IAP: confirming the store order failed after fulfillment for {id}; the order is retained.");
					return;
				}
				if (order is ConfirmedOrder)
				{
					s_orders.Remove(id);
				}
			}
			InAppPurchaseLogger.Log($"Purchase confirmation received: {id}.");
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
			purchaser?.Fail(purchaser._purchase, ClientError(PURCHASE_DEFERRED_ERROR_CODE, $"Purchase deferred, awaiting payment approval ({sku})"));
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
		#endregion

		/// <summary>
		/// Finish the order in the store, so it is not delivered again. An Apple order is only forgotten once IAP
		/// reports the confirmation, so a failed confirmation can be retried.
		/// </summary>
		private void ConfirmStoreOrder(PendingOrder order, CompletedTransaction transaction)
		{
			var id = order.Info?.TransactionID;
			OrderFulfillment fulfillment = null;
			if (id != null) s_orders.TryGetValue(id, out fulfillment);
			if (fulfillment != null) fulfillment.Fulfilled = true;
			try
			{
				_storeController.ConfirmPurchase(order);
			}
			catch (Exception)
			{
				if (fulfillment != null) fulfillment.Busy = false;
				Debug.LogWarning($"Beamable IAP: rewards were granted, but confirming the store order {id} failed; it is retained for another confirmation.");
			}
			if (!IsApplePlayer) ReleaseStoreOrder(order);
			ForgetOpenTxid(GetOrderProduct(order)?.definition?.id, transaction.Txid);
		}

		/// <summary>
		/// Stop tracking the order as being fulfilled. The store delivers it again on the next FetchPurchases; an
		/// Apple order is also retained for a retry without another charge.
		/// </summary>
		private static void ReleaseStoreOrder(PendingOrder order)
		{
			var id = order.Info?.TransactionID;
			if (string.IsNullOrEmpty(id) || !s_orders.TryGetValue(id, out var fulfillment)) return;
			if (IsApplePlayer)
			{
				fulfillment.Busy = false;
				fulfillment.Purchase = null;
			}
			else
			{
				s_orders.Remove(id);
			}
		}

		#region "Apple receipt recovery"
		/// <summary>
		/// Apple orders retained after a failed fulfillment, optionally only those of one SKU.
		/// </summary>
		private static List<OrderFulfillment> RetainedAppleOrders(string sku)
		{
			var orders = new List<OrderFulfillment>();
			if (!IsApplePlayer) return orders;
			foreach (var fulfillment in s_orders.Values)
			{
				if (sku == null || fulfillment.Product?.definition?.id == sku) orders.Add(fulfillment);
			}
			return orders;
		}

		private bool CanRetry(OrderFulfillment fulfillment) =>
			fulfillment.Owner == this && fulfillment.Generation == _generation && !fulfillment.Busy;

		/// <summary>
		/// On Apple devices, a StartPurchase of a SKU whose order failed fulfillment retries that order instead of
		/// charging the player again. Returns false when there is no such order.
		/// </summary>
		private bool TryResumeRetainedOrder(ActivePurchase purchase)
		{
			foreach (var fulfillment in RetainedAppleOrders(purchase.Sku))
			{
				if (!CanRetry(fulfillment))
				{
					purchase.Result.CompleteError(ClientError(409,
						"An Apple order is pending recovery. Finish recovery before another purchase; restart after an account change."));
					return true;
				}

				purchase.Txid = fulfillment.Transaction.Txid;
				purchase.StoreStarted = true;
				_purchase = fulfillment.Purchase = purchase;
				fulfillment.RefreshAttempted = false;
				ProcessAppleOrder(fulfillment, true);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Retry the fulfillment of this purchaser's retained Apple orders. Returns false when there are none.
		/// </summary>
		private bool RetryRetainedOrders()
		{
			var retrying = false;
			foreach (var fulfillment in RetainedAppleOrders(null))
			{
				if (!CanRetry(fulfillment)) continue;
				retrying = true;
				fulfillment.RefreshAttempted = false;
				ProcessAppleOrder(fulfillment, true);
			}
			return retrying;
		}

		/// <summary>
		/// Fulfill an Apple order with the app receipt (PKCS#7) that Beamable's backend verifies. The Unity receipt
		/// envelope is never sent. When the receipt is missing or doesn't identify this order unambiguously, it is
		/// refreshed, but only when the player started the purchase or recovery, because refreshing can prompt for
		/// the Apple ID. Otherwise the order is left pending for a later retry.
		/// </summary>
		private void ProcessAppleOrder(OrderFulfillment fulfillment, bool allowRefresh)
		{
			if (fulfillment.Busy) return;
			fulfillment.Busy = true;
			fulfillment.AllowRefresh = allowRefresh;
			if (fulfillment.Fulfilled)
			{
				// Rewards were granted, but confirming the order with the store failed; confirm it again.
				ConfirmStoreOrder(fulfillment.Order, fulfillment.Transaction);
				Succeed(fulfillment.Purchase, fulfillment.Transaction);
				return;
			}

			var receipt = ReadAppleReceipt(fulfillment.Order);
			if (ReceiptContainsOnlyExpectedPurchase(receipt, fulfillment))
			{
				SubmitAppleReceipt(fulfillment, receipt);
			}
			else if (allowRefresh)
			{
				RefreshAndSubmitAppleReceipt(fulfillment);
			}
			else
			{
				StopFulfillment(fulfillment.Order, fulfillment.Purchase, ClientError(400,
					"The Apple receipt needs a player-initiated recovery. Retry this product or restore purchases; the player is not charged again."));
			}
		}

		private void RefreshAndSubmitAppleReceipt(OrderFulfillment fulfillment)
		{
			fulfillment.RefreshAttempted = true;
			RefreshAppleReceipt().Then(receipt =>
			{
				if (!IsCurrent(fulfillment.Generation))
				{
					StopFulfillment(fulfillment.Order, fulfillment.Purchase, ClientError(409, "Login changed during receipt recovery; order left pending."));
				}
				else if (!ReceiptContainsOnlyExpectedPurchase(receipt, fulfillment))
				{
					StopFulfillment(fulfillment.Order, fulfillment.Purchase, ClientError(400,
						"The refreshed Apple receipt is empty, invalid, missing this transaction, or has several purchases of this product; order left pending."));
				}
				else
				{
					SubmitAppleReceipt(fulfillment, receipt);
				}
			}).Error(_ => StopFulfillment(fulfillment.Order, fulfillment.Purchase,
				ClientError(0, "Refreshing the Apple receipt failed or timed out; order left pending. Retry the recovery.")));
		}

		/// <summary>
		/// After the backend rejects an Apple receipt as malformed, retry once with a refreshed receipt if the player
		/// started this fulfillment. Returns true when the retry started.
		/// </summary>
		private bool TryRefreshMalformedReceipt(PendingOrder order)
		{
			var id = order.Info?.TransactionID;
			if (!IsApplePlayer || id == null || !s_orders.TryGetValue(id, out var fulfillment)) return false;
			if (!fulfillment.AllowRefresh || fulfillment.RefreshAttempted || fulfillment.Generation != _generation) return false;

			RefreshAndSubmitAppleReceipt(fulfillment);
			return true;
		}

		private void SubmitAppleReceipt(OrderFulfillment fulfillment, string receipt)
		{
			var previous = fulfillment.Transaction;
			fulfillment.Transaction = new CompletedTransaction(previous.Txid, receipt, previous.PriceInLocalCurrency, previous.IsoCurrencySymbol);
			InAppPurchaseLogger.Log($"Apple receipt ready: appleTx={fulfillment.Order.Info.TransactionID}, beamTx={previous.Txid}, length={receipt.Length}.");
			FulfillTransaction(fulfillment.Transaction, fulfillment.Order, fulfillment.Purchase);
		}

		/// <summary>
		/// The App Store receipt inside Unity's receipt envelope, or null when it is missing or the envelope is malformed.
		/// </summary>
		private static string ReadAppleReceipt(PendingOrder order)
		{
			try
			{
				var envelope = order.Info?.Receipt;
				return string.IsNullOrEmpty(envelope) ? null : JsonUtility.FromJson<UnityPurchaseReceipt>(envelope)?.Payload;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// True when the app receipt contains this order's transaction and no other purchase of the same product.
		/// Beamable's backend matches purchases by product, so an ambiguous receipt could credit the wrong one.
		/// This only checks the format and the match; the backend still verifies the receipt.
		/// </summary>
		private static bool ReceiptContainsOnlyExpectedPurchase(string receipt, OrderFulfillment fulfillment)
		{
			if (string.IsNullOrWhiteSpace(receipt)) return false;
			try
			{
				var parsed = new AppleReceiptParser().Parse(Convert.FromBase64String(receipt));
				if (parsed?.inAppPurchaseReceipts == null) return false;
				var found = false;
				foreach (var entry in parsed.inAppPurchaseReceipts)
				{
					if (!string.Equals(entry.productID, fulfillment.Product.definition.storeSpecificId, StringComparison.Ordinal)) continue;
					if (!string.Equals(entry.transactionID, fulfillment.Order.Info.TransactionID, StringComparison.Ordinal)) return false;
					found = true;
				}
				return found;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private Promise<string> RefreshAppleReceipt()
		{
			// IAP keeps one native callback slot for receipt refreshes, so concurrent requests share one refresh. Even
			// after a timeout, the slot isn't reused until the native refresh returns.
			if (s_receiptRefresh != null) return s_receiptRefresh;
			var promise = new Promise<string>();
			s_receiptRefresh = promise;
			Action<string, bool> finish = (value, success) =>
			{
				if (!ReferenceEquals(s_receiptRefresh, promise)) return;
				s_receiptRefresh = null;
				if (promise.IsCompleted) return;
				if (success) promise.CompleteSuccess(value);
				else promise.CompleteError(new Exception("Apple receipt refresh failed."));
			};
			try
			{
				var apple = _storeController.AppleStoreExtendedPurchaseService;
				if (apple == null) throw new InvalidOperationException("Apple store extension unavailable.");
#pragma warning disable CS0612, CS0618 // Beamable's receipt verification expects the legacy PKCS#7 app receipt, not a StoreKit 2 JWS.
				apple.RefreshAppReceipt(value => finish(value, true), _ => finish(null, false));
#pragma warning restore CS0612, CS0618
				if (!promise.IsCompleted) GetCoroutineService().StartCoroutine(ReceiptRefreshTimeout(promise));
			}
			catch (Exception)
			{
				finish(null, false);
			}
			return promise;
		}

		private static IEnumerator ReceiptRefreshTimeout(Promise<string> promise)
		{
			yield return new WaitForSecondsRealtime(120);
			if (!promise.IsCompleted) promise.CompleteError(new TimeoutException("Apple receipt refresh timed out."));
		}
		#endregion

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
#else
		private IStoreController _storeController;
#pragma warning disable CS0649
		private IAppleExtensions _appleExtensions;
		private IGooglePlayStoreExtensions _googleExtensions;
#pragma warning restore CS0649

		// IAP 4 keeps no shared store state between purchasers, and doesn't retain orders for a retry.
		private bool IsRegisteredWithStore => true;

		private Promise<Unit> InitializeStore(List<SKU> skus)
		{
#if USE_STEAMWORKS && !UNITY_EDITOR
			var builder = ConfigurationBuilder.Instance(new Steam.SteamPurchasingModule(_serviceProvider));
			foreach (var sku in skus)
			{
				if (sku == null) continue;

				var ids = UnityBeamablePurchaserUtil.CreateIdsFromSku(
					new SkuIdPair {skuProductId = sku.productIds?.steam, storeId = Steam.SteamStore.Name,}
				);
				builder.AddProduct(sku.name, ProductType.Consumable, ids);
			}
#else
			var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
			foreach (var sku in skus)
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
		}

		private void UnregisterFromStore() { }

		private Product GetStoreProduct(string skuSymbol) => _storeController?.products.WithID(skuSymbol);

		private static string GetStoreTransactionId(Product order) => order?.transactionID;

		private Promise<Unit> PrepareStoreForPurchase() => PromiseBase.SuccessfulUnit;

		private void StartStorePurchase(ActivePurchase purchase) => _storeController.InitiatePurchase(purchase.Sku, purchase.Txid.ToString());

		private void RestoreStoreTransactions()
		{
#if UNITY_PURCHASING_4_6_OR_NEWER
			_appleExtensions.RestoreTransactions(OnTransactionsRestored);
#else
			_appleExtensions.RestoreTransactions(result => OnTransactionsRestored(result, null));
#endif
		}

		private bool TryResumeRetainedOrder(ActivePurchase purchase) => false;

		private bool RetryRetainedOrders() => false;

		private bool TryRefreshMalformedReceipt(Product order) => false;

		private static long GetOpenTxid(string sku) => 0;

		private static void ForgetOpenTxid(string sku, long txid) { }

		private void ConfirmStoreOrder(Product product, CompletedTransaction transaction) => _storeController.ConfirmPendingPurchase(product);

		private static void ReleaseStoreOrder(Product product) { }

		#region "IStoreListener"
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
			_appleExtensions?.RegisterPurchaseDeferredListener(OnPurchaseDeferred);
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
			if (_googleExtensions != null && _googleExtensions.IsPurchasedProductDeferred(args.purchasedProduct))
			{
				// The payment isn't done yet, so there is nothing to fulfill. Google Play delivers the purchase again
				// once the payment is approved.
				OnPurchaseDeferred(args.purchasedProduct);
				return PurchaseProcessingResult.Pending;
			}

			var rawReceipt = ReadLegacyReceipt(args.purchasedProduct);

			// Products restored at startup, or delivered while another purchase is in flight, are fulfilled
			// without a txid and must not complete the active purchase's promise.
			var purchase = IsActivePurchase(args.purchasedProduct.definition.id) ? _purchase : null;
			var transaction = new CompletedTransaction(
			   purchase?.Txid ?? 0,
			   rawReceipt,
			   FormatPrice(args.purchasedProduct),
			   args.purchasedProduct.metadata.isoCurrencyCode
			);
			FulfillTransaction(transaction, args.purchasedProduct, purchase);

			return PurchaseProcessingResult.Pending;
		}

		/// <summary>
		/// The store receipt inside Unity's receipt envelope, or the envelope itself when it has no payload.
		/// </summary>
		private static string ReadLegacyReceipt(Product product)
		{
			if (product.hasReceipt)
			{
				try
				{
					var receipt = JsonUtility.FromJson<UnityPurchaseReceipt>(product.receipt);
					InAppPurchaseLogger.Log($"UnityIAP Payload: {receipt.Payload}");
					InAppPurchaseLogger.Log($"UnityIAP Raw Receipt: {product.receipt}");
					if (!string.IsNullOrEmpty(receipt.Payload)) return receipt.Payload;
				}
				catch (Exception)
				{
					// A malformed envelope is sent as it is.
				}
			}
			return product.receipt;
		}

		/// <summary>
		/// React to a purchase the store deferred until the payment is approved (Apple Ask to Buy, Google Play
		/// pending payment methods). The store calls ProcessPurchase once the payment is approved.
		/// </summary>
		private void OnPurchaseDeferred(Product product)
		{
			var sku = product?.definition?.id;
			InAppPurchaseLogger.Log($"Purchase deferred: {sku}");
			if (IsActivePurchase(sku))
			{
				Fail(_purchase, ClientError(PURCHASE_DEFERRED_ERROR_CODE, $"Purchase deferred, awaiting payment approval ({sku})"));
			}
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
		#endregion
#endif // UNITY_PURCHASING_5_OR_NEWER
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
