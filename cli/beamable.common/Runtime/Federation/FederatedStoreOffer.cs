using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Federation for a store's offers. A microservice implements this to expose a catalog the Portal can
	/// author campaigns against, and to grant / revoke / redeem those offers for a player.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface is the extension point, not a Beamable feature with an interface bolted on. Beamable
	/// ships <c>BeamableStoreOfferService</c> (federation id <c>beamable_store</c>) as the default
	/// implementation over its own commerce, but a game that sells through Steam, a console store, or its
	/// own web shop implements this same interface under its own <see cref="FederationIdAttribute"/> and is
	/// treated identically by the gateway, the campaign runtime, and the Portal. Nothing outside a given
	/// implementation may branch on which federation id it is talking to.
	/// </para>
	/// <para>
	/// The DTOs below are deliberately generic for that reason — a store maps its own catalog onto
	/// <see cref="OfferItem"/> the way a message rail maps its own provider onto
	/// <see cref="MessageRailPayload"/>. <see cref="OfferItem.properties"/> and
	/// <see cref="OfferGrantContext.extraDataFed"/> are the escape hatches for anything this contract does
	/// not name, so a provider does not need a contract version bump to carry its own data.
	/// </para>
	/// <para>
	/// A federation id must be <c>[A-Za-z][A-Za-z0-9_]*</c> — the source generator rejects anything else
	/// (BEAM_FED_0004), because the id becomes a route segment and a generated client member.
	/// </para>
	/// </remarks>
	public interface IFederatedStoreOffer<in T> : IFederation where T : IFederationId, new()
	{
		/// <summary>
		/// The offers this store can sell right now. Backs the Portal's offer picker, so it is called
		/// interactively and should be cheap; page with <see cref="OfferCatalogResponse.nextCursor"/>
		/// rather than returning an unbounded catalog.
		/// </summary>
		Promise<OfferCatalogResponse> ListOffers(OfferQuery query);

		/// <summary>
		/// One offer by id, plus whether it can still be granted. A campaign may have been authored months
		/// before it sends, so <see cref="OfferDetailsResponse.available"/> is a live answer, not a copy of
		/// what the catalog said at authoring time.
		/// </summary>
		Promise<OfferDetailsResponse> GetOffer(string offerId);

		/// <summary>
		/// Entitle a player to an offer, returning the grant that represents it. Called by the campaign
		/// runtime as a send goes out, so the resulting <see cref="OfferGrantResponse.grantId"/> can ride
		/// the message the player receives.
		///
		/// <para>
		/// Must be safe to call again for the same <see cref="OfferGrantContext.outreachId"/>: the send is
		/// retried on any retriable failure downstream, and a store that double-grants would pay out twice
		/// for one outreach. Return the existing grant rather than a second one.
		/// </para>
		/// </summary>
		Promise<OfferGrantResponse> GrantOffer(string playerId, string offerId, OfferGrantContext context);

		/// <summary>
		/// Withdraw a grant that has not been redeemed. Used when a campaign is deactivated or an operator
		/// pulls an offer back.
		/// </summary>
		Promise<OfferGrantResponse> RevokeOffer(string playerId, string grantId);

		/// <summary>
		/// Consume a grant — the player claiming what they were offered. Client-callable through the
		/// gateway, so implementations must treat <paramref name="playerId"/> as already authorized by the
		/// caller and must be idempotent on <see cref="OfferRedeemRequest.transactionId"/>.
		/// </summary>
		Promise<OfferRedeemResponse> RedeemOffer(string playerId, string grantId, OfferRedeemRequest request);

		/// <summary>
		/// Every grant this store is currently holding for a player, in any state. The read side of the
		/// grant lifecycle — what the player can still claim, and what they already did.
		/// </summary>
		Promise<OfferEntitlementsResponse> GetPlayerEntitlements(string playerId);

		/// <summary>
		/// A purchase settled somewhere the federation does not control (a platform store callback, an IAP
		/// receipt), letting the store close out the matching grant. Delivered at-least-once, so it must be
		/// idempotent on <see cref="OfferPurchaseNotification.transactionId"/>.
		/// </summary>
		Promise<OfferPurchaseAck> OnPurchaseCompleted(OfferPurchaseNotification notification);
	}

	/// <summary>What the Portal's picker is asking the store for. Every field is optional.</summary>
	[Serializable]
	public class OfferQuery
	{
		public string search;
		public List<string> tags = new List<string>();

		/// <summary>Page size. 0 means "the store's own default" — never "no offers".</summary>
		public int limit;

		/// <summary>Opaque continuation from <see cref="OfferCatalogResponse.nextCursor"/>.</summary>
		public string cursor;
	}

	[Serializable]
	public class OfferItem
	{
		/// <summary>
		/// The store's own reference for this offer, and the value written to a campaign send node's
		/// <c>Offer</c>. Opaque to everything outside the store that issued it.
		/// </summary>
		public string offerId;

		public string title;
		public string description;
		public string imageUrl;

		/// <summary>
		/// Already formatted for display ("$4.99", "1200 Gems"). A string rather than an amount plus a
		/// currency code because only the store knows how it prices — and the Portal only ever shows this.
		/// </summary>
		public string priceLabel;

		public List<string> tags = new List<string>();

		/// <summary>
		/// Anything this contract does not name. The escape hatch that lets a store carry its own fields
		/// through to its own Portal extension without a contract version bump.
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	[Serializable]
	public class OfferCatalogResponse
	{
		public List<OfferItem> offers = new List<OfferItem>();

		/// <summary>Pass back as <see cref="OfferQuery.cursor"/> for the next page. Empty on the last page.</summary>
		public string nextCursor;
	}

	[Serializable]
	public class OfferDetailsResponse
	{
		public OfferItem offer;

		/// <summary>Whether this offer can be granted right now, which the catalog cannot promise later.</summary>
		public bool available;

		/// <summary>Operator-facing reason when <see cref="available"/> is false. Shown, so write it for a human.</summary>
		public string unavailableReason;
	}

	/// <summary>
	/// Why a grant is happening, so a store can attribute it. Every field is campaign bookkeeping the store
	/// may record and hand back, but must not require — a grant issued outside a campaign carries none of it.
	/// </summary>
	[Serializable]
	public class OfferGrantContext
	{
		public string campaignId;
		public string campaignVersion;
		public string nodeId;

		/// <summary>
		/// The per-recipient join key the campaign and the message rail share. Also the idempotency key for
		/// <see cref="IFederatedStoreOffer{T}.GrantOffer"/> — see that method's remarks.
		/// </summary>
		public string outreachId;

		/// <summary>
		/// The store's own authored fields from the campaign send, keyed under
		/// <see cref="StoreOfferContract.KeyPrefix"/> — never the message rail's.
		///
		/// <para>
		/// For a provider that serves a catalog this is optional colour. For one that mints an offer per
		/// campaign — authoring it in its Portal extension rather than looking it up — <b>this is the offer
		/// itself</b>, and <see cref="IFederatedStoreOffer{T}.GrantOffer"/> has nothing to grant without it.
		/// </para>
		/// </summary>
		public Dictionary<string, string> extraDataFed = new Dictionary<string, string>();

		/// <summary>When the grant should stop being claimable. 0 = the store's own default.</summary>
		public long expiresAtUnixSeconds;
	}

	[Serializable]
	public class OfferGrantResponse
	{
		public string playerId;
		public string offerId;

		/// <summary>The store's handle on this entitlement. Required when <see cref="success"/> is true.</summary>
		public string grantId;

		public bool success;

		/// <summary>
		/// A <see cref="StoreOfferContract"/> status on failure, or the store's own string. Empty on success.
		/// </summary>
		public string status;

		public string message;

		/// <summary>
		/// Whether the caller should try again. The campaign runtime keeps the send pending and retries when
		/// this is true, and delivers the message without an offer when it is false — so a store that marks a
		/// permanent failure retriable will stall the outreach rather than degrade it.
		/// </summary>
		public bool retriable;
	}

	[Serializable]
	public class OfferRedeemRequest
	{
		/// <summary>The caller's idempotency key. A repeat must return the first result, not redeem twice.</summary>
		public string transactionId;

		public Dictionary<string, string> @params = new Dictionary<string, string>();
	}

	[Serializable]
	public class OfferRedeemResponse
	{
		public string grantId;
		public bool success;
		public string status;
		public string message;
	}

	[Serializable]
	public class OfferEntitlement
	{
		public string grantId;
		public string offerId;

		/// <summary>One of <see cref="StoreOfferContract"/>'s entitlement states.</summary>
		public string state;

		public long grantedAtUnixSeconds;

		/// <summary>0 when the grant does not expire.</summary>
		public long expiresAtUnixSeconds;
	}

	[Serializable]
	public class OfferEntitlementsResponse
	{
		public string playerId;
		public List<OfferEntitlement> entitlements = new List<OfferEntitlement>();
	}

	[Serializable]
	public class OfferPurchaseNotification
	{
		public string playerId;

		/// <summary>The grant being settled, when the purchase can be tied to one. May be empty.</summary>
		public string grantId;

		public string offerId;

		/// <summary>The payment processor's id for this purchase. The idempotency key for the callback.</summary>
		public string transactionId;

		public string sku;

		/// <summary>ISO 4217, e.g. "USD".</summary>
		public string currency;

		/// <summary>Minor units, to keep money off floating point.</summary>
		public long amountCents;

		public long completedAtUnixSeconds;
	}

	[Serializable]
	public class OfferPurchaseAck
	{
		public bool acknowledged;
		public string message;
	}

	/// <summary>
	/// Shared wire vocabulary for the store-offer federation contract, so every implementation, the Beamable
	/// backend, and the Portal agree on the exact strings. Mirrors <see cref="MessageRailContract"/>'s role
	/// for the message rail.
	/// </summary>
	public static class StoreOfferContract
	{
		/// <summary>
		/// The campaign payload key carrying the authored offer reference. Reserved by the campaign — mirrors
		/// <c>CampaignSendPayload.ReservedKeys</c> in the Beamable backend.
		/// </summary>
		public const string OfferKey = "offer";

		/// <summary>
		/// The campaign payload key carrying the <see cref="OfferGrantResponse.grantId"/> of the grant made
		/// for this send, so the message rail can deep-link the player straight to what they were given.
		/// Also reserved — a rail must not emit it.
		/// </summary>
		public const string GrantKey = "beam_offer_grant";

		/// <summary>
		/// The namespace every offer provider's authored fields sit in inside a campaign send's payload.
		///
		/// <para>
		/// Load-bearing, not cosmetic: a lane's message rail and its offer provider spread their authored
		/// data into the <b>same</b> <c>customProperties</c> map, and nothing else in a stored graph tells
		/// the two apart. This prefix is what routes each half back to the extension that wrote it when a
		/// campaign is reopened, and what lets the campaign runtime hand the store its own fields —
		/// and only its own — in <see cref="OfferGrantContext.extraDataFed"/>.
		/// </para>
		/// </summary>
		public const string KeyPrefix = "offer_";

		// --- Grant / redeem failure statuses -------------------------------

		/// <summary>The offer exists but cannot be granted right now (sold out, region-locked, expired).</summary>
		public const string UnavailableStatus = "unavailable";

		/// <summary>
		/// This outreach was already granted. Not an error — the expected answer to a retried grant, and
		/// implementations should return the original <see cref="OfferGrantResponse.grantId"/> alongside it.
		/// </summary>
		public const string AlreadyGrantedStatus = "already-granted";

		/// <summary>The store is rate-limiting or shedding load. Retriable.</summary>
		public const string OverCapacityStatus = "over-capacity";

		/// <summary>No such grant, or it does not belong to this player.</summary>
		public const string UnknownGrantStatus = "unknown-grant";

		// --- Entitlement states -------------------------------------------

		/// <summary>Granted and still claimable.</summary>
		public const string StateGranted = "granted";

		/// <summary>Claimed by the player.</summary>
		public const string StateRedeemed = "redeemed";

		/// <summary>Withdrawn before it was claimed.</summary>
		public const string StateRevoked = "revoked";

		/// <summary>Passed <see cref="OfferEntitlement.expiresAtUnixSeconds"/> unclaimed.</summary>
		public const string StateExpired = "expired";
	}
}
