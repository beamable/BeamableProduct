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
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Beamable.Purchasing
{
	/// <summary>
	/// Implementation of Unity IAP for Beamable purchasing.
	/// </summary>
	public class UnityBeamablePurchaser : IStoreListener
										, IBeamablePurchaser

#if UNITY_PURCHASING_4_6_OR_NEWER // if this is a newer IAP, then include the detailed store listener.
	                                    , IDetailedStoreListener
#endif

	{
		public PurchasingInitializationStatus InitializationStatus { get; private set; } =
			PurchasingInitializationStatus.NotInitialized;

		private IStoreController _storeController;
#pragma warning disable CS0649
		private IAppleExtensions _appleExtensions;
		private IGooglePlayStoreExtensions _googleExtensions;
#pragma warning restore CS0649

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

		#region "IBeamablePurchaser"
		public string GetLocalizedPrice(string skuSymbol)
		{
			var product = _storeController?.products.WithID(skuSymbol);
			return product?.metadata.localizedPriceString ?? "???";
		}

		public bool TryGetLocalizedPrice(string skuSymbol, out string localizedPrice)
		{
			var product = _storeController?.products.WithID(skuSymbol);
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
				_storeController.InitiatePurchase(skuSymbol, _txid.ToString());
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
			if (Application.platform == RuntimePlatform.IPhonePlayer ||
				Application.platform == RuntimePlatform.OSXPlayer)
			{
				InAppPurchaseLogger.Log("RestorePurchases started ...");

				// Begin the asynchronous process of restoring purchases. Expect a confirmation response in
				// the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
#if UNITY_PURCHASING_4_6_OR_NEWER
				_appleExtensions.RestoreTransactions((result, error) =>
#else
				_appleExtensions.RestoreTransactions(result =>
#endif
				{
					// The first phase of restoration. If no more responses are received on ProcessPurchase then
					// no purchases are available to be restored.
					InAppPurchaseLogger.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
#if UNITY_PURCHASING_4_6_OR_NEWER
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
		}

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
		/// <param name="purchasedProduct">The product being purchased</param>
		private void FulfillTransaction(CompletedTransaction transaction, Product purchasedProduct)
		{
			GetPaymentService().CompletePurchase(transaction).Then(_ =>
			{
				_storeController.ConfirmPendingPurchase(purchasedProduct);
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
					GetCoroutineService().StartCoroutine(RetryTransaction(transaction, purchasedProduct));
				}
				else
				{
					_storeController.ConfirmPendingPurchase(purchasedProduct);
					_fail?.Invoke(err);
					ClearCallbacks();
				}
			});
		}

		/// <summary>
		/// If fulfillment failed, retry fulfillment with a backoff, as a coroutine.
		/// </summary>
		/// <param name="transaction">The failed transaction</param>
		/// <param name="purchasedProduct">The product that should have been fulfilled</param>
		private IEnumerator RetryTransaction(CompletedTransaction transaction, Product purchasedProduct)
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

			FulfillTransaction(transaction, purchasedProduct);
		}
	}

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
			builder.AddSingleton<IBeamablePurchaser, UnityBeamablePurchaser>();
		}
	}

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

	public class BeamableIAPInitializationException : Exception
	{
		public InitializationFailureReason Reason { get; }

		public BeamableIAPInitializationException(InitializationFailureReason reason) : base(
			$"Beamable IAP failed due to: {reason}")
		{
			Reason = reason;
		}
	}


	[Serializable]
	public class UnityPurchaseReceipt
	{
		public string Store;
		public string TransactionID;
		public string Payload;
	}
}
