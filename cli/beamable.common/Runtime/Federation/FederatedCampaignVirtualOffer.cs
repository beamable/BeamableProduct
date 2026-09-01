using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Federation for a store's <b>virtual</b> offers — ones a player buys with soft currency. A microservice
	/// implements this to expose a catalog the Portal can author campaigns against, and to grant / revoke /
	/// redeem those offers for a player.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Virtual, deliberately.</b> A real-money offer is a different federation, because it is a different
	/// problem: it needs platform product ids, a native purchase flow, receipt verification and a settlement
	/// callback from outside Beamable, none of which a soft-currency offer has any use for. Carrying both on
	/// one interface meant every virtual provider inherited fields it could not fill. So the price on this
	/// contract is a currency amount, and a provider that finds a real-money listing in its catalog should
	/// refuse it rather than describe it.
	/// </para>
	/// <para>
	/// This interface is the extension point, not a Beamable feature with an interface bolted on. Beamable
	/// ships <c>BeamableCampaignOfferService</c> (federation id <c>beamable_virtual_store</c>) as the default
	/// implementation over its own commerce, but a game with its own virtual economy implements this same
	/// interface under its own <see cref="FederationIdAttribute"/> and is treated identically by the gateway,
	/// the campaign runtime, and the Portal. Nothing outside a given implementation may branch on which
	/// federation id it is talking to.
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
	public interface IFederatedCampaignVirtualOffer<in T> : IFederation where T : IFederationId, new()
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
		/// Already formatted for display ("1200 Gems"), for surfaces that only ever print it.
		///
		/// <para>
		/// <b>Never the only representation of a price.</b> It is neither localizable nor comparable, so a
		/// client that has to decide whether the player can afford this, or to show the cost against a
		/// balance, needs <see cref="CampaignOfferListingRef.price"/> instead — its
		/// <see cref="CampaignOfferPrice.symbol"/> and <see cref="CampaignOfferPrice.amount"/>.
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
		/// What the player gets for buying this offer — the bundle's contents, itemised.
		///
		/// <para>
		/// <b>Disclosure, not a fulfilment instruction.</b> The store still fulfils however it fulfils;
		/// this exists so a surface can tell the player what they are about to buy. Nothing consumes it
		/// to grant anything, and a client must never reconcile it against what actually landed — a loot
		/// roll, a VIP multiplier or a store-side promotion can legitimately make the two differ.
		/// </para>
		///
		/// <para>
		/// Empty is legitimate and must render: a provider that cannot enumerate its payout (an opaque
		/// third-party bundle) leaves it empty, and a client falls back to
		/// <see cref="description"/>. Do not treat empty as "this offer gives nothing".
		/// </para>
		/// </summary>
		public List<CampaignOfferReward> rewards = new List<CampaignOfferReward>();

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

		/// <summary>
		/// Anything this contract does not name about the listing specifically. Same escape hatch as
		/// <see cref="CampaignOfferItem.properties"/>, at listing scope — so a store with two listings
		/// on one offer does not have to hoist per-listing data up to the item and re-key it.
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	/// <summary>
	/// What a listing costs, structured rather than pre-formatted.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A label alone is enough for the Portal and useless to a game client: to spend on a player's behalf,
	/// or to show them whether they can afford it, a client needs the currency and the amount as numbers.
	/// So <see cref="symbol"/> and <see cref="amount"/> are the price, and <see cref="label"/> is a
	/// convenience on top of them.
	/// </para>
	/// <para>
	/// <b>Soft currency only.</b> There is no real-money price here and no platform product ids — a
	/// real-money offer belongs to its own federation. A provider whose catalog contains a SKU-priced
	/// listing must not describe it through this contract; see the interface remarks.
	/// </para>
	/// </remarks>
	[Serializable]
	public class CampaignOfferPrice
	{
		/// <summary>
		/// <c>"currency"</c> for the ordinary case, or the store's own vocabulary. Never a real-money type —
		/// a listing priced in money is not representable here.
		/// </summary>
		public string type;

		/// <summary>The currency symbol this is priced in.</summary>
		public string symbol;

		/// <summary>How much of <see cref="symbol"/> the listing costs.</summary>
		public long amount;

		/// <summary>Already formatted for display. A convenience, never the only representation.</summary>
		public string label;

		/// <summary>
		/// Anything this contract does not name about the price. A store with its own pricing concept
		/// (a regional tier, a subscription interval, a bundle discount) carries it here rather than
		/// encoding it into <see cref="label"/>, which is display-only.
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
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

		/// <summary>
		/// Anything this contract does not name about why. Lets a store carry the structured form of
		/// what <see cref="message"/> says in prose — the requirement's id, the threshold, the reset
		/// time — so a client can build its own copy without parsing English.
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	/// <summary>
	/// One thing an offer gives the player. A bundle is a list of these.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The federation contract deliberately carries no opinion about what a "product" is: Beamable's own
	/// commerce happens to grant currencies, items, entitlements and loot rolls, but a store selling
	/// through Steam or a console grants whatever that platform grants. So <see cref="type"/> is an open
	/// string and <see cref="symbol"/> is opaque, exactly like <see cref="CampaignOfferItem.offerId"/>.
	/// </para>
	/// <para>
	/// <b>Never switch exhaustively on <see cref="type"/>.</b> A client renders the types it knows and
	/// falls back to <see cref="title"/> (then <see cref="symbol"/>) for the rest — a provider is free
	/// to invent a type this contract predates, and a client that treats an unknown type as an error
	/// breaks the extension point.
	/// </para>
	/// </remarks>
	[Serializable]
	public class CampaignOfferReward
	{
		/// <summary>
		/// What kind of thing this is. Beamable's own provider emits
		/// <see cref="CampaignOfferContract.RewardCurrency"/>,
		/// <see cref="CampaignOfferContract.RewardItem"/>,
		/// <see cref="CampaignOfferContract.RewardEntitlement"/> and
		/// <see cref="CampaignOfferContract.RewardLootRoll"/>; a third-party store may emit its own.
		/// </summary>
		public string type;

		/// <summary>
		/// The store's own reference for the thing granted — a currency id, a content id, an
		/// entitlement symbol. Opaque: only the store that issued it can interpret it.
		/// </summary>
		public string symbol;

		/// <summary>
		/// How many. <c>1</c> for a single item, a quantity for currency. <c>0</c> where the amount is
		/// not known until fulfilment, which is the normal case for
		/// <see cref="CampaignOfferContract.RewardLootRoll"/> — so a client must not render <c>0</c> as
		/// "nothing".
		/// </summary>
		public long amount;

		/// <summary>Display name, when the store has one. Falls back to <see cref="symbol"/>.</summary>
		public string title;

		/// <summary>Icon or art for this reward, when the store has one.</summary>
		public string imageUrl;

		/// <summary>
		/// Anything this contract does not name about the reward — item properties, an entitlement
		/// specialization, a rarity, a duration. The escape hatch that keeps
		/// <see cref="type"/> from having to grow a field per product kind.
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	[Serializable]
	public class CampaignOfferCatalogResponse
	{
		public List<CampaignOfferItem> offers = new List<CampaignOfferItem>();

		/// <summary>Pass back as <see cref="CampaignOfferQuery.cursor"/> for the next page. Empty on the last page.</summary>
		public string nextCursor;

		/// <summary>
		/// Anything this contract does not name about the catalog as a whole.
		///
		/// <para>
		/// The reason it exists: an empty <see cref="offers"/> list is ambiguous, and the two things it can
		/// mean need different words in front of an operator. "This realm has authored nothing yet" is a
		/// normal starting state. "This store has listings but none it can offer here" is a filter the
		/// operator cannot see and will not guess — they published a listing and it is simply absent.
		/// Without somewhere to say so, a picker can only report the absence.
		/// </para>
		/// <para>
		/// <see cref="CampaignOfferContract.CatalogWithheldCountKey"/> and
		/// <see cref="CampaignOfferContract.CatalogWithheldReasonKey"/> are the well-known keys for that.
		/// A store with its own reason to withhold entries uses the same pair — the reason is a sentence
		/// the store writes, so nothing consuming it has to know what was filtered or why.
		/// </para>
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
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
		/// <see cref="IFederatedCampaignVirtualOffer{T}.GrantOffer"/> — see that method's remarks.
		/// </summary>
		public string outreachId;

		/// <summary>
		/// The store's own authored fields from the campaign send, keyed under
		/// <see cref="CampaignOfferContract.KeyPrefix"/> — never the message rail's.
		///
		/// <para>
		/// For a provider that serves a catalog this is optional colour. For one that mints an offer per
		/// campaign — authoring it in its Portal extension rather than looking it up — <b>this is the offer
		/// itself</b>, and <see cref="IFederatedCampaignVirtualOffer{T}.GrantOffer"/> has nothing to grant without it.
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
		/// Carried here so that <see cref="IFederatedCampaignVirtualOffer{T}.GetPlayerEntitlements"/> alone is
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

		// --- Catalog-level properties ---------------------------------------
		//
		// Well-known keys on CampaignOfferCatalogResponse.properties. Both are optional; a surface that
		// does not recognise them shows the catalog exactly as before.

		/// <summary>
		/// How many catalog entries the store declined to offer, as a decimal string. Present only when
		/// non-zero, so its absence means "nothing was withheld" rather than "unknown".
		/// </summary>
		public const string CatalogWithheldCountKey = "withheldCount";

		/// <summary>
		/// Why those entries were withheld, as a sentence to show an operator. Written by the store,
		/// because only the store knows what it filtered — a consumer renders it and does not interpret it.
		/// </summary>
		public const string CatalogWithheldReasonKey = "withheldReason";

		// ── Reward types (CampaignOfferReward.type) ─────────────────────────────────────────────
		//
		// The four kinds Beamable's own commerce can grant. A store is NOT limited to these — the
		// field is an open string precisely so a third-party provider can name its own — so a client
		// must render an unknown type rather than reject it.

		/// <summary>A soft-currency amount. <c>symbol</c> is the currency content id.</summary>
		public const string RewardCurrency = "currency";

		/// <summary>An inventory item. <c>symbol</c> is the item content id.</summary>
		public const string RewardItem = "item";

		/// <summary>
		/// A granted right — DLC, a coupon, tier membership. <c>symbol</c> is the entitlement symbol;
		/// a specialization travels in <c>properties</c>.
		/// </summary>
		public const string RewardEntitlement = "entitlement";

		/// <summary>
		/// A roll against a loot table. Its contents are not known until fulfilment, so
		/// <c>amount</c> is <c>0</c> and the surface should say "contents vary" rather than "nothing".
		/// </summary>
		public const string RewardLootRoll = "lootRoll";
	}
}
