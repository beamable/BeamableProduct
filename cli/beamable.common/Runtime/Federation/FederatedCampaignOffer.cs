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
	/// ships <c>BeamableCampaignOfferService</c> (federation id <c>beamable_store</c>) as the default
	/// implementation over its own commerce, but a game that sells through Steam, a console store, or its
	/// own web shop implements this same interface under its own <see cref="FederationIdAttribute"/> and is
	/// treated identically by the gateway, the campaign runtime, and the Portal. Nothing outside a given
	/// implementation may branch on which federation id it is talking to.
	/// </para>
	/// <para>
	/// The DTOs below are deliberately generic for that reason — a store maps its own catalog onto
	/// <see cref="CampaignOfferItem"/> the way a message rail maps its own provider onto
	/// <see cref="MessageRailPayload"/>. <see cref="CampaignOfferItem.properties"/> and
	/// <see cref="CampaignOfferGrantContext.extraDataFed"/> are the escape hatches for anything this contract does
	/// not name, so a provider does not need a contract version bump to carry its own data.
	/// </para>
	/// <para>
	/// A federation id must be <c>[A-Za-z][A-Za-z0-9_]*</c> — the source generator rejects anything else
	/// (BEAM_FED_0004), because the id becomes a route segment and a generated client member.
	/// </para>
	/// </remarks>
	public interface IFederatedCampaignOffer<in T> : IFederation where T : IFederationId, new()
	{
		/// <summary>
		/// The offers this store can sell right now. Backs the Portal's offer picker, so it is called
		/// interactively and should be cheap; page with <see cref="CampaignOfferCatalogResponse.nextCursor"/>
		/// rather than returning an unbounded catalog.
		/// </summary>
		Promise<CampaignOfferCatalogResponse> ListOffers(CampaignOfferQuery query);

		/// <summary>
		/// One offer by id, plus whether it can still be granted. A campaign may have been authored months
		/// before it sends, so <see cref="CampaignOfferDetailsResponse.available"/> is a live answer, not a copy of
		/// what the catalog said at authoring time.
		/// </summary>
		Promise<CampaignOfferDetailsResponse> GetOffer(string offerId);

		/// <summary>
		/// Entitle a player to an offer, returning the grant that represents it. Called by the campaign
		/// runtime as a send goes out, so the resulting <see cref="CampaignOfferGrantResponse.grantId"/> can ride
		/// the message the player receives.
		///
		/// <para>
		/// Must be safe to call again for the same <see cref="CampaignOfferGrantContext.outreachId"/>: the send is
		/// retried on any retriable failure downstream, and a store that double-grants would pay out twice
		/// for one outreach. Return the existing grant rather than a second one.
		/// </para>
		/// </summary>
		Promise<CampaignOfferGrantResponse> GrantOffer(string playerId, string offerId, CampaignOfferGrantContext context);

		/// <summary>
		/// Withdraw a grant that has not been redeemed. Used when a campaign is deactivated or an operator
		/// pulls an offer back.
		/// </summary>
		Promise<CampaignOfferGrantResponse> RevokeOffer(string playerId, string grantId);

		/// <summary>
		/// Consume a grant — the player claiming what they were offered. Client-callable through the
		/// gateway, so implementations must treat <paramref name="playerId"/> as already authorized by the
		/// caller and must be idempotent on <see cref="CampaignOfferRedeemRequest.transactionId"/>.
		/// </summary>
		Promise<CampaignOfferRedeemResponse> RedeemOffer(string playerId, string grantId, CampaignOfferRedeemRequest request);

		/// <summary>
		/// Every grant this store is currently holding for a player, in any state. The read side of the
		/// grant lifecycle — what the player can still claim, and what they already did.
		/// </summary>
		Promise<CampaignOfferEntitlementsResponse> GetPlayerEntitlements(string playerId);

		/// <summary>
		/// A purchase settled somewhere the federation does not control (a platform store callback, an IAP
		/// receipt), letting the store close out the matching grant. Delivered at-least-once, so it must be
		/// idempotent on <see cref="CampaignOfferPurchaseNotification.transactionId"/>.
		/// </summary>
		Promise<CampaignOfferPurchaseAck> OnPurchaseCompleted(CampaignOfferPurchaseNotification notification);
	}

	/// <summary>What the Portal's picker is asking the store for. Every field is optional.</summary>
	[Serializable]
	public class CampaignOfferQuery
	{
		public string search;
		public List<string> tags = new List<string>();

		/// <summary>Page size. 0 means "the store's own default" — never "no offers".</summary>
		public int limit;

		/// <summary>Opaque continuation from <see cref="CampaignOfferCatalogResponse.nextCursor"/>.</summary>
		public string cursor;

		/// <summary>
		/// Which language to resolve <see cref="CampaignOfferItem.title"/> and
		/// <see cref="CampaignOfferItem.description"/> into. Empty means the store's own default.
		/// A store that has no localizations simply ignores it.
		/// </summary>
		public string language;
	}

	[Serializable]
	public class CampaignOfferItem
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
		/// Already formatted for display ("$4.99", "1200 Gems"), for surfaces that only ever print it.
		///
		/// <para>
		/// <b>Never the only representation of a price.</b> It is neither localizable nor transactable,
		/// so a client that has to open a native purchase flow needs
		/// <see cref="CampaignOfferListingRef.price"/> instead — in particular its
		/// <see cref="CampaignOfferPrice.productIds"/>.
		/// </para>
		/// </summary>
		public string priceLabel;

		/// <summary>
		/// The storefront listings this offer resolves to, if any.
		///
		/// <para>
		/// <b>Zero or more, deliberately.</b> Empty for a provider that grants directly with no
		/// storefront; one for the ordinary case; more for a bundle. Read it as a list — indexing
		/// <c>listings[0]</c> is how "zero or more" quietly becomes "exactly one", and a provider that
		/// fulfils without a listing is legitimate.
		/// </para>
		/// </summary>
		public List<CampaignOfferListingRef> listings = new List<CampaignOfferListingRef>();

		/// <summary>
		/// Every language this offer has text for, keyed by language code. <see cref="title"/> and
		/// <see cref="description"/> hold the one resolved for <see cref="CampaignOfferQuery.language"/>,
		/// so a caller that does not care about localization can ignore this entirely.
		///
		/// <para>
		/// Carried in full rather than collapsed to the resolved language because a client that switches
		/// language at runtime cannot get the other translations back without a second round trip.
		/// </para>
		/// </summary>
		public Dictionary<string, CampaignOfferText> localizations = new Dictionary<string, CampaignOfferText>();

		public List<string> tags = new List<string>();

		/// <summary>
		/// Anything this contract does not name. The escape hatch that lets a store carry its own fields
		/// through to its own Portal extension without a contract version bump.
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	/// <summary>One language's presentation for an offer.</summary>
	[Serializable]
	public class CampaignOfferText
	{
		public string title;
		public string description;
	}

	/// <summary>
	/// A storefront listing an offer resolves to. Set by a provider that fulfils through a store;
	/// absent for one that grants directly.
	/// </summary>
	/// <remarks>
	/// This is how "the offer is a listing" reaches a client without the contract growing an opinion
	/// about Beamable's own commerce: the symbols are the provider's, and only the provider that
	/// issued them can interpret them.
	/// </remarks>
	[Serializable]
	public class CampaignOfferListingRef
	{
		/// <summary>The store's reference for the listing. Opaque outside that store.</summary>
		public string listingSymbol;

		/// <summary>
		/// Which store the listing belongs to, when the provider has that concept. The Portal's picker
		/// groups by it when present and shows a flat list when it is absent, so a provider with no
		/// store concept needs no special handling.
		/// </summary>
		public string storeSymbol;

		public CampaignOfferPrice price;
	}

	/// <summary>
	/// What a listing costs, structured rather than pre-formatted.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A label alone is enough for the Portal and useless to a game client: to charge for a real-money
	/// listing the client has to address the platform's own product, which is what
	/// <see cref="productIds"/> carries. A client that only has a formatted string can display a price
	/// it cannot take.
	/// </para>
	/// </remarks>
	[Serializable]
	public class CampaignOfferPrice
	{
		/// <summary><c>"sku"</c> for real money, <c>"currency"</c> for soft, or the store's own vocabulary.</summary>
		public string type;

		/// <summary>The SKU or currency symbol this is priced in.</summary>
		public string symbol;

		/// <summary>Soft-currency amount, when <see cref="type"/> is a currency.</summary>
		public long amount;

		/// <summary>Real-money price in minor units, to keep money off floating point. 0 when not priced in money.</summary>
		public long realPriceCents;

		/// <summary>ISO 4217, e.g. "USD".</summary>
		public string currencyCode;

		/// <summary>
		/// Platform product ids for a real-money price — <c>{ "itunes": …, "googleplay": …, "steam": … }</c>.
		/// The handle a mobile or console client needs to open the native purchase flow.
		/// </summary>
		public Dictionary<string, string> productIds = new Dictionary<string, string>();

		/// <summary>Already formatted for display. A convenience, never the only representation.</summary>
		public string label;
	}

	/// <summary>
	/// Why something is not available, in both a machine-readable and a human-readable form.
	/// </summary>
	/// <remarks>
	/// <see cref="code"/> lets a client branch or re-localize; <see cref="message"/> is what to show when
	/// it does neither. A client that can only render a disabled row with no explanation produces a
	/// support ticket, which is what this exists to avoid.
	/// </remarks>
	[Serializable]
	public class CampaignOfferReason
	{
		/// <summary>One of <see cref="CampaignOfferContract"/>'s reason codes, or the store's own.</summary>
		public string code;

		/// <summary>Human-readable, already resolved for the requested language where the store can.</summary>
		public string message;

		/// <summary>Optional extra context — the stat that failed, the limit that was hit.</summary>
		public string detail;
	}

	[Serializable]
	public class CampaignOfferCatalogResponse
	{
		public List<CampaignOfferItem> offers = new List<CampaignOfferItem>();

		/// <summary>Pass back as <see cref="CampaignOfferQuery.cursor"/> for the next page. Empty on the last page.</summary>
		public string nextCursor;
	}

	[Serializable]
	public class CampaignOfferDetailsResponse
	{
		public CampaignOfferItem offer;

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
	public class CampaignOfferGrantContext
	{
		public string campaignId;
		public string campaignVersion;
		public string nodeId;

		/// <summary>
		/// The per-recipient join key the campaign and the message rail share. Also the idempotency key for
		/// <see cref="IFederatedCampaignOffer{T}.GrantOffer"/> — see that method's remarks.
		/// </summary>
		public string outreachId;

		/// <summary>
		/// The store's own authored fields from the campaign send, keyed under
		/// <see cref="CampaignOfferContract.KeyPrefix"/> — never the message rail's.
		///
		/// <para>
		/// For a provider that serves a catalog this is optional colour. For one that mints an offer per
		/// campaign — authoring it in its Portal extension rather than looking it up — <b>this is the offer
		/// itself</b>, and <see cref="IFederatedCampaignOffer{T}.GrantOffer"/> has nothing to grant without it.
		/// </para>
		/// </summary>
		public Dictionary<string, string> extraDataFed = new Dictionary<string, string>();

		/// <summary>When the grant should stop being claimable. 0 = the store's own default.</summary>
		public long expiresAtUnixSeconds;

		/// <summary>
		/// The campaign offer group this grant belongs to, or empty. Bookkeeping the store may record —
		/// it is not what decides anything; see <see cref="invalidatesOfferIds"/>.
		/// </summary>
		public string groupId;

		/// <summary>
		/// The offer ids this grant forfeits when it is purchased, already resolved by the campaign
		/// runtime. Empty means "forfeits nothing".
		///
		/// <para>
		/// <b>A store's whole obligation here is one sentence: on purchase, revoke exactly these.</b> The
		/// campaign's grouping vocabulary — whether the offers stack or are alternatives, whether taking
		/// one forfeits a named sibling or the entire group — is resolved campaign-side and never reaches
		/// this contract. That is deliberate: a Steam or console store should not have to learn campaign
		/// concepts, and the campaign can change how groups work without a contract version bump.
		/// </para>
		/// </summary>
		public List<string> invalidatesOfferIds = new List<string>();
	}

	[Serializable]
	public class CampaignOfferGrantResponse
	{
		public string playerId;
		public string offerId;

		/// <summary>The store's handle on this entitlement. Required when <see cref="success"/> is true.</summary>
		public string grantId;

		public bool success;

		/// <summary>
		/// A <see cref="CampaignOfferContract"/> status on failure, or the store's own string. Empty on success.
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
	public class CampaignOfferRedeemRequest
	{
		/// <summary>The caller's idempotency key. A repeat must return the first result, not redeem twice.</summary>
		public string transactionId;

		public Dictionary<string, string> @params = new Dictionary<string, string>();
	}

	[Serializable]
	public class CampaignOfferRedeemResponse
	{
		public string grantId;
		public bool success;
		public string status;
		public string message;
	}

	[Serializable]
	public class CampaignOfferEntitlement
	{
		public string grantId;
		public string offerId;

		/// <summary>One of <see cref="CampaignOfferContract"/>'s entitlement states.</summary>
		public string state;

		public long grantedAtUnixSeconds;

		/// <summary>0 when the grant does not expire.</summary>
		public long expiresAtUnixSeconds;

		/// <summary>
		/// The offer this grant is for, in full.
		///
		/// <para>
		/// Carried here so that <see cref="IFederatedCampaignOffer{T}.GetPlayerEntitlements"/> alone is
		/// enough to render a store UI — without it every client fans out a <c>GetOffer</c> per row. A
		/// provider that finds this expensive may leave it null and let the client fall back.
		/// </para>
		/// </summary>
		public CampaignOfferItem offer;

		/// <summary>
		/// The listings to open to act on this entitlement. Usually mirrors
		/// <see cref="CampaignOfferItem.listings"/>; a store may narrow it per player.
		/// </summary>
		public List<CampaignOfferListingRef> listings = new List<CampaignOfferListingRef>();

		/// <summary>
		/// Whether the player can act on this right now. Distinct from <see cref="state"/>: a grant can be
		/// <c>granted</c> and still unavailable because a requirement on its listing is unmet.
		/// </summary>
		public bool available;

		/// <summary>Why <see cref="available"/> is false. Empty when it is true.</summary>
		public List<CampaignOfferReason> unavailableReasons = new List<CampaignOfferReason>();
	}

	[Serializable]
	public class CampaignOfferEntitlementsResponse
	{
		public string playerId;
		public List<CampaignOfferEntitlement> entitlements = new List<CampaignOfferEntitlement>();
	}

	[Serializable]
	public class CampaignOfferPurchaseNotification
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
	public class CampaignOfferPurchaseAck
	{
		public bool acknowledged;
		public string message;
	}

	/// <summary>
	/// Shared wire vocabulary for the campaign-offer federation contract, so every implementation, the Beamable
	/// backend, and the Portal agree on the exact strings. Mirrors <see cref="MessageRailContract"/>'s role
	/// for the message rail.
	/// </summary>
	public static class CampaignOfferContract
	{
		/// <summary>
		/// The campaign payload key carrying the authored offer reference. Reserved by the campaign — mirrors
		/// <c>CampaignSendPayload.ReservedKeys</c> in the Beamable backend.
		/// </summary>
		public const string OfferKey = "offer";

		/// <summary>
		/// The campaign payload key carrying the <see cref="CampaignOfferGrantResponse.grantId"/> of the grant made
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
		/// and only its own — in <see cref="CampaignOfferGrantContext.extraDataFed"/>.
		/// </para>
		/// </summary>
		public const string KeyPrefix = "offer_";

		// --- Grant / redeem failure statuses -------------------------------

		/// <summary>The offer exists but cannot be granted right now (sold out, region-locked, expired).</summary>
		public const string UnavailableStatus = "unavailable";

		/// <summary>
		/// This outreach was already granted. Not an error — the expected answer to a retried grant, and
		/// implementations should return the original <see cref="CampaignOfferGrantResponse.grantId"/> alongside it.
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

		/// <summary>Passed <see cref="CampaignOfferEntitlement.expiresAtUnixSeconds"/> unclaimed.</summary>
		public const string StateExpired = "expired";

		// --- Unavailable reason codes ---------------------------------------
		//
		// The codes a client can branch on or re-localize. A store may emit its own instead; a client
		// that does not recognise a code falls back to CampaignOfferReason.message, which is why the
		// message is never optional.

		/// <summary>A player stat requirement on the listing is unmet.</summary>
		public const string ReasonStatRequirement = "stat-requirement";

		/// <summary>Already bought, and the listing does not allow buying it again.</summary>
		public const string ReasonAlreadyPurchased = "already-purchased";

		/// <summary>A purchase limit on the listing has been reached.</summary>
		public const string ReasonPurchaseLimit = "purchase-limit";

		/// <summary>Forfeited by purchasing an offer this one was an alternative to.</summary>
		public const string ReasonForfeited = "forfeited";

		/// <summary>Past its expiry.</summary>
		public const string ReasonExpired = "expired";

		/// <summary>Outside the listing's active period or schedule.</summary>
		public const string ReasonNotActive = "not-active";
	}
}
