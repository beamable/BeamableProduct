#if USE_STEAMWORKS && (!UNITY_EDITOR || BEAMABLE_STEAM_IN_EDITOR)
// BEAMABLE_STEAM_IN_EDITOR is an opt-in testing hook that forces the Steam store path
// in playmode so a mock ISteamService can drive the full purchase flow.
#define BEAMABLE_STEAM_STORE
#endif
#if BEAMABLE_STEAM_STORE && UNITY_PURCHASING_5_OR_NEWER && !BEAMABLE_STEAM_IMPLEMENTATION_DISABLED
#define BEAMABLE_STEAM_STORE_V5
#endif
using Beamable.Api;
using Beamable.Api.Auth;
using Beamable.Api.Payments;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
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

		/// <inheritdoc/>
		public PurchasingInitializationStatus InitializationStatus { get; private set; } =
			PurchasingInitializationStatus.NotInitialized;

		/// <summary>
		/// The purchase a <see cref="StartPurchase"/> call is driving.
		/// </summary>
		private class ActivePurchase
		{
			/// <summary>
			/// The Beamable SKU being bought.
			/// </summary>
			public string Sku;
			/// <summary>
			/// The Beamable transaction id from BeginPurchase, or 0 before it returns.
			/// </summary>
			public long Txid;
			/// <summary>
			/// The access token BeginPurchase is sent with. The transaction is closed with it, even after
			/// ChangeAuthorizedPlayer replaced the context's token.
			/// </summary>
			public IAccessToken Auth;
			/// <summary>
			/// Sends requests with <see cref="Auth"/>, which can't be refreshed. Created while the context is alive, so the
			/// transaction can still be closed after the context is disposed. Null when no player was signed in.
			/// </summary>
			public IBeamableRequester Requester;
			/// <summary>
			/// The player <see cref="Auth"/> belongs to, who owns the transaction and its store order.
			/// </summary>
			public long PlayerId;
			/// <summary>
			/// Whether the store was asked to buy the SKU yet.
			/// </summary>
			public bool StoreStarted;
			/// <summary>
			/// Completes when the purchase succeeds or fails. StartPurchase returns it.
			/// </summary>
			public readonly Promise<CompletedTransaction> Result = new Promise<CompletedTransaction>();
		}

		/// <summary>
		/// Seconds to wait before each retry of a failed fulfillment. Their number bounds the retries.
		/// </summary>
		static readonly int[] RETRY_DELAYS = { 1, 2, 5, 10, 20 }; // TODO: Just double a few times. ~ACM 2021-03-10

		/// <summary>
		/// Completes when the current initialization does.
		/// </summary>
		private Promise<Unit> _initPromise = new Promise<Unit>();
		/// <summary>
		/// Incremented by every Initialize, so work started for an earlier login can tell it is stale.
		/// </summary>
		private int _generation;
		/// <summary>
		/// True once the context this purchaser belongs to was disposed.
		/// </summary>
		private bool _disposed;
		/// <summary>
		/// The purchase StartPurchase is driving, or null when none is in flight.
		/// </summary>
		private ActivePurchase _purchase;
		/// <summary>
		/// The services of the context this purchaser belongs to.
		/// </summary>
		private IDependencyProvider _serviceProvider;
		/// <summary>
		/// The requester of that context. Captured so it can still be read after the context's services are disposed.
		/// </summary>
		private IPlatformRequester _requester;
		/// <summary>
		/// The route of the payment provider's purchase requests, e.g. "/basic/payments/{provider}/purchase".
		/// </summary>
		private string _purchaseUri;
		/// <summary>
		/// The access token <see cref="_playerId"/> was looked up for.
		/// </summary>
		private IAccessToken _playerIdToken;
		/// <summary>
		/// The lookup of the player <see cref="_playerIdToken"/> belongs to.
		/// </summary>
		private Promise<long> _playerId;

		#region "IBeamablePurchaser"
		/// <inheritdoc/>
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
			_requester = provider.GetService<IPlatformRequester>();
			_purchaseUri = $"/basic/payments/{provider.GetService<IPaymentServiceOptions>().ProviderId}/purchase";

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

		/// <summary>
		/// Fail a purchase left in flight and stop receiving store events. On IAP 5 this also releases the
		/// orders this purchaser was fulfilling.
		/// </summary>
		public Promise OnDispose()
		{
			_disposed = true;
			UnregisterFromStore();
			Fail(_purchase, ClientError(409, "The purchasing context was disposed during the purchase."));
			return Promise.Success;
		}

		/// <inheritdoc/>
		public string GetLocalizedPrice(string skuSymbol)
		{
			var product = GetStoreProduct(skuSymbol);
			return product?.metadata.localizedPriceString ?? "???";
		}

		/// <inheritdoc/>
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
				return Promise<CompletedTransaction>.Failed(InitializationStatus.StatusToErrorCode());
			}
			if (_purchase != null)
			{
				// Starting another purchase would orphan the one in flight, whose promise would never complete.
				return Promise<CompletedTransaction>.Failed(StoreError(PurchaseFailureReason.ExistingPurchasePending, _purchase.Sku));
			}

			var auth = CurrentToken;
			var purchase = new ActivePurchase
			{
				Sku = skuSymbol, Auth = auth, Requester = auth == null ? null : _requester.WithAccessToken(ToTokenResponse(auth))
			};
			_purchase = purchase;

			// The purchase belongs to the player of the token it begins with, not to the context's user, which is
			// unknown during login and still the previous player right after ChangeAuthorizedPlayer.
			GetPlayerId().FlatMap(playerId =>
			{
				var error = CheckCanBegin(purchase);
				if (error != null) return Promise<Unit>.Failed(error);
				purchase.PlayerId = playerId;
				// On Apple devices, a retained order of this SKU is retried instead of charging the player again.
				return TryResumeRetainedOrder(purchase) ? PromiseBase.SuccessfulUnit : BeginStorePurchase(purchase, listingSymbol);
			}).Error(err =>
			{
				if (!IsInFlight(purchase))
				{
					// Retired before its store purchase started: the caller was already settled, and nothing is open.
					return;
				}
				Debug.LogError($"There was an exception making the begin purchase request: {err}");
				if (purchase.Txid != 0)
				{
					// The Beamable transaction began, but the store purchase could not start.
					ForgetOpenPurchase(skuSymbol, purchase.Txid);
					CloseTransaction(purchase.Txid, purchase, false, skuSymbol + ":" + err.Message);
				}
				// NoConnectivityException and other exceptions map to the network error code 0.
				Fail(purchase, err as ErrorCode ?? ClientError(0, err.Message));
			});

			return purchase.Result;
		}

		/// <summary>
		/// Reconnect the store if needed, begin the purchase's Beamable transaction, then ask the store to buy the SKU.
		/// A purchase retired meanwhile never reaches the store, and its transaction is closed.
		/// </summary>
		private Promise<Unit> BeginStorePurchase(ActivePurchase purchase, string listingSymbol)
		{
			var skuSymbol = purchase.Sku;
			return PrepareStoreForPurchase().FlatMap(_ =>
			{
				var error = CheckCanBegin(purchase);
				if (error != null) return Promise<PurchaseResponse>.Failed(error);
				// Check the product before beginning the Beamable transaction, so an unknown SKU doesn't leave one open.
				var product = GetStoreProduct(skuSymbol);
				if (product == null || !product.availableToPurchase)
				{
					return Promise<PurchaseResponse>.Failed(StoreError(PurchaseFailureReason.ProductUnavailable, skuSymbol));
				}
				// CheckCanBegin made sure this request is sent with purchase.Auth.
				return GetPaymentService().BeginPurchase(listingSymbol);
			}).FlatMap(rsp =>
			{
				if (!IsInFlight(purchase))
				{
					// The purchase was retired while its Beamable transaction began. Its caller already got an error, so
					// close the transaction instead of asking the store to charge the player.
					CloseTransaction(rsp.txid, purchase, false, skuSymbol + ":PurchaseRetired");
					return PromiseBase.SuccessfulUnit;
				}
				purchase.Txid = rsp.txid;
				purchase.StoreStarted = true;
				StartStorePurchase(purchase);
				return PromiseBase.SuccessfulUnit;
			});
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
			RetryRetainedOrders().Then(retrying =>
			{
				if (retrying || _storeController == null || _disposed)
				{
					return;
				}

				// Begin the asynchronous process of restoring purchases. Expect a confirmation response in
				// OnTransactionsRestored, and a pending purchase for every previously purchased product to restore.
				InAppPurchaseLogger.Log("RestorePurchases started ...");
				RestoreStoreTransactions();
			});
#endif
		}
		#endregion

		/// <summary>
		/// Log the result of a RestoreTransactions call.
		/// </summary>
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

		/// <summary>
		/// An error raised by the game client rather than by the Beamable backend.
		/// </summary>
		private static ErrorCode ClientError(long code, string message) => new ErrorCode(code, GameSystem.GAME_CLIENT, message);

		/// <summary>
		/// The Beamable error behind a failed request, or null when the exception carries none (e.g. no connectivity).
		/// </summary>
		private static ErrorCode ToErrorCode(Exception ex)
		{
			if (ex is ErrorCode error) return error;
			var platformError = (ex as PlatformRequesterException)?.Error;
			return platformError != null ? new ErrorCode(platformError) : null;
		}

		/// <summary>
		/// A client error for a store failure reason, with a detail such as the product id.
		/// </summary>
		private static ErrorCode StoreError(PurchaseFailureReason reason, string detail) => ClientError((int)reason, $"{reason} ({detail})");

		/// <summary>
		/// True when <paramref name="sku"/> is the product the store is buying for this purchaser's StartPurchase.
		/// </summary>
		private bool IsActivePurchase(string sku) => IsBuyingInStore(sku, false);

		/// <summary>
		/// True when the store is buying <paramref name="sku"/> for this purchaser's StartPurchase.
		/// </summary>
		/// <param name="anyWhenUnknown">Whether a null SKU (the store didn't say which product) matches any purchase
		/// in the store.</param>
		private bool IsBuyingInStore(string sku, bool anyWhenUnknown) =>
			_purchase != null && _purchase.StoreStarted && (sku == null ? anyWhenUnknown : sku == _purchase.Sku);

		/// <summary>
		/// True while <paramref name="purchase"/> is still this purchaser's purchase in flight. Initialize, OnDispose
		/// and settling the purchase retire it.
		/// </summary>
		private bool IsInFlight(ActivePurchase purchase) => _purchase == purchase;

		/// <summary>
		/// Why the purchase can't begin its Beamable transaction now, or null when it can: it must still be in flight,
		/// and the context must still send the token the purchase started with.
		/// </summary>
		private ErrorCode CheckCanBegin(ActivePurchase purchase)
		{
			if (!IsInFlight(purchase))
			{
				// Initialize or OnDispose already failed this purchase; don't begin a Beamable transaction for it.
				return ClientError(409, "The purchase was retired before it began.");
			}
			if (CurrentToken != purchase.Auth)
			{
				// ChangeAuthorizedPlayer swaps the token before it initializes purchasing again, which retires the purchase.
				return ClientError(409, "The signed-in account changed before the purchase began.");
			}
			return null;
		}

		/// <summary>
		/// True while this purchaser still serves the login that started work in <paramref name="generation"/>.
		/// </summary>
		private bool IsCurrent(int generation) => generation == _generation && !_disposed && IsRegisteredWithStore;

		/// <summary>
		/// The payment service of this purchaser's context.
		/// </summary>
		private PaymentService GetPaymentService()
		{
			return _serviceProvider.GetService<PaymentService>();
		}

		/// <summary>
		/// The access token this purchaser's context sends with its requests.
		/// </summary>
		private IAccessToken CurrentToken => _requester?.AccessToken;

		/// <summary>
		/// A token response carrying the given access token but not its refresh token, for requests that must be sent
		/// with it. When the backend rejects the token, such a request fails instead of refreshing the token and
		/// saving it as the realm's login, which may belong to another player by then.
		/// </summary>
		private static TokenResponse ToTokenResponse(IAccessToken token) => new TokenResponse
		{
			access_token = token.Token,
			expires_in = token.ExpiresIn
		};

		/// <summary>
		/// The id of the player the context's token belongs to. Purchases and orders are owned by that player, never
		/// by the context's user, which is unknown during login and still the previous player right after
		/// ChangeAuthorizedPlayer replaced the token. When the token changes before the lookup returns (the lookup
		/// refreshed it, or the account switched), the player of the new token is looked up instead. The lookup is
		/// shared until the token changes, and repeated after it fails. It never resolves to 0.
		/// </summary>
		private Promise<long> GetPlayerId()
		{
			var token = CurrentToken;
			if (token == null)
			{
				return Promise<long>.Failed(ClientError(401, "No player is signed in."));
			}
			if (_playerIdToken != token || _playerId == null || _playerId.IsFailed)
			{
				_playerIdToken = token;
				_playerId = _serviceProvider.GetService<IAuthService>().GetUser().FlatMap(user =>
				{
					if (CurrentToken != token)
					{
						// The response may be for either token, so look up the one the context sends now.
						return GetPlayerId();
					}
					return user != null && user.id != 0
						? Promise<long>.Successful(user.id)
						: Promise<long>.Failed(ClientError(401, "The signed-in player could not be identified."));
				});
			}
			return _playerId;
		}

		/// <summary>
		/// Fail or cancel a Beamable transaction with the access token that began it, even after ChangeAuthorizedPlayer
		/// replaced the context's token or the context was disposed. A failure is logged; the transaction then stays
		/// open on the backend.
		/// </summary>
		/// <param name="txid">The transaction, or 0 when none began.</param>
		/// <param name="began">The purchase that began the transaction, or null when it isn't known.</param>
		/// <param name="cancelled">True when the player cancelled the purchase.</param>
		/// <param name="reason">Why the purchase failed. Ignored when it was cancelled.</param>
		private void CloseTransaction(long txid, ActivePurchase began, bool cancelled, string reason)
		{
			var requester = began?.Requester;
			if (txid == 0 || requester == null) return;

			var request = cancelled
				? requester.Request<EmptyResponse>(Method.POST, _purchaseUri + "/cancel", new CancelPurchaseRequest(txid))
				: requester.Request<EmptyResponse>(Method.POST, _purchaseUri + "/fail", new FailPurchaseRequest(txid, reason));
			request.Error(err =>
			{
				// Never log the exception itself: payment errors can contain the receipt.
				Debug.LogWarning($"Beamable IAP closing the transaction failed: beamTx={txid}, code={ToErrorCode(err)?.Code ?? 0}, " +
				                 $"type={err.GetType().Name}. The transaction stays open.");
			});
		}

		/// <summary>
		/// The coroutine service of this purchaser's context, which runs fulfillment retries.
		/// </summary>
		private CoroutineService GetCoroutineService()
		{
			return _serviceProvider.GetService<CoroutineService>();
		}

		/// <summary>
		/// The product's price as text for the backend. It doesn't depend on the device's culture (e.g.
		/// "4,99").
		/// </summary>
		private static string FormatPrice(Product product) =>
			product?.metadata == null ? string.Empty : product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture);

		/// <summary>
		/// The transaction sent to the backend to fulfill an order of the product, with the product's price and
		/// currency.
		/// </summary>
		/// <param name="txid">The Beamable transaction the order completes, or 0 when no StartPurchase began one.</param>
		/// <param name="receipt">The store receipt the backend verifies.</param>
		private static CompletedTransaction CreateTransaction(long txid, string receipt, Product product) =>
			new CompletedTransaction(txid, receipt, FormatPrice(product), product?.metadata?.isoCurrencyCode ?? string.Empty);

		/// <summary>
		/// Read the store's receipt inside Unity's receipt envelope. Returns false when the envelope is malformed.
		/// </summary>
		/// <param name="payload">The store's receipt, or null when the envelope is missing or has none.</param>
		private static bool TryReadReceiptPayload(string envelope, out string payload)
		{
			try
			{
				payload = string.IsNullOrEmpty(envelope) ? null : JsonUtility.FromJson<UnityPurchaseReceipt>(envelope)?.Payload;
				return true;
			}
			catch (Exception)
			{
				payload = null;
				return false;
			}
		}

		/// <summary>
		/// Settle a failed store purchase: cancel or fail its Beamable transaction, and fail the purchase in
		/// flight if it is the one that failed.
		/// </summary>
		private void OnPurchaseFailedInternal(Product product, PurchaseFailureReason failureReason, OptionalString message, OptionalString productId)
		{
			var sku = product?.definition?.id;
			var storeProductId = product?.definition?.storeSpecificId ?? productId?.Value ?? "unknown";

			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing
			// this reason with the user to guide their troubleshooting actions.
			InAppPurchaseLogger.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1} Message: {2}, ProductId: {3}",
												  storeProductId, failureReason, message?.Value, productId?.Value));

			// When the store doesn't say which product failed, it is the purchase in flight.
			var purchase = IsBuyingInStore(sku, true) ? _purchase : null;
			var began = purchase ?? GetOpenPurchase(sku);
			var txid = began?.Txid ?? 0;
			ForgetOpenPurchase(sku, txid);
			CloseTransaction(txid, began, failureReason == PurchaseFailureReason.UserCancelled,
			                 storeProductId + ":" + failureReason);

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
		/// <param name="ownerId">The player who owns the order, or 0 when no player owns it. The order is left pending
		/// while the context's token belongs to another player, so it never credits another account. Any token of the
		/// owner fulfills it, e.g. after a refresh or in a replacement context.</param>
		private void FulfillTransaction(CompletedTransaction transaction, StoreOrder order, ActivePurchase purchase, long ownerId)
		{
			var generation = _generation;
			Action<Exception> failed = ex =>
			{
				if (!IsCurrent(generation))
				{
					StopFulfillment(order, purchase, ClientError(409, "Login changed during fulfillment; order left pending."));
					return;
				}

				var err = ToErrorCode(ex);
				var retryable = ex is NoConnectivityException ||
				                (err != null && (err.Code >= 500 || err.Code == 429 || err.Code == 0)); // Server error or rate limiting or network error
				// Never log the exception itself: payment errors can contain the receipt.
				Debug.LogWarning($"Beamable IAP completing the purchase failed: storeTx={GetStoreTransactionId(order)}, " +
				                 $"beamTx={transaction.Txid}, code={err?.Code ?? 0}, type={ex.GetType().Name}.");

				if ((ex as PlatformRequesterException)?.Error?.error == "MalformedReceiptError" && TryRefreshMalformedReceipt(order))
				{
					return;
				}
				if (retryable && transaction.Retries < RETRY_DELAYS.Length)
				{
					GetCoroutineService().StartCoroutine(RetryTransaction(transaction, order, purchase, ownerId, generation));
					return;
				}
				StopFulfillment(order, purchase, err ?? ClientError(0, "Purchase fulfillment failed; order left pending."));
			};

			try
			{
				var owner = ownerId == 0 ? Promise<long>.Successful(0) : GetPlayerId();
				owner.FlatMap(playerId => playerId == ownerId
					? GetPaymentService().CompletePurchase(transaction)
					: Promise<EmptyResponse>.Failed(ClientError(409, "The order belongs to another account; order left pending.")))
				.Then(_ =>
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
		private IEnumerator RetryTransaction(CompletedTransaction transaction, StoreOrder order, ActivePurchase purchase, long ownerId, int generation)
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
			FulfillTransaction(transaction, order, purchase, ownerId);
		}

#if UNITY_PURCHASING_5_OR_NEWER
		// Unity IAP v5 services are domain-wide singletons: every StoreController shares them, and handlers added
		// through any controller stay attached until removed. So all purchasers (one per BeamContext) share one
		// controller with one set of handlers, and the static handlers below dispatch store events to them.

		/// <summary>
		/// The controller every purchaser shares.
		/// </summary>
		private static StoreController s_storeController;
		/// <summary>
		/// The purchasers that receive store events, one per initialized context.
		/// </summary>
		private static readonly List<UnityBeamablePurchaser> s_purchasers = new List<UnityBeamablePurchaser>();
		/// <summary>
		/// The product fetch in progress, or null.
		/// </summary>
		private static ProductFetchBatch s_productFetchBatch;
		/// <summary>
		/// Whether a fetch was requested while another one was in progress. It starts once that one
		/// completes.
		/// </summary>
		private static bool s_refetchRequested;
		/// <summary>
		/// Orders being fulfilled, by store transaction id, so an order the store delivers again (e.g.
		/// FetchPurchases during another initialization) is not fulfilled twice. Apple orders stay here after
		/// a failed fulfillment, so StartPurchase of the same SKU or RestorePurchases can retry them without
		/// another charge.
		/// </summary>
		private static readonly Dictionary<string, OrderFulfillment> s_orders = new Dictionary<string, OrderFulfillment>();
		/// <summary>
		/// Store purchases that started but did not complete, by SKU. An order that arrives later (deferred payment
		/// approved, or the context that started it is gone) completes its own transaction, owned by the player who
		/// began it, and a failure closes it with the token it began with.
		/// </summary>
		private static readonly Dictionary<string, ActivePurchase> s_openPurchases = new Dictionary<string, ActivePurchase>();
		/// <summary>
		/// The Apple receipt refresh in progress, which concurrent requests share.
		/// </summary>
		private static Promise<string> s_receiptRefresh;

		/// <summary>
		/// One FetchProducts call. IAP reports the products it found, then a failure for the rest, so the batch is
		/// complete once every requested id has one of those outcomes.
		/// </summary>
		private class ProductFetchBatch
		{
			/// <summary>
			/// The requested product ids that have no outcome yet.
			/// </summary>
			public readonly HashSet<string> RemainingIds = new HashSet<string>();
			/// <summary>
			/// The purchasers whose initialization waits for this batch, with the initialization they
			/// started.
			/// </summary>
			public readonly List<(UnityBeamablePurchaser Purchaser, Promise<Unit> Init)> Waiters =
				new List<(UnityBeamablePurchaser, Promise<Unit>)>();
			/// <summary>
			/// The reason IAP gave for the products it couldn't fetch, if any.
			/// </summary>
			public string FailureReason;
			/// <summary>
			/// True while the batch settles its purchasers, so late store callbacks are ignored.
			/// </summary>
			public bool Completing;
		}

		/// <summary>
		/// A store order being fulfilled. The Apple fields drive the receipt recovery.
		/// </summary>
		private class OrderFulfillment
		{
			/// <summary>
			/// The store's order.
			/// </summary>
			public PendingOrder Order;
			/// <summary>
			/// The product the order is for.
			/// </summary>
			public Product Product;
			/// <summary>
			/// The purchaser fulfilling the order, which releases it when its context is disposed. A purchaser signed in
			/// as <see cref="PlayerId"/> takes it over to retry it.
			/// </summary>
			public UnityBeamablePurchaser Owner;
			/// <summary>
			/// The owner's initialization generation when it took the order, so work started for an earlier login stops.
			/// </summary>
			public int Generation;
			/// <summary>
			/// The player who owns the order: the player who began its Beamable transaction, or else the player of the
			/// token that took it. Only that player can recover it. 0 while that player is looked up.
			/// </summary>
			public long PlayerId;
			/// <summary>
			/// The transaction sent to the backend to fulfill the order.
			/// </summary>
			public CompletedTransaction Transaction;
			/// <summary>
			/// The StartPurchase this order completes, or null for other orders.
			/// </summary>
			public ActivePurchase Purchase;
			/// <summary>
			/// True while the order is being fulfilled, so a redelivery of it is skipped.
			/// </summary>
			public bool Busy;
			/// <summary>
			/// Apple only: the backend granted the rewards, but confirming the order with the store may have failed.
			/// </summary>
			public bool Fulfilled;

			/// <summary>
			/// Apple only: the player started this fulfillment, so the receipt may be refreshed, which can prompt for the
			/// Apple ID.
			/// </summary>
			public bool AllowRefresh;

			/// <summary>
			/// Apple only: the receipt was already refreshed during this attempt.
			/// </summary>
			public bool RefreshAttempted;
		}

		/// <summary>
		/// The shared controller, once this purchaser is registered.
		/// </summary>
		private StoreController _storeController;
		/// <summary>
		/// The store product definitions, built during Initialize and fetched once the store connects.
		/// </summary>
		private List<ProductDefinition> _productDefinitions;
		/// <summary>
		/// True while this purchaser's initialization waits for the store to return its products.
		/// </summary>
		private bool _awaitingProducts;
		/// <summary>
		/// Completes when the reconnection a purchase is waiting for does.
		/// </summary>
		private Promise<Unit> _connectPromise;

#if BEAMABLE_STEAM_STORE
		/// <summary>
		/// Whether orders come from the App Store. Steam builds report OSXPlayer on macOS, but their orders
		/// come from the Steam store.
		/// </summary>
		private static bool IsApplePlayer => false;
#else
		/// <summary>
		/// Whether orders come from the App Store.
		/// </summary>
		private static bool IsApplePlayer => Application.platform == RuntimePlatform.IPhonePlayer ||
		                                     Application.platform == RuntimePlatform.OSXPlayer;
#endif

		/// <summary>
		/// Whether this purchaser receives store events.
		/// </summary>
		private bool IsRegisteredWithStore => s_purchasers.Contains(this);

		/// <summary>
		/// Reset the shared store state when play mode starts, including with domain reload disabled.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			// With domain reload disabled, statics survive between play sessions, but Unity IAP 5 resets its own
			// service containers at SubsystemRegistration. Release the old controller's handlers so the next
			// session creates a controller on the new services.
			if (s_storeController != null) SetStoreHandlers(s_storeController, false);
			s_storeController = null;
			s_purchasers.Clear();
			s_productFetchBatch = null;
			s_refetchRequested = false;
			s_orders.Clear();
			s_openPurchases.Clear();
			s_receiptRefresh = null;
#if BEAMABLE_STEAM_STORE_V5
			// The custom store registration is dropped with IAP's containers, so it must be registered again.
			_registeredSteamStore = null;
#endif
		}

		/// <summary>
		/// Set up the store for the Beamable SKUs. The initialization promise completes once the store is
		/// ready.
		/// </summary>
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

		/// <summary>
		/// Stop receiving store events, and release the orders this purchaser was fulfilling.
		/// </summary>
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

		/// <summary>
		/// The store's product for a Beamable SKU, or null when the store doesn't have it.
		/// </summary>
		private Product GetStoreProduct(string skuSymbol) => _storeController?.GetProductById(skuSymbol);

		/// <summary>
		/// The store's transaction id of an order, used in logs and to track the order.
		/// </summary>
		private static string GetStoreTransactionId(Order order) => order?.Info?.TransactionID;

		/// <summary>
		/// Resolves once the store can start a purchase.
		/// </summary>
		private Promise<Unit> PrepareStoreForPurchase()
		{
			EnsureStoreController();
			return EnsureConnected();
		}

		/// <summary>
		/// Ask the store to buy the purchase's SKU, after its Beamable transaction began.
		/// </summary>
		private void StartStorePurchase(ActivePurchase purchase)
		{
			// v5 has no developer-payload argument; the in-flight purchase is tracked by _purchase
			// and, for orders that arrive after this purchase settles, by s_openPurchases.
			s_openPurchases[purchase.Sku] = purchase;
#if BEAMABLE_STEAM_STORE_V5
			// The Beamable Steam backend initiates the Steam microtransaction with the txid as order
			// id; hand it to the store so only the matching authorization resolves this purchase.
			_steamStore?.SetExpectedTransaction(purchase.Txid.ToString());
#endif
			_storeController.PurchaseProduct(GetStoreProduct(purchase.Sku));
		}

		/// <summary>
		/// Ask the App Store to deliver previous purchases again.
		/// </summary>
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
				SetStoreHandlers(s_storeController, true);
			}

			if (!s_purchasers.Contains(this))
			{
				s_purchasers.Add(this);
			}
			_storeController = s_storeController;
			return s_storeController;
		}

		/// <summary>
		/// Subscribe the store event handlers to the controller, or unsubscribe them. One list for both, so they can't
		/// drift apart.
		/// </summary>
		private static void SetStoreHandlers(StoreController controller, bool attach)
		{
			if (attach)
			{
				controller.OnStoreConnected += HandleStoreConnected;
				controller.OnStoreDisconnected += HandleStoreDisconnected;
				controller.OnProductsFetched += HandleProductsFetched;
				controller.OnProductsFetchFailed += HandleProductsFetchFailed;
				controller.OnPurchasePending += HandlePurchasePending;
				controller.OnPurchaseConfirmed += HandlePurchaseConfirmed;
				controller.OnPurchaseFailed += HandlePurchaseFailed;
				controller.OnPurchaseDeferred += HandlePurchaseDeferred;
				controller.OnPurchasesFetched += HandlePurchasesFetched;
				controller.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
			}
			else
			{
				controller.OnStoreConnected -= HandleStoreConnected;
				controller.OnStoreDisconnected -= HandleStoreDisconnected;
				controller.OnProductsFetched -= HandleProductsFetched;
				controller.OnProductsFetchFailed -= HandleProductsFetchFailed;
				controller.OnPurchasePending -= HandlePurchasePending;
				controller.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
				controller.OnPurchaseFailed -= HandlePurchaseFailed;
				controller.OnPurchaseDeferred -= HandlePurchaseDeferred;
				controller.OnPurchasesFetched -= HandlePurchasesFetched;
				controller.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
			}
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

		/// <summary>
		/// Settle the reconnection a purchase is waiting for. The error is null when the store connected.
		/// </summary>
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

		/// <summary>
		/// Whether the store has any of the products available to purchase.
		/// </summary>
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

		/// <summary>
		/// Pick the store-specific product id for the current runtime platform from the Beamable SKU. v5's
		/// ProductDefinition carries a single store id, unlike v4's multi-store IDs.
		/// </summary>
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
				if (purchaser.IsBuyingInStore(sku, true)) return purchaser;
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

		/// <summary>
		/// The unfinished store purchase of the SKU, or null when there is none.
		/// </summary>
		private static ActivePurchase GetOpenPurchase(string sku)
		{
			return sku != null && s_openPurchases.TryGetValue(sku, out var purchase) ? purchase : null;
		}

		/// <summary>
		/// Forget the unfinished store purchase of the SKU once its transaction is settled.
		/// </summary>
		private static void ForgetOpenPurchase(string sku, long txid)
		{
			if (sku != null && s_openPurchases.TryGetValue(sku, out var purchase) && purchase.Txid == txid)
			{
				s_openPurchases.Remove(sku);
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
		/// React to a store disconnection: fail the reconnections purchases are waiting for, and the initializations
		/// still in progress. After initialization, StartPurchase reconnects the store.
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
		/// React to products being fetched by recording them in the current batch. The batch completes, resolving
		/// the waiting initializations and fetching outstanding purchases, once every requested product has an outcome.
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
			var transactionId = GetStoreTransactionId(order);
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
			// The purchase whose Beamable transaction this order completes, if any. Its player owns the order.
			var began = purchase ?? GetOpenPurchase(sku);
			var txid = began?.Txid ?? 0;
			var transactionId = GetStoreTransactionId(order);

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
						Order = order, Product = product, Owner = this, Generation = _generation, PlayerId = began?.PlayerId ?? 0, Purchase = purchase,
						Transaction = CreateTransaction(txid, string.Empty, product)
					};
					s_orders.Add(transactionId, fulfillment);
					if (fulfillment.PlayerId != 0)
					{
						ProcessAppleOrder(fulfillment, purchase != null);
						return;
					}
				}
				FulfillAppleOrderAsSignedInPlayer(fulfillment, transactionId, purchase != null);
				return;
			}

			var envelope = order.Info?.Receipt;
			if (!TryReadReceiptPayload(envelope, out var payload))
			{
				StopFulfillment(order, purchase, ClientError(400, "Unable to read the store receipt; order left pending."));
				return;
			}
			var receipt = (!string.IsNullOrEmpty(payload) ? payload : envelope) ?? string.Empty;
			if (!string.IsNullOrEmpty(transactionId))
			{
				s_orders[transactionId] = new OrderFulfillment
				{
					Order = order, Product = product, Owner = this, Generation = _generation, PlayerId = began?.PlayerId ?? 0, Busy = true
				};
			}
			FulfillTransaction(CreateTransaction(txid, receipt, product), order, purchase, began?.PlayerId ?? 0);
		}

		/// <summary>
		/// React to a confirmed purchase. Success is already reported from FulfillTransaction. IAP also reports a
		/// failed confirmation here; the Apple order is then retained so a retry confirms it again.
		/// </summary>
		private static void HandlePurchaseConfirmed(Order order)
		{
			var id = GetStoreTransactionId(order);
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
		/// delivers the order through OnPurchasePending, which completes it with the txid kept in s_openPurchases.
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
			var id = GetStoreTransactionId(order);
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
			ForgetOpenPurchase(GetOrderProduct(order)?.definition?.id, transaction.Txid);
		}

		/// <summary>
		/// Stop tracking the order as being fulfilled. The store delivers it again on the next FetchPurchases; an
		/// Apple order is also retained for a retry without another charge.
		/// </summary>
		private static void ReleaseStoreOrder(PendingOrder order)
		{
			var id = GetStoreTransactionId(order);
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

		/// <summary>
		/// Take over a retained order for this purchaser when the order isn't in progress and belongs to the given
		/// player. The order may come from an earlier initialization or a disposed context of that player.
		/// </summary>
		/// <param name="playerId">The player of the token this purchaser's context sends.</param>
		private bool TryAdopt(OrderFulfillment fulfillment, long playerId)
		{
			if (fulfillment.Busy || fulfillment.PlayerId == 0 || fulfillment.PlayerId != playerId) return false;
			fulfillment.Owner = this;
			fulfillment.Generation = _generation;
			return true;
		}

		/// <summary>
		/// Fulfill an Apple order once the player of the context's token is known. A new order becomes that player's;
		/// a retained order is only taken over when it is theirs. The order is Busy meanwhile, so a redelivery of it is
		/// skipped. When the player can't be identified, a new order is forgotten and the store delivers it again.
		/// </summary>
		private void FulfillAppleOrderAsSignedInPlayer(OrderFulfillment fulfillment, string transactionId, bool allowRefresh)
		{
			var generation = _generation;
			fulfillment.Busy = true;
			fulfillment.Owner = this;
			fulfillment.Generation = generation;

			// Another purchaser takes the order over when this one is disposed meanwhile and the store delivers it again.
			Func<bool> stillClaimed = () => fulfillment.Owner == this && fulfillment.Generation == generation;
			GetPlayerId().Then(playerId =>
			{
				if (!stillClaimed()) return;
				fulfillment.Busy = false;
				if (!IsCurrent(generation))
				{
					if (fulfillment.PlayerId == 0) s_orders.Remove(transactionId);
					return;
				}
				if (fulfillment.PlayerId == 0) fulfillment.PlayerId = playerId;
				if (!TryAdopt(fulfillment, playerId))
				{
					Debug.LogWarning("Beamable IAP: a pending Apple order belongs to another account; it is recovered when that account signs in again.");
					return;
				}
				ProcessAppleOrder(fulfillment, allowRefresh);
			}).Error(_ =>
			{
				if (!stillClaimed()) return;
				fulfillment.Busy = false;
				if (fulfillment.PlayerId == 0) s_orders.Remove(transactionId);
				Debug.LogWarning($"Beamable IAP: the signed-in player could not be identified, so the Apple order {transactionId} is left pending.");
			});
		}

		/// <summary>
		/// On Apple devices, a StartPurchase of a SKU whose order failed fulfillment retries that order instead of
		/// charging the player again. Returns false when there is no such order. The purchase must already be in
		/// flight, with its player known.
		/// </summary>
		private bool TryResumeRetainedOrder(ActivePurchase purchase)
		{
			foreach (var fulfillment in RetainedAppleOrders(purchase.Sku))
			{
				if (fulfillment.Busy)
				{
					Fail(purchase, ClientError(409, "An Apple order of this product is being recovered; wait for it to finish."));
					return true;
				}
				if (!TryAdopt(fulfillment, purchase.PlayerId))
				{
					Fail(purchase, ClientError(409,
						"An Apple order of this product from another account is pending recovery; it is recovered when that account signs in again."));
					return true;
				}

				purchase.Txid = fulfillment.Transaction.Txid;
				purchase.StoreStarted = true;
				fulfillment.Purchase = purchase;
				fulfillment.RefreshAttempted = false;
				ProcessAppleOrder(fulfillment, true);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Retry the fulfillment of the signed-in player's retained Apple orders. Resolves false when there are none,
		/// or when the player can't be identified.
		/// </summary>
		private Promise<bool> RetryRetainedOrders()
		{
			if (RetainedAppleOrders(null).Count == 0) return Promise<bool>.Successful(false);
			var generation = _generation;
			return GetPlayerId().Map(playerId =>
			{
				if (!IsCurrent(generation)) return false;
				var retrying = false;
				foreach (var fulfillment in RetainedAppleOrders(null))
				{
					if (!TryAdopt(fulfillment, playerId)) continue;
					retrying = true;
					fulfillment.RefreshAttempted = false;
					ProcessAppleOrder(fulfillment, true);
				}
				return retrying;
			}).Recover(_ => false);
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

			// A malformed envelope counts as a missing receipt.
			TryReadReceiptPayload(fulfillment.Order.Info?.Receipt, out var receipt);
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

		/// <summary>
		/// Refresh the app receipt, and fulfill the order with it if it identifies the order. Otherwise leave
		/// the order pending.
		/// </summary>
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
			var id = GetStoreTransactionId(order);
			if (!IsApplePlayer || id == null || !s_orders.TryGetValue(id, out var fulfillment)) return false;
			if (!fulfillment.AllowRefresh || fulfillment.RefreshAttempted || fulfillment.Generation != _generation) return false;

			RefreshAndSubmitAppleReceipt(fulfillment);
			return true;
		}

		/// <summary>
		/// Fulfill the order with the given app receipt.
		/// </summary>
		private void SubmitAppleReceipt(OrderFulfillment fulfillment, string receipt)
		{
			var previous = fulfillment.Transaction;
			fulfillment.Transaction = new CompletedTransaction(previous.Txid, receipt, previous.PriceInLocalCurrency, previous.IsoCurrencySymbol);
			InAppPurchaseLogger.Log($"Apple receipt ready: appleTx={GetStoreTransactionId(fulfillment.Order)}, beamTx={previous.Txid}, length={receipt.Length}.");
			FulfillTransaction(fulfillment.Transaction, fulfillment.Order, fulfillment.Purchase, fulfillment.PlayerId);
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
					if (!string.Equals(entry.transactionID, GetStoreTransactionId(fulfillment.Order), StringComparison.Ordinal)) return false;
					found = true;
				}
				return found;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Refresh the app receipt through IAP. Resolves with the receipt, or fails when the refresh fails or
		/// times out.
		/// </summary>
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

		/// <summary>
		/// Fail the refresh if IAP hasn't answered within 120 seconds.
		/// </summary>
		private static IEnumerator ReceiptRefreshTimeout(Promise<string> promise)
		{
			yield return new WaitForSecondsRealtime(120);
			if (!promise.IsCompleted) promise.CompleteError(new TimeoutException("Apple receipt refresh timed out."));
		}
		#endregion

#if BEAMABLE_STEAM_STORE_V5
		/// <summary>
		/// The Beamable Steam store registered with Unity IAP v5. IAP caches store services per store name
		/// for the domain lifetime (StoreServiceContainer throws on duplicate registration), so this is a
		/// domain-lifetime singleton whose dependency provider must be refreshed on every Initialize.
		/// </summary>
		private static Steam.SteamStoreV5 _registeredSteamStore;
		/// <summary>
		/// The registered Steam store, used to pass the expected Beamable txid to it.
		/// </summary>
		private Steam.SteamStoreV5 _steamStore;

		/// <summary>
		/// Register the Beamable Steam custom store with Unity IAP v5 exactly once per domain, and keep a
		/// handle so StartPurchase can pass the expected Beamable txid to the store.
		/// </summary>
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
		/// <summary>
		/// The store controller IAP passed to OnInitialized.
		/// </summary>
		private IStoreController _storeController;
#pragma warning disable CS0649
		/// <summary>
		/// App Store extensions, for restores and deferred purchases.
		/// </summary>
		private IAppleExtensions _appleExtensions;
		/// <summary>
		/// Google Play extensions, for deferred purchases.
		/// </summary>
		private IGooglePlayStoreExtensions _googleExtensions;
#pragma warning restore CS0649

		// IAP 4 keeps no shared store state between purchasers, and doesn't retain orders for a retry.
		/// <summary>
		/// Whether this purchaser receives store events. Always true on IAP 4.
		/// </summary>
		private bool IsRegisteredWithStore => true;

		/// <summary>
		/// Set up the store for the Beamable SKUs. The initialization promise completes once the store is
		/// ready.
		/// </summary>
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

		/// <summary>
		/// Stop receiving store events. IAP 4 keeps no shared store state or orders, so there is nothing to release.
		/// </summary>
		private void UnregisterFromStore() { }

		/// <summary>
		/// The store's product for a Beamable SKU, or null when the store doesn't have it.
		/// </summary>
		private Product GetStoreProduct(string skuSymbol) => _storeController?.products.WithID(skuSymbol);

		/// <summary>
		/// The store's transaction id of an order, used in logs and to track the order.
		/// </summary>
		private static string GetStoreTransactionId(Product order) => order?.transactionID;

		/// <summary>
		/// Resolves once the store can start a purchase.
		/// </summary>
		private Promise<Unit> PrepareStoreForPurchase() => PromiseBase.SuccessfulUnit;

		/// <summary>
		/// Ask the store to buy the purchase's SKU, after its Beamable transaction began.
		/// </summary>
		private void StartStorePurchase(ActivePurchase purchase) => _storeController.InitiatePurchase(purchase.Sku, purchase.Txid.ToString());

		/// <summary>
		/// Ask the App Store to deliver previous purchases again.
		/// </summary>
		private void RestoreStoreTransactions()
		{
#if UNITY_PURCHASING_4_6_OR_NEWER
			_appleExtensions.RestoreTransactions(OnTransactionsRestored);
#else
			_appleExtensions.RestoreTransactions(result => OnTransactionsRestored(result, null));
#endif
		}

		/// <summary>
		/// IAP 4 retains no orders, so there is no order to resume.
		/// </summary>
		private bool TryResumeRetainedOrder(ActivePurchase purchase) => false;

		/// <summary>
		/// IAP 4 retains no orders, so there is no order to retry.
		/// </summary>
		private Promise<bool> RetryRetainedOrders() => Promise<bool>.Successful(false);

		/// <summary>
		/// IAP 4 doesn't refresh receipts.
		/// </summary>
		private bool TryRefreshMalformedReceipt(Product order) => false;

		/// <summary>
		/// IAP 4 tracks no unfinished store purchases besides the purchase in flight.
		/// </summary>
		private static ActivePurchase GetOpenPurchase(string sku) => null;

		/// <summary>
		/// IAP 4 tracks no unfinished store purchases besides the purchase in flight.
		/// </summary>
		private static void ForgetOpenPurchase(string sku, long txid) { }

		/// <summary>
		/// Finish the order in the store, so it is not delivered again.
		/// </summary>
		private void ConfirmStoreOrder(Product product, CompletedTransaction transaction) => _storeController.ConfirmPendingPurchase(product);

		/// <summary>
		/// IAP 4 keeps no record of the orders being fulfilled.
		/// </summary>
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

		/// <summary>
		/// Handle failed initialization by logging the error and failing the initialization.
		/// </summary>
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

			// Products restored at startup, or delivered while another purchase is in flight, are fulfilled
			// without a txid and must not complete the active purchase's promise.
			var purchase = IsActivePurchase(args.purchasedProduct.definition.id) ? _purchase : null;
			var transaction = CreateTransaction(purchase?.Txid ?? 0, UnityPurchaseReceipt.GetReceipt(args), args.purchasedProduct);
			FulfillTransaction(transaction, args.purchasedProduct, purchase, purchase?.PlayerId ?? 0);

			return PurchaseProcessingResult.Pending;
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
	/// <summary>
	/// A Beamable SKU's product id paired with the IAP store it belongs to.
	/// </summary>
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

	/// <summary>
	/// Registers UnityBeamablePurchaser as the Beamable purchaser.
	/// </summary>
	[BeamContextSystem]
	public static class UnityBeamablePurchaserRegister
	{
		/// <summary>
		/// Replace the registered purchaser with UnityBeamablePurchaser, unless the purchasing implementation
		/// is disabled.
		/// </summary>
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
	/// <summary>
	/// Helpers for building Unity IAP 4 product ids from Beamable SKUs.
	/// </summary>
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

	/// <summary>
	/// Thrown when Beamable's Unity IAP purchaser fails to initialize.
	/// </summary>
	public class BeamableIAPInitializationException : Exception
	{
#if UNITY_PURCHASING_5_OR_NEWER
		/// <summary>
		/// The Beamable initialization status at the time of failure. Unity IAP v5 surfaces
		/// initialization problems as a status + message rather than the legacy
		/// <c>InitializationFailureReason</c> enum, so v5 throws through this constructor.
		/// </summary>
		public PurchasingInitializationStatus Status { get; }

		/// <summary>
		/// Create the exception for a failed initialization status and its message.
		/// </summary>
		public BeamableIAPInitializationException(PurchasingInitializationStatus status, string message) : base(
			$"Beamable IAP failed due to: {status} ({message})")
		{
			Status = status;
		}
#else
		/// <summary>
		/// The reason Unity IAP gave for the failed initialization.
		/// </summary>
		public InitializationFailureReason Reason { get; }

		/// <summary>
		/// Create the exception for the reason Unity IAP gave.
		/// </summary>
		public BeamableIAPInitializationException(InitializationFailureReason reason) : base(
			$"Beamable IAP failed due to: {reason}")
		{
			Reason = reason;
		}
#endif
	}


	/// <summary>
	/// Helper class for Unity IAP receipts.
	/// It takes a receipt payload and parses it into a UnityPurchaseReceipt.
	/// </summary>
	[Serializable]
	public class UnityPurchaseReceipt
	{
		/// <summary>
		/// The name of the store that issued the receipt.
		/// </summary>
		public string Store;

		/// <summary>
		/// The store's id of the purchase transaction.
		/// </summary>
		public string TransactionID;

		/// <summary>
		/// The store's own receipt data, e.g. the App Store receipt or the Google Play purchase.
		/// </summary>
		public string Payload;

#if !BEAMABLE_PURCHASING_IMPLEMENTATION_DISABLED && !UNITY_PURCHASING_5_OR_NEWER // PurchaseEventArgs is obsolete in IAP 5.
		/// <summary>
		///  Returns the payload of the receipt.
		/// </summary>
		/// <param name="eventArgs">EventArgs for the purchase.</param>
		/// <returns>The receipt payload, or the whole receipt when it has no payload or can't be parsed.</returns>
		public static string GetReceipt(PurchaseEventArgs eventArgs)
		{
			if (eventArgs.purchasedProduct.hasReceipt)
			{
				try
				{
					var receipt = JsonUtility.FromJson<UnityPurchaseReceipt>(eventArgs.purchasedProduct.receipt);
					InAppPurchaseLogger.Log($"UnityIAP Payload: {receipt.Payload}");
					InAppPurchaseLogger.Log($"UnityIAP Raw Receipt: {eventArgs.purchasedProduct.receipt}");
					if (!string.IsNullOrEmpty(receipt.Payload))
					{
						return receipt.Payload;
					}
				}
				catch
				{
					// ignored
				}
			}

			return eventArgs.purchasedProduct.receipt;
		}
#endif
	}
}
