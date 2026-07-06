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
	public class UnityBeamablePurchaser : IBeamablePurchaser

#if !UNITY_PURCHASING_5_OR_NEWER // v5 removed the listener interfaces in favor of StoreController events.
										, IStoreListener

#if UNITY_PURCHASING_4_6_OR_NEWER // if this is a newer IAP, then include the detailed store listener.
	                                    , IDetailedStoreListener
#endif
#endif

	{
		public PurchasingInitializationStatus InitializationStatus { get; private set; } =
			PurchasingInitializationStatus.NotInitialized;

#if UNITY_PURCHASING_5_OR_NEWER
		private StoreController _storeController;
		// The store product definitions, built during Initialize and fetched once the store connects.
		private System.Collections.Generic.List<ProductDefinition> _productDefinitions;
#else
		private IStoreController _storeController;
#pragma warning disable CS0649
		private IAppleExtensions _appleExtensions;
		private IGooglePlayStoreExtensions _googleExtensions;
#pragma warning restore CS0649
#endif

		private Promise<Unit> _initPromise = new Promise<Unit>();
		private long _txid;
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
#if USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR) && !!BEAMABLE_STEAM_IMPLEMENTATION_DISABLED
				// Register the Steam custom store with the SDK, then target it by name.
				// BEAMABLE_STEAM_IN_EDITOR is an opt-in testing hook that forces the Steam store path
				// in playmode so a mock ISteamService can drive the full purchase flow.
				RegisterSteamStore();
				_storeController = new StoreController(Steam.SteamStoreV5.Name);
#else
				_storeController = new StoreController();
#endif

				// Subscribe to the StoreController events before connecting so no callback is missed.
				_storeController.OnStoreConnected += HandleStoreConnected;
				_storeController.OnStoreDisconnected += HandleStoreDisconnected;
				_storeController.OnProductsFetched += HandleProductsFetched;
				_storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
				_storeController.OnPurchasePending += HandlePurchasePending;
				_storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
				_storeController.OnPurchaseFailed += HandlePurchaseFailed;
				_storeController.OnPurchasesFetched += HandlePurchasesFetched;

				var productDefinitions = new System.Collections.Generic.List<ProductDefinition>();
				foreach (var sku in rsp.skus.definitions)
				{
					if (sku == null) continue;

					var storeSpecificId = GetStoreSpecificId(sku);
					if (string.IsNullOrEmpty(storeSpecificId)) storeSpecificId = sku.name;
					productDefinitions.Add(new ProductDefinition(sku.name, storeSpecificId, ProductType.Consumable));
				}
				_productDefinitions = productDefinitions;

				// Kick off the split async v5 lifecycle: Connect -> (HandleStoreConnected) FetchProducts
				// -> (HandleProductsFetched) FetchPurchases. The promise resolves from the event handlers.
				_storeController.Connect();
				return _initPromise;

				// Pick the store-specific product id for the current runtime platform from the Beamable SKU.
				// v5's ProductDefinition carries a single store id, unlike v4's multi-store IDs.
				string GetStoreSpecificId(SKU sku)
				{
#if USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR)
					// Note: unlike v4 (which omitted SKUs lacking a steam id), an unmapped SKU falls back to
					// sku.name at the call site — the net effect is the same, because SteamStoreV5.OnRetrieved
					// matches Steam products by the Beamable SKU name and simply yields no ProductDescription.
					return sku.productIds?.steam;
#else
					switch (Application.platform)
					{
						case RuntimePlatform.IPhonePlayer:
						case RuntimePlatform.OSXPlayer:
							return sku.productIds?.itunes;
						case RuntimePlatform.Android:
							return sku.productIds?.googleplay;
						default:
							return sku.productIds?.itunes ?? sku.productIds?.googleplay;
					}
#endif
				}
#else
#if USE_STEAMWORKS && !UNITY_EDITOR
                var builder = ConfigurationBuilder.Instance(new Steam.SteamPurchasingModule(_serviceProvider));
                foreach (var sku in rsp.skus.definitions)
                {
				   if (sku == null) continue;

                   var ids = ExtractValidIdsFromSku(
						new SkuIdPair {skuProductId = sku.productIds?.steam, storeId = Steam.SteamStore.Name,}
				   );
				   builder.AddProduct(sku.name, ProductType.Consumable, ids);
                }
#else
				var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
				foreach (var sku in rsp.skus.definitions)
				{
					if (sku == null) continue;

					var ids = ExtractValidIdsFromSku(
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

				// Create an IDs instance from a list of Beamable SKU product id and IAP store ids.
				// This method will only add the beamable SKU product ids that are not null or empty strings.
				IDs ExtractValidIdsFromSku(params SkuIdPair[] skuIdPairs)
				{
					var ids = new IDs();

					foreach (var pair in skuIdPairs)
					{
						if (string.IsNullOrEmpty(pair.skuProductId)) continue;
						ids.Add(pair.skuProductId, pair.storeId);
					}

					return ids;
				}
#endif
			});

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
		}

		private PaymentService GetPaymentService()
		{
			return _serviceProvider.GetService<PaymentService>();
		}

		private CoroutineService GetCoroutineService()
		{
			return _serviceProvider.GetService<CoroutineService>();
		}

#if UNITY_PURCHASING_5_OR_NEWER && USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR) && !BEAMABLE_STEAM_IMPLEMENTATION_DISABLED
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
#if UNITY_PURCHASING_5_OR_NEWER
			var product = _storeController?.GetProductById(skuSymbol);
#else
			var product = _storeController?.products.WithID(skuSymbol);
#endif
			return product?.metadata.localizedPriceString ?? "???";
		}

		public bool TryGetLocalizedPrice(string skuSymbol, out string localizedPrice)
		{
#if UNITY_PURCHASING_5_OR_NEWER
			var product = _storeController?.GetProductById(skuSymbol);
#else
			var product = _storeController?.products.WithID(skuSymbol);
#endif
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
			_txid = 0;
			_success = result.CompleteSuccess;
			_fail = result.CompleteError;
			if (_cancelled == null) _cancelled = () =>
			{
				result.CompleteError(
				 new ErrorCode(400, GameSystem.GAME_CLIENT, "Purchase Cancelled"));
			};

			GetPaymentService().BeginPurchase(listingSymbol).Then(rsp =>
			{
				_txid = rsp.txid;
#if UNITY_PURCHASING_5_OR_NEWER
				// v5 has no developer-payload argument; the in-flight purchase is tracked via _txid
				// (single-in-flight assumption, unchanged from v4).
#if USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR)
				// The Beamable Steam backend initiates the Steam microtransaction with the txid as order
				// id; hand it to the store so only the matching authorization resolves this purchase.
				_steamStore?.SetExpectedTransaction(_txid.ToString());
#endif
				var product = _storeController.GetProductById(skuSymbol);
				_storeController.PurchaseProduct(product);
#else
				_storeController.InitiatePurchase(skuSymbol, _txid.ToString());
#endif
			}).Error(err =>
			{
				Debug.LogError($"There was an exception making the begin purchase request: {err}");

				if (err is NoConnectivityException)
				{
					_fail?.Invoke(new ErrorCode(0, GameSystem.GAME_CLIENT, err.Message)); // network error code
				}
				else
					_fail?.Invoke(err as ErrorCode);
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
		private void HandleStoreConnected()
		{
			InAppPurchaseLogger.Log("Store connected. Fetching products.");
			_storeController.FetchProducts(_productDefinitions);
		}

		/// <summary>
		/// React to a store disconnection. Only fails initialization if it happens before init completes.
		/// </summary>
		private void HandleStoreDisconnected(StoreConnectionFailureDescription description)
		{
			Debug.LogError($"Billing store disconnected! {description?.message}");
			if (InitializationStatus == PurchasingInitializationStatus.InProgress)
			{
				InitializationStatus = PurchasingInitializationStatus.ErrorPurchasingUnavailable;
				_initPromise.CompleteError(new BeamableIAPInitializationException(InitializationStatus, description?.message));
			}
		}

		/// <summary>
		/// React to products being fetched. This is the v5 analog of v4's OnInitialized: the store is
		/// now ready. Fetch outstanding purchases (which drives restore/fulfillment) and resolve init.
		/// </summary>
		private void HandleProductsFetched(System.Collections.Generic.List<Product> products)
		{
			InitializationStatus = PurchasingInitializationStatus.Success;
			InAppPurchaseLogger.Log("Successfully initialized IAP.");
			_storeController.FetchPurchases();
			RestorePurchases();
			if (!_initPromise.IsCompleted)
			{
				_initPromise.CompleteSuccess(PromiseBase.Unit);
			}
		}

		/// <summary>
		/// React to a product fetch failure by failing initialization.
		/// </summary>
		private void HandleProductsFetchFailed(ProductFetchFailed failure)
		{
			InitializationStatus = PurchasingInitializationStatus.ErrorNoProductsAvailable;
			InAppPurchaseLogger.Log($"No products available for purchase! {failure?.FailureReason}");
			if (!_initPromise.IsCompleted)
			{
				_initPromise.CompleteError(new BeamableIAPInitializationException(InitializationStatus, failure?.FailureReason));
			}
		}

		/// <summary>
		/// Process a pending purchase (new or restored) by fulfilling it through the payments service.
		/// This is the v5 analog of v4's ProcessPurchase. Outstanding purchases fetched at startup are
		/// rerouted here automatically by the StoreController for built-in stores only (Apple/Google/etc.);
		/// custom stores such as Steam are excluded, which is moot because SteamStoreV5.FetchPurchases
		/// reports an empty set (v4 parity — Steam consumables are not restored).
		/// </summary>
		private void HandlePurchasePending(PendingOrder order)
		{
			var product = GetOrderProduct(order);

			string rawReceipt;
			var receiptString = order.Info?.Receipt;
			if (!string.IsNullOrEmpty(receiptString))
			{
				var receipt = JsonUtility.FromJson<UnityPurchaseReceipt>(receiptString);
				rawReceipt = receipt != null && !string.IsNullOrEmpty(receipt.Payload) ? receipt.Payload : receiptString;
				InAppPurchaseLogger.Log($"UnityIAP Payload: {rawReceipt}");
				InAppPurchaseLogger.Log($"UnityIAP Raw Receipt: {receiptString}");
			}
			else
			{
				rawReceipt = receiptString;
			}

			var transaction = new CompletedTransaction(
			   _txid,
			   rawReceipt,
			   product != null ? product.metadata.localizedPrice.ToString() : string.Empty,
			   product != null ? product.metadata.isoCurrencyCode : string.Empty
			);
			FulfillTransaction(transaction, order);
		}

		/// <summary>
		/// React to a confirmed purchase. Success is already reported from FulfillTransaction.
		/// </summary>
		private void HandlePurchaseConfirmed(Order order)
		{
			InAppPurchaseLogger.Log("Purchase confirmed.");
		}

		/// <summary>
		/// React to a failed purchase from Unity IAP v5.
		/// </summary>
		private void HandlePurchaseFailed(FailedOrder failedOrder)
		{
			var product = GetOrderProduct(failedOrder);
			var productId = product?.definition?.storeSpecificId;
			OnPurchaseFailedInternal(product, failedOrder.FailureReason, new OptionalString(failedOrder.Details), new OptionalString(productId));
		}

		/// <summary>
		/// React to fetched purchases. For built-in stores, outstanding pending orders are automatically
		/// rerouted through OnPurchasePending by the StoreController, so this is informational only.
		/// (Custom stores are not rerouted; the Steam store reports an empty set here.)
		/// </summary>
		private void HandlePurchasesFetched(Orders orders)
		{
			InAppPurchaseLogger.Log($"Purchases fetched. Pending: {orders?.PendingOrders?.Count}, Confirmed: {orders?.ConfirmedOrders?.Count}.");
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
					InAppPurchaseLogger.Log("Unknown billing error: '{error}'");
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

			var transaction = new CompletedTransaction(
			   _txid,
			   rawReceipt,
			   args.purchasedProduct.metadata.localizedPrice.ToString(),
			   args.purchasedProduct.metadata.isoCurrencyCode
			);
			FulfillTransaction(transaction, args.purchasedProduct);

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
			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing
			// this reason with the user to guide their troubleshooting actions.
			InAppPurchaseLogger.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1} Message: {2}, ProductId: {3}",
												  product.definition.storeSpecificId, failureReason, message?.Value, productId?.Value));
			var paymentService = GetPaymentService();
			var reasonInt = (int)failureReason;
			if (failureReason == PurchaseFailureReason.UserCancelled)
			{
				paymentService.CancelPurchase(_txid);
				_cancelled?.Invoke();
			}
			else
			{
				paymentService.FailPurchase(_txid, product.definition.storeSpecificId + ":" + failureReason);
				var errorCode = new ErrorCode(reasonInt, GameSystem.GAME_CLIENT,
				   failureReason.ToString() + $" ({product.definition.storeSpecificId})");
				_fail?.Invoke(errorCode);
			}

			ClearCallbacks();
		}
		#endregion

		/// <summary>
		/// Fulfill a completed transaction by completing the purchase in the
		/// payments service and informing Unity IAP of completion.
		/// </summary>
		/// <param name="transaction">Completed IAP transaction</param>
#if UNITY_PURCHASING_5_OR_NEWER
		/// <param name="purchasedOrder">The pending order being fulfilled</param>
		private void FulfillTransaction(CompletedTransaction transaction, PendingOrder purchasedOrder)
#else
		/// <param name="purchasedProduct">The product being purchased</param>
		private void FulfillTransaction(CompletedTransaction transaction, Product purchasedProduct)
#endif
		{
			GetPaymentService().CompletePurchase(transaction).Then(_ =>
			{
#if UNITY_PURCHASING_5_OR_NEWER
				_storeController.ConfirmPurchase(purchasedOrder);
#else
				_storeController.ConfirmPendingPurchase(purchasedProduct);
#endif
				_success?.Invoke(transaction);
				ClearCallbacks();
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
						return;
					}
				}

				var retryable = err.Code >= 500 || err.Code == 429 || err.Code == 0;   // Server error or rate limiting or network error
				if (retryable)
				{
#if UNITY_PURCHASING_5_OR_NEWER
					GetCoroutineService().StartCoroutine(RetryTransaction(transaction, purchasedOrder));
#else
					GetCoroutineService().StartCoroutine(RetryTransaction(transaction, purchasedProduct));
#endif
				}
				else
				{
#if UNITY_PURCHASING_5_OR_NEWER
					_storeController.ConfirmPurchase(purchasedOrder);
#else
					_storeController.ConfirmPendingPurchase(purchasedProduct);
#endif
					_fail?.Invoke(err);
					ClearCallbacks();
				}
			});
		}

		/// <summary>
		/// If fulfillment failed, retry fulfillment with a backoff, as a coroutine.
		/// </summary>
		/// <param name="transaction">The failed transaction</param>
#if UNITY_PURCHASING_5_OR_NEWER
		/// <param name="purchasedOrder">The pending order that should have been fulfilled</param>
		private IEnumerator RetryTransaction(CompletedTransaction transaction, PendingOrder purchasedOrder)
#else
		/// <param name="purchasedProduct">The product that should have been fulfilled</param>
		private IEnumerator RetryTransaction(CompletedTransaction transaction, Product purchasedProduct)
#endif
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

#if UNITY_PURCHASING_5_OR_NEWER
			FulfillTransaction(transaction, purchasedOrder);
#else
			FulfillTransaction(transaction, purchasedProduct);
#endif
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
