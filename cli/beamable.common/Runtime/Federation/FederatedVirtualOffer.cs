using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Federation for a provider's <b>virtual</b> offers — ones a player buys with soft currency, or is simply
	/// given. Beamable's offer registry owns every grant: whether it exists, is visible to the player, was
	/// redeemed or ended. A provider keeps no grant state. It resolves an offer when it is granted, delivers it
	/// when it is redeemed, says whether it is available by its own rules, and undoes what a grant created in
	/// its own service when the grant ends unredeemed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Every call names the grant by its <c>grantId</c>,</b> the registry's id for it and the provider's
	/// idempotency key. A retried call carries the same id and must be answered as the same grant, the same
	/// delivery, the same revoke — never a second one.
	/// </para>
	/// <para>
	/// <b>Virtual, deliberately.</b> A real-money offer is a different federation: it needs platform product
	/// ids, a native purchase flow, receipt verification and a settlement callback from outside Beamable, none
	/// of which a soft-currency offer has any use for.
	/// </para>
	/// <para>
	/// <b>There is no catalog here.</b> The only consumer of a catalog is the provider's own Portal extension,
	/// which reaches its own microservice through ordinary <c>[ServerCallable]</c> methods.
	/// </para>
	/// <para>
	/// Beamable ships its own providers, but a game with its own virtual economy implements this same
	/// interface under its own <see cref="FederationIdAttribute"/> and is treated identically. A federation id
	/// must be <c>[A-Za-z][A-Za-z0-9_]*</c> (BEAM_FED_0004): it becomes a route segment and a generated client
	/// member.
	/// </para>
	/// <para>
	/// Every method takes a list or a named object because the backend posts a JSON body whose fields are the
	/// parameter names (<c>{"grants":[...]}</c>, <c>{"grant":{...},"params":{...}}</c>), so the parameters
	/// must keep these names.
	/// </para>
	/// </remarks>
	public interface IFederatedVirtualOffer<in T> : IFederation where T : IFederationId, new()
	{
		/// <summary>
		/// Resolve grants the registry already wrote, answering <b>exactly one</b>
		/// <see cref="OfferGrantResponse"/> per entry of <paramref name="grants"/>, in the <b>same order</b>.
		///
		/// <para>
		/// Validate the offer, resolve it into the <see cref="OfferGrantResponse.offer"/> the player is shown, and
		/// return whatever you need back on redeem and revoke in
		/// <see cref="OfferGrantResponse.providerMetadata"/>. Do not record the grant: the registry does. A
		/// provider whose offers exist in a third-party service may create them there, keyed on the grant id.
		/// </para>
		///
		/// <para>
		/// Each item stands alone: one failing must not fail the rest — report it on its response
		/// (<see cref="OfferGrantResponse.success"/>, <see cref="OfferGrantResponse.retriable"/>). A grant is
		/// unconditional: a campaign condition is a gate the gateway re-evaluates on read, never seen here.
		/// </para>
		/// </summary>
		Promise<List<OfferGrantResponse>> GrantOffers(List<OfferGrantItem> grants);

		/// <summary>
		/// Undo what grants that ended unredeemed created in your service — expired, withdrawn, forfeited, or
		/// never shown because their message never went out. Answer one <see cref="OfferRevokeResponse"/> per
		/// entry, matched by <see cref="OfferRevokeResponse.grantId"/>.
		///
		/// <para>
		/// Called for every provider: one with nothing to undo answers success. Must work from the grant id
		/// alone, because a grant whose grant answer was lost has no <see cref="OfferGrantRef.offer"/> and no
		/// <see cref="OfferGrantRef.providerMetadata"/>. Idempotent: a repeat answers success.
		/// </para>
		/// </summary>
		Promise<List<OfferRevokeResponse>> RevokeOffer(List<OfferGrantRef> revokes);

		/// <summary>
		/// Deliver a grant the player is redeeming. The registry already claimed it for this redeem and checked
		/// the campaign's gate, so enforce only your own rules (cost, stock).
		///
		/// <para>
		/// Idempotent by grant id: a redeem asked again — after an <see cref="OfferRedeemOutcome.Ambiguous"/>
		/// answer, or a call that failed — is the same delivery and must answer what it already did, never
		/// deliver twice. Answer <see cref="OfferRedeemOutcome.Ambiguous"/> when you cannot tell whether the
		/// delivery happened; the player retries later with the same grant id.
		/// </para>
		/// </summary>
		Promise<OfferRedeemResponse> RedeemOffer(OfferGrantRef grant, Dictionary<string, string> @params);

		/// <summary>
		/// Whether each of a page of the player's grants is available right now by your own rules (stock,
		/// schedule, purchase limits). Answer one <see cref="OfferAvailability"/> per grant, matched by grant
		/// id. The registry lists the grants; this only answers availability, and a provider that does not
		/// answer in time leaves its grants listed without its rules. Never called for a provider whose
		/// <see cref="IOfferProviderId"/> declares <see cref="OfferProviderSettings.hasAvailabilityRules"/>
		/// false.
		/// </summary>
		Promise<List<OfferAvailability>> GetOffers(List<OfferGrantRef> grants);
	}

	// ─── The shared primitive ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// N of something. Serves <b>both</b> sides of a trade: what an offer costs, and what it gives.
	/// </summary>
	/// <remarks>
	/// <b>Never switch exhaustively on <see cref="type"/>.</b> A client renders the types it knows and falls
	/// back to <see cref="title"/> (then <see cref="symbol"/>) for the rest — a provider is free to invent a
	/// type this contract predates.
	/// </remarks>
	[Serializable]
	public class OfferAmount
	{
		/// <summary>
		/// What kind of thing this is: <see cref="OfferContract.AmountCurrency"/>,
		/// <see cref="OfferContract.AmountItem"/>, <see cref="OfferContract.AmountEntitlement"/>,
		/// <see cref="OfferContract.AmountLootRoll"/>, or a provider's own.
		/// </summary>
		public string type;

		/// <summary>The provider's own reference for the thing. Opaque outside the provider that issued it.</summary>
		public string symbol;

		/// <summary>
		/// How many. Always a real quantity: a loot roll is <c>1</c> roll, not <c>0</c> of a prize. On a cost
		/// entry, <c>0</c> means free.
		/// </summary>
		public long amount;

		/// <summary>Display name, when the provider has one. Falls back to <see cref="symbol"/>.</summary>
		public string title;

		public string imageUrl;

		/// <summary>Anything this contract does not name — a rarity, a duration, an item property.</summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	// ─── The offer ─────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// The description of a trade: pay <see cref="cost"/>, receive <see cref="rewards"/>. Resolved by the
	/// provider when the offer is granted and kept with the grant, so editing a catalog afterwards does not
	/// change what a player was given.
	/// </summary>
	/// <remarks>
	/// <see cref="offerId"/> is the only required field, which lets a provider with no storefront describe an
	/// offer without inventing concepts it does not have.
	/// </remarks>
	[Serializable]
	public class OfferItem
	{
		/// <summary>
		/// The provider's own reference for this offer, and the value written to an offer entry on a campaign
		/// send node. Opaque — do not parse it or key anything durable on its shape. A provider may encode its
		/// own routing into it.
		/// </summary>
		public string offerId;

		public string title;
		public string description;
		public string imageUrl;

		/// <summary>What the player pays. Several entries are an <b>AND</b>. Empty means free.</summary>
		public List<OfferAmount> cost = new List<OfferAmount>();

		/// <summary>
		/// What the player gets. <b>Disclosure, not a fulfilment instruction</b>: nothing consumes it to grant
		/// anything. Empty means "this provider cannot enumerate its payout", never "this gives nothing".
		/// </summary>
		public List<OfferAmount> rewards = new List<OfferAmount>();

		/// <summary>Already formatted for display. Never the only representation of a price.</summary>
		public string priceLabel;

		/// <summary>
		/// Whether the provider could sell this when it resolved the offer. What a player sees is asked again
		/// on every read (<see cref="IFederatedVirtualOffer{T}.GetOffers"/>), with the campaign's gate folded in
		/// by the gateway.
		/// </summary>
		public bool available = true;

		public List<OfferReason> unavailableReasons = new List<OfferReason>();

		/// <summary>
		/// Every language this offer has text for, keyed by language code. <see cref="title"/> and
		/// <see cref="description"/> hold the resolved one.
		/// </summary>
		public Dictionary<string, OfferText> localizations = new Dictionary<string, OfferText>();

		public List<string> tags = new List<string>();

		/// <summary>Anything this contract does not name.</summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	/// <summary>One language's presentation for an offer.</summary>
	[Serializable]
	public class OfferText
	{
		public string title;
		public string description;
	}

	/// <summary>Why something is not available, machine-readable and human-readable.</summary>
	/// <remarks>
	/// <see cref="code"/> lets a client branch or re-localize; <see cref="message"/> is what to show when it
	/// does neither. Never put a value the client could not otherwise read into <see cref="properties"/>.
	/// </remarks>
	[Serializable]
	public class OfferReason
	{
		/// <summary>One of <see cref="OfferContract"/>'s reason codes, or the provider's own.</summary>
		public string code;

		public string message;

		public string detail;

		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	// ─── Provider settings ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// A provider's federation id that also declares <see cref="OfferProviderSettings"/>. The microservice
	/// sends them with its federation registration, deployed or running locally, and the backend reads them
	/// from there. A provider whose id is a plain <see cref="IFederationId"/> gets the defaults.
	/// </summary>
	public interface IOfferProviderId : IFederationId
	{
		OfferProviderSettings Settings { get; }
	}

	/// <summary>
	/// What a provider declares about itself. Every field defaults to what holds for a provider that
	/// declares nothing.
	/// </summary>
	[Serializable]
	public class OfferProviderSettings
	{
		/// <summary>
		/// Whether the provider has rules of its own on whether a grant can be redeemed now (stock, schedule,
		/// purchase limits). False when every grant is always available: the backend then answers
		/// availability itself and never calls <c>GetOffers</c>, which saves the player's offer list a call.
		/// </summary>
		public bool hasAvailabilityRules = true;

		/// <summary>
		/// The most <c>RevokeOffer</c> calls per second the backend makes to this provider, across every realm
		/// of the customer. 0 = no limit of its own: the platform's ceiling applies.
		/// </summary>
		public double revokeCallsPerSecond;

		/// <summary>The most <c>RevokeOffer</c> calls in flight at once. 0 = the platform's ceiling.</summary>
		public int revokeConcurrency;

		/// <summary>
		/// The most grants of one player revoked per <see cref="revokePlayerWindowSeconds"/>, for a service that
		/// limits calls per player: counted per grant, not per <c>RevokeOffer</c> call, because a call carries
		/// many players and you undo each grant on its own. 0 = no per-player limit.
		/// </summary>
		public int revokesPerPlayer;

		/// <summary>The window <see cref="revokesPerPlayer"/> counts over. 0 = one minute.</summary>
		public int revokePlayerWindowSeconds;

		/// <summary>
		/// How long before a grant's expiry it is ended and revoked, so whatever it created in your service is
		/// gone before the grant would have stopped being redeemable in Beamable. 0 = at the expiry. Capped by the
		/// platform.
		/// </summary>
		public int revokeLeadSeconds;

		/// <summary>
		/// For how long after a redeem's delivery first started, without a definite answer (an
		/// <see cref="OfferRedeemOutcome.Ambiguous"/> or a failed call), Beamable may ask you for the delivery
		/// again. Past it Beamable settles the redeem as <see cref="ambiguousOutcome"/> says. Bound it by how long
		/// your own service deduplicates a repeat. Null takes the platform's default (12 h), and anything shorter
		/// than the platform's minimum (1 h), 0 included, is raised to it, so every unanswered delivery ends. A
		/// delivery that can never be repeated safely is made idempotent by grant id or declares
		/// <see cref="OfferAmbiguousOutcome.Hold"/>: with <see cref="OfferAmbiguousOutcome.NotDelivered"/> one
		/// that went through without an answer is given back and can be delivered twice.
		/// </summary>
		public long? ambiguousRedeliverySeconds;

		/// <summary>
		/// What Beamable takes a delivery to have been once <see cref="ambiguousRedeliverySeconds"/> passed without
		/// an answer. A realm overrides it for your provider with the realm config
		/// <c>offers|{federationId}.ambiguousOutcome</c>.
		/// </summary>
#if DB_MICROSERVICE
		[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
#endif
		public OfferAmbiguousOutcome ambiguousOutcome = OfferAmbiguousOutcome.NotDelivered;
	}

	// ─── Availability ──────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Whether a player can act on a grant right now by the provider's own rules. The campaign's gate is not
	/// answered here: the gateway folds it in.
	/// </summary>
	[Serializable]
	public class OfferAvailability
	{
		public string grantId;

		public bool available = true;

		public List<OfferReason> unavailableReasons = new List<OfferReason>();
	}

	// ─── Grant / revoke / redeem ───────────────────────────────────────────────────────────────────

	/// <summary>One entry of a <see cref="IFederatedVirtualOffer{T}.GrantOffers"/> batch.</summary>
	[Serializable]
	public class OfferGrantItem
	{
		/// <summary>
		/// The registry's id for this grant, and your idempotency key: a retried grant carries the same id and
		/// must be answered as the grant it already is.
		/// </summary>
		public string grantId;

		public string playerId;

		public string offerId;

		/// <summary>
		/// When the grant stops being redeemable. A provider whose offer lives in a third-party service can
		/// hand this on, so the offer there ends when the grant does.
		/// </summary>
		public long expiresAtUnixSeconds;

		/// <summary>
		/// The caller's opaque data for this grant: what it is attributed to (a campaign adds
		/// <see cref="OfferContract.MetadataCampaign"/> and its siblings) and the offer's own authored fields.
		/// For a provider that mints an offer per hand-out rather than serving a catalog, this is the offer.
		/// </summary>
		public Dictionary<string, string> metadata = new Dictionary<string, string>();
	}

	[Serializable]
	public class OfferGrantResponse
	{
		/// <summary>Echo of <see cref="OfferGrantItem.grantId"/>. Answers are matched by position.</summary>
		public string grantId;

		public bool success;

		/// <summary>An <see cref="OfferContract"/> status on failure, or the provider's own string.</summary>
		public string status;

		public string message;

		/// <summary>
		/// Whether the caller should try again. A campaign holds its send and retries when true, and sends the
		/// message without the offer when false — so marking a permanent failure retriable stalls the
		/// outreach instead of degrading it.
		/// </summary>
		public bool retriable;

		/// <summary>
		/// The offer as resolved for this grant, shown to the player. Without it the grant still stands and the
		/// player is shown only the offer id.
		/// </summary>
		public OfferItem offer;

		/// <summary>
		/// Whatever you need back on redeem and revoke (an id in your own service, say). Stored with the grant
		/// and handed back verbatim on <see cref="OfferGrantRef.providerMetadata"/>; never read by Beamable.
		/// </summary>
		public Dictionary<string, string> providerMetadata = new Dictionary<string, string>();
	}

	/// <summary>
	/// A grant as the registry knows it, handed back on redeem, on revoke and when its availability is asked.
	/// </summary>
	[Serializable]
	public class OfferGrantRef
	{
		public string grantId;

		public string playerId;

		public string offerId;

		/// <summary>
		/// The offer as you resolved it at grant time. Null only on a revoke of a grant whose grant answer was
		/// lost.
		/// </summary>
		public OfferItem offer;

		/// <summary>The caller's data for the grant, as on <see cref="OfferGrantItem.metadata"/>.</summary>
		public Dictionary<string, string> metadata = new Dictionary<string, string>();

		/// <summary>What you answered on <see cref="OfferGrantResponse.providerMetadata"/>; empty when it was lost.</summary>
		public Dictionary<string, string> providerMetadata = new Dictionary<string, string>();
	}

	[Serializable]
	public class OfferRevokeResponse
	{
		public string grantId;

		/// <summary>True when nothing is left to undo, including when there never was anything.</summary>
		public bool success;

		public string status;

		public string message;

		public bool retriable;

		/// <summary>How long to be left alone before the next attempt. 0 = no ask.</summary>
		public long retryAfterMs;
	}

	/// <summary>What an unanswered delivery is taken to have been once it can no longer be asked for again.</summary>
	/// <remarks>Travels by name, so a member inserted anywhere does not reinterpret another.</remarks>
	public enum OfferAmbiguousOutcome
	{
		/// <summary>Nothing moved: the grant is given back and can be redeemed again. If it did move, the player may get it twice.</summary>
		NotDelivered,

		/// <summary>It moved: the grant is redeemed and its group forfeited. If it did not, the player keeps nothing.</summary>
		Delivered,

		/// <summary>Nobody can tell: the grant stays in progress until someone who checked your service resolves it.</summary>
		Hold,
	}

	/// <summary>What a provider says about a delivery it was asked to make.</summary>
	/// <remarks>Travels by name, so a member inserted anywhere does not reinterpret another.</remarks>
	public enum OfferRedeemOutcome
	{
		/// <summary>Delivered. The grant is redeemed.</summary>
		Settled,

		/// <summary>Not delivered, and nothing moved: the player keeps the grant.</summary>
		Refused,

		/// <summary>
		/// You cannot tell whether you delivered. The redeem stays in progress; a retry asks again with the
		/// same grant id and you answer it as the same delivery.
		/// </summary>
		Ambiguous
	}

	[Serializable]
	public class OfferRedeemResponse
	{
		public string grantId;

#if DB_MICROSERVICE
		[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
#endif
		public OfferRedeemOutcome outcome;

		/// <summary>An <see cref="OfferContract"/> status, or the provider's own string.</summary>
		public string status;

		/// <summary>Why it was refused or is unresolved, fit to show a player.</summary>
		public string message;
	}

	// ─── Shared vocabulary ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Shared wire vocabulary for the virtual-offer federation, so every provider, the Beamable backend and the
	/// Portal agree on the exact strings.
	/// </summary>
	public static class OfferContract
	{
		/// <summary>
		/// The version of this wire contract. Distinct from the Portal's <c>offerContractVersion</c>, which
		/// versions the protocol between the Campaigns extension and a provider's authoring UI.
		/// </summary>
		public const int Version = 2;

		/// <summary>
		/// The per-recipient data key (<c>MessageRailRecipient.data</c>) carrying every grant id made for a
		/// campaign send, comma-separated, so a message can deep-link the player to what they were given.
		/// Reserved by the campaign — a rail must not emit it.
		/// </summary>
		public const string GrantsKey = "beam_offer_grants";

		/// <summary>The conventional prefix for a provider's own authored keys in a grant's metadata.</summary>
		public const string KeyPrefix = "offer_";

		// --- Metadata a campaign adds to every grant -------------------------
		//
		// Reserved: an authored field of the same name is overwritten.

		public const string MetadataCampaign = "beam_campaign";
		public const string MetadataCampaignVersion = "beam_campaign_version";
		public const string MetadataNode = "beam_node";
		public const string MetadataOutreach = "beam_outreach";

		// --- Failure statuses ------------------------------------------------

		/// <summary>The offer exists but cannot be granted or delivered right now (sold out, expired).</summary>
		public const string UnavailableStatus = "unavailable";

		/// <summary>The provider is rate-limiting or shedding load. Retriable.</summary>
		public const string OverCapacityStatus = "over-capacity";

		/// <summary>No such grant, or it does not belong to this player.</summary>
		public const string UnknownGrantStatus = "unknown-grant";

		/// <summary>A delivery that is still unresolved. Retry later with the same grant id.</summary>
		public const string PendingStatus = "pending";

		/// <summary>The grant was redeemed. A repeated redeem answers this again.</summary>
		public const string RedeemedStatus = "redeemed";

		// --- Unavailable reason codes ----------------------------------------

		public const string ReasonStatRequirement = "stat-requirement";
		public const string ReasonAlreadyPurchased = "already-purchased";
		public const string ReasonPurchaseLimit = "purchase-limit";
		public const string ReasonForfeited = "forfeited";
		public const string ReasonExpired = "expired";
		public const string ReasonNotActive = "not-active";

		/// <summary>
		/// The campaign gate is not satisfied yet. The player keeps the grant and it unlocks when they
		/// qualify, so a client should say "not yet" rather than "not for you".
		/// </summary>
		public const string ReasonConditionUnmet = "condition-unmet";

		/// <summary>The player cannot afford the cost.</summary>
		public const string ReasonInsufficientFunds = "insufficient-funds";

		// --- Well-known reason properties ------------------------------------

		/// <summary>Which attribute a gate is about, qualified as <c>namespace/key</c>.</summary>
		public const string ReasonAttributeKey = "attribute";

		/// <summary>The value the gate requires.</summary>
		public const string ReasonTargetKey = "target";

		/// <summary>The player's value now. Omitted whenever it is not safe to expose.</summary>
		public const string ReasonCurrentKey = "current";

		/// <summary>Operator-authored copy to show a player while the gate is closed.</summary>
		public const string ReasonLockedMessageKey = "lockedMessage";

		// --- Amount types (OfferAmount.type) ---------------------------------
		//
		// A provider is NOT limited to these: the field is open, so a client must render an unknown type
		// rather than reject it.

		/// <summary>A soft-currency amount. <c>symbol</c> is the currency content id.</summary>
		public const string AmountCurrency = "currency";

		/// <summary>An inventory item. <c>symbol</c> is the item content id.</summary>
		public const string AmountItem = "item";

		/// <summary>A granted right — DLC, a coupon, tier membership. <c>symbol</c> is the entitlement symbol.</summary>
		public const string AmountEntitlement = "entitlement";

		/// <summary>One roll against a loot table. <c>amount</c> is the number of rolls.</summary>
		public const string AmountLootRoll = "lootRoll";
	}
}
