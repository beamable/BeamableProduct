using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Payments federation. A microservice implements this to own a payment provider end to end:
	/// starting a purchase with the provider, proving a receipt is genuine, and granting what was
	/// bought.
	///
	/// <para>Beamable keeps the transaction ledger, the state machine and replay protection; the
	/// federation supplies only the provider-specific parts. That split is what makes a provider we
	/// have never heard of usable without a platform change, and what lets payments be used without
	/// Beamable's own store or inventory.</para>
	///
	/// <para><b>Fulfillment is part of this interface on purpose.</b> A transaction may not be
	/// reported complete until the goods exist, so the same party that can prove payment is also the
	/// party that confirms delivery. If fulfillment lived outside the contract, the platform would
	/// have to call some particular store to finish a purchase, and the decoupling would be lost.</para>
	///
	/// <para>The typical flow, for an on-device store:</para>
	/// <list type="number">
	/// <item><description>The game asks Beamable to begin a purchase. Beamable records a transaction
	/// and calls <see cref="BeginPayment"/>.</description></item>
	/// <item><description>The player completes the OS-level payment and the device produces a
	/// receipt.</description></item>
	/// <item><description>The game sends the receipt back. Beamable calls
	/// <see cref="VerifyPayments"/> and matches the result to the transaction.</description></item>
	/// <item><description>Beamable calls <see cref="FulfillPayment"/>; once it confirms, the
	/// transaction reads COMPLETED.</description></item>
	/// </list>
	/// </summary>
	/// <example>
	/// <code>
	///    [Microservice("payments")]
	///    public class PaymentsService : Microservice, IFederatedPayments&lt;MyStore&gt;
	///    {
	///        public async Promise&lt;FederatedBeginPaymentResponse&gt; BeginPayment(
	///            string playerId, FederatedPaymentDetails details)
	///        {
	///            var order = await MyStore.CreateOrder(playerId, details.providerProductId);
	///            return new FederatedBeginPaymentResponse { providerOrderId = order.Id };
	///        }
	///
	///        public async Promise&lt;FederatedVerifyPaymentsResponse&gt; VerifyPayments(
	///            string playerId, List&lt;long&gt; transactionIds, string receipt)
	///        {
	///            var parsed = await MyStore.Verify(receipt);
	///            // Only claim the transaction the receipt actually covers.
	///            return new FederatedVerifyPaymentsResponse
	///            {
	///                verified = new Dictionary&lt;long, FederatedReceiptDetails&gt;
	///                {
	///                    [transactionIds[0]] = new FederatedReceiptDetails
	///                    {
	///                        providerTransactionId = parsed.OrderId,
	///                        productId = parsed.ProductId,
	///                        replayGuard = parsed.OrderId
	///                    }
	///                }
	///            };
	///        }
	///
	///        public async Promise&lt;bool&gt; VerifyReceipt(string receipt)
	///            =&gt; await MyStore.IsReceiptValid(receipt);
	///
	///        public async Promise&lt;FederatedFulfillPaymentResponse&gt; FulfillPayment(
	///            string playerId, long transactionId, string goods)
	///        {
	///            // `goods` is opaque to Beamable and was frozen when the purchase began.
	///            var granted = await MyInventory.Grant(playerId, goods);
	///            return new FederatedFulfillPaymentResponse { fulfilled = granted };
	///        }
	///    }
	/// </code>
	/// </example>
	public interface IFederatedPayments<in T> : IFederation where T : IFederationId, new()
	{
		/// <summary>
		/// Start a purchase with the provider. Called after Beamable has recorded the transaction, so
		/// a throw here leaves the transaction FAILED rather than orphaned.
		/// </summary>
		/// <param name="playerId">The purchasing player.</param>
		/// <param name="details">What is being bought, including the provider's own product id.</param>
		Promise<FederatedBeginPaymentResponse> BeginPayment(
			string playerId,
			FederatedPaymentDetails details);

		/// <summary>
		/// Prove a receipt is genuine and say which transactions it covers.
		///
		/// <para>Takes a LIST of candidate transaction ids because a player may have started a
		/// purchase that never completed — a crash between payment and receipt — and a later receipt
		/// has to be matchable against those earlier attempts. Return an entry only for a transaction
		/// the receipt genuinely covers; anything you claim here will be fulfilled.</para>
		/// </summary>
		/// <param name="playerId">The player whose receipt this is.</param>
		/// <param name="transactionIds">Candidate transactions the receipt might settle.</param>
		/// <param name="receipt">The receipt, exactly as the client supplied it.</param>
		Promise<FederatedVerifyPaymentsResponse> VerifyPayments(
			string playerId,
			List<long> transactionIds,
			string receipt);

		/// <summary>
		/// Check whether a receipt is valid, changing no state. Used for diagnostics and for
		/// pre-flight checks; it never settles a transaction.
		/// </summary>
		/// <param name="receipt">The receipt, exactly as the client supplied it.</param>
		Promise<bool> VerifyReceipt(string receipt);

		/// <summary>
		/// Grant what was bought, and report whether the goods now exist.
		///
		/// <para>Return <c>fulfilled = false</c> (with a reason) rather than throwing when the purchase
		/// legitimately cannot be delivered — the transaction is then recorded FAILED with your reason.
		/// Do NOT return true optimistically: Beamable reports the transaction COMPLETED on the
		/// strength of this answer, and a player who is charged for goods that never arrive has no
		/// other signal that something went wrong.</para>
		///
		/// <para>Should be idempotent. A retry must not grant twice.</para>
		/// </summary>
		/// <param name="playerId">The player the goods are owed to.</param>
		/// <param name="transactionId">The Beamable transaction being fulfilled.</param>
		/// <param name="goods">
		/// The opaque payload frozen when the purchase began. Beamable stores it and hands it back
		/// untouched; its shape is agreed between whoever starts the purchase and whoever fulfills it.
		/// Empty when the purchase carried none.
		/// </param>
		Promise<FederatedFulfillPaymentResponse> FulfillPayment(
			string playerId,
			long transactionId,
			string goods);
	}

	/// <summary>
	/// What is being purchased. Mirrors the transaction details Beamable records, so a federation can
	/// read the provider's product id without needing to know anything about Beamable's stores.
	/// </summary>
	[Serializable]
	public class FederatedPaymentDetails
	{
		/// <summary>Price in cents.</summary>
		public int price;

		/// <summary>Quantity purchased; generally 1.</summary>
		public int quantity = 1;

		/// <summary>Beamable's name for the sku used as the purchase price.</summary>
		public string sku;

		/// <summary>The provider's own product identifier — what you look up on your side.</summary>
		public string providerProductId;

		/// <summary>In-game name of the thing purchased (GOLD, GEMS, ...).</summary>
		public string name;

		/// <summary>In-game reference/id of the thing purchased.</summary>
		public string reference;

		/// <summary>In-game location of the purchase (Dialog, Speed-up, ...).</summary>
		public string gameplace;

		/// <summary>Local currency code; defaults to USD.</summary>
		public string localCurrency;

		/// <summary>Local price as reported by the client.</summary>
		public string localPrice;
	}

	[Serializable]
	public class FederatedBeginPaymentResponse
	{
		/// <summary>
		/// Provider-side identifier for the started purchase, when the provider mints one before a
		/// receipt exists. Recorded for support and reconciliation. Optional.
		/// </summary>
		public string providerOrderId;

		/// <summary>
		/// Where to send the player to complete payment, for providers using a hosted checkout rather
		/// than an on-device flow. Leave empty for on-device providers.
		/// </summary>
		public string redirectUrl;
	}

	[Serializable]
	public class FederatedVerifyPaymentsResponse
	{
		/// <summary>
		/// Beamable transaction id to the receipt facts that settle it. Most providers put a single
		/// transaction in a receipt, so this usually has one entry.
		/// </summary>
		public Dictionary<long, FederatedReceiptDetails> verified =
			new Dictionary<long, FederatedReceiptDetails>();
	}

	[Serializable]
	public class FederatedReceiptDetails
	{
		/// <summary>The provider's transaction id, read out of the receipt.</summary>
		public string providerTransactionId;

		/// <summary>
		/// The product id the receipt actually covers. Beamable compares this against what the
		/// transaction claimed and rejects a mismatch as a tampering attempt, so report what the
		/// receipt says rather than what was expected.
		/// </summary>
		public string productId;

		/// <summary>
		/// The value that uniquely identifies this payment for replay protection. Beamable refuses a
		/// second verification carrying the same value. Leave empty to use
		/// <see cref="providerTransactionId"/>; set it when the provider has a better single-use key.
		/// </summary>
		public string replayGuard;
	}

	[Serializable]
	public class FederatedFulfillPaymentResponse
	{
		/// <summary>True only once the goods are durably granted. See <see cref="IFederatedPayments{T}.FulfillPayment"/>.</summary>
		public bool fulfilled;

		/// <summary>Why fulfillment was refused. Recorded on the transaction history.</summary>
		public string reason;
	}
}
