using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Federation for a store's <b>virtual</b> offers — ones a player buys with soft currency. A microservice
	/// implements this to grant / revoke / redeem those offers for a player, and to report what a player
	/// currently holds.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Virtual, deliberately.</b> A real-money offer is a different federation, because it is a different
	/// problem: it needs platform product ids, a native purchase flow, receipt verification and a settlement
	/// callback from outside Beamable, none of which a soft-currency offer has any use for. Carrying both on
	/// one interface meant every virtual provider inherited fields it could not fill.
	/// </para>
	/// <para>
	/// <b>There is no catalog here.</b> Listing and describing offers is not a federation concern — the only
	/// consumer of a catalog is the provider's own Portal extension, which already knows the provider
	/// intimately. A provider that wants an authoring picker ships an ordinary microservice with
	/// <c>[ServerCallable]</c> catalog methods and points its extension at it. The message rail, which this
	/// federation is modelled on, has no catalog methods either.
	/// </para>
	/// <para>
	/// This interface is the extension point, not a Beamable feature with an interface bolted on. Beamable
	/// ships <c>BeamableCampaignOfferService</c> (federation id <c>beamable_virtual_store</c>) as the default
	/// implementation over its own commerce, but a game with its own virtual economy — currency and offers in
	/// its own microstorage, no Beamable store at all — implements this same interface under its own
	/// <see cref="FederationIdAttribute"/> and is treated identically by the gateway, the campaign runtime,
	/// and the Portal. Nothing outside a given implementation may branch on which federation id it is
	/// talking to.
	/// </para>
	/// <para>
	/// The DTOs below are deliberately generic for that reason. <see cref="CampaignOfferItem.properties"/> and
	/// <see cref="CampaignOfferGrantContext.extraDataFed"/> are the escape hatches for anything this contract
	/// does not name, so a provider does not need a contract version bump to carry its own data.
	/// </para>
	/// <para>
	/// A federation id must be <c>[A-Za-z][A-Za-z0-9_]*</c> — the source generator rejects anything else
	/// (BEAM_FED_0004), because the id becomes a route segment and a generated client member.
	/// </para>
	/// </remarks>
	public interface IFederatedCampaignVirtualOffer<in T> : IFederation where T : IFederationId, new()
	{
		/// <summary>
		/// Entitle a player to an offer, returning the grant that represents it. Called by the campaign
		/// runtime as a send goes out, so the resulting <see cref="CampaignOfferGrantResponse.grantId"/> can
		/// ride the message the player receives.
		///
		/// <para>
		/// <b>Grant unconditionally.</b> A campaign condition is no longer a filter that decides whether to
		/// grant — it is a gate re-evaluated on read (see <see cref="CampaignOfferGrantContext.conditionToken"/>).
		/// A player who does not yet qualify still receives the grant, and the offer unlocks when they do.
		/// </para>
		///
		/// <para>
		/// Must be safe to call again for the same <see cref="CampaignOfferGrantContext.outreachId"/>: the
		/// send is retried on any retriable failure downstream, and a store that double-grants would pay out
		/// twice for one outreach. Return the existing grant rather than a second one.
		/// </para>
		/// </summary>
		Promise<CampaignOfferGrantResponse> GrantOffer(string playerId, string offerId, CampaignOfferGrantContext context);

		/// <summary>
		/// Withdraw grants that have not been redeemed. Called when a campaign reaches a terminal state —
		/// an operator hard-stopped it, or it expired — so its outstanding offers stop being claimable.
		///
		/// <para>
		/// <b>A list, because the caller has one.</b> The halt's force-exit walks accounts a page at a time,
		/// so revokes arrive in batches. Unlike <see cref="GrantOffer"/>, whose caller is per-account by
		/// construction, this one genuinely has many players in hand at once.
		/// </para>
		///
		/// <para>
		/// Idempotent: a repeat must report success rather than failing, because a halt pass can be
		/// redelivered. A grant that was already redeemed must be <b>refused, not clawed back</b> — the
		/// player already has the goods.
		/// </para>
		/// </summary>
		Promise<List<CampaignOfferGrantResponse>> RevokeOffer(List<CampaignOfferRevokeRequest> revokes);

		/// <summary>
		/// Consume a grant — the player claiming what they were offered. Client-callable through the
		/// gateway, so implementations must treat <paramref name="playerId"/> as already authorized by the
		/// caller and must be idempotent on <see cref="CampaignOfferRedeemRequest.transactionId"/>.
		///
		/// <para>
		/// The gateway refuses a redeem whose campaign condition is unmet before dispatching here, so an
		/// implementation only has to enforce its <em>own</em> store rules.
		/// </para>
		/// </summary>
		Promise<CampaignOfferRedeemResponse> RedeemOffer(string playerId, string grantId, CampaignOfferRedeemRequest request);

		/// <summary>
		/// Every offer this store is currently holding for a player, filtered by state. The read side of the
		/// grant lifecycle — what the player can still claim, and what they already did.
		///
		/// <para>
		/// Report <see cref="CampaignOffer.available"/> against your <em>own</em> store rules only. Campaign
		/// conditions are evaluated and overlaid by the gateway; an implementation never sees them.
		/// </para>
		/// </summary>
		Promise<CampaignOffersResponse> GetCampaignOffers(string playerId, CampaignOfferFilter filter);
	}

	// ─── The shared primitive ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// N of something. Serves <b>both</b> sides of a trade: what an offer costs, and what it gives.
	/// </summary>
	/// <remarks>
	/// <para>
	/// One type rather than two because a cost is a reward in the other direction, and a client renders
	/// them identically — icon, quantity, name. Splitting them produced a contract where
	/// <see cref="CampaignOfferItem.rewards"/> lived on the offer while its price lived two levels down
	/// inside a storefront-listing wrapper, which a provider without a storefront could not fill at all.
	/// </para>
	/// <para>
	/// <b>Never switch exhaustively on <see cref="type"/>.</b> A client renders the types it knows and falls
	/// back to <see cref="title"/> (then <see cref="symbol"/>) for the rest — a provider is free to invent a
	/// type this contract predates, and a client that treats an unknown type as an error breaks the
	/// extension point.
	/// </para>
	/// </remarks>
	[Serializable]
	public class CampaignOfferAmount
	{
		/// <summary>
		/// What kind of thing this is. Beamable's own provider emits
		/// <see cref="CampaignOfferContract.AmountCurrency"/>, <see cref="CampaignOfferContract.AmountItem"/>,
		/// <see cref="CampaignOfferContract.AmountEntitlement"/> and
		/// <see cref="CampaignOfferContract.AmountLootRoll"/>; a third-party store may emit its own.
		///
		/// <para>
		/// On a <see cref="CampaignOfferItem.cost"/> entry this is what lets an offer be priced in something
		/// other than currency — three of an item, for a barter economy — which the contract could not
		/// express while price was its own type.
		/// </para>
		/// </summary>
		public string type;

		/// <summary>
		/// The store's own reference for the thing — a currency id, a content id, an entitlement symbol.
		/// Opaque: only the store that issued it can interpret it.
		/// </summary>
		public string symbol;

		/// <summary>
		/// How many. <b>Always a real quantity.</b>
		///
		/// <para>
		/// There is no "unknown" sentinel: a loot roll is <c>amount: 1</c> of a roll, not <c>0</c> of a
		/// prize. What varies is what the roll <em>yields</em>, which is not knowable and so is correctly
		/// absent from this contract. A store that cannot enumerate its payout at all leaves
		/// <see cref="CampaignOfferItem.rewards"/> empty.
		/// </para>
		///
		/// <para>On a cost entry, <c>0</c> means free, and is not a way to say "varies".</para>
		/// </summary>
		public long amount;

		/// <summary>Display name, when the store has one. Falls back to <see cref="symbol"/>.</summary>
		public string title;

		/// <summary>Icon or art, when the store has one.</summary>
		public string imageUrl;

		/// <summary>
		/// Anything this contract does not name — item properties, an entitlement specialization, a rarity,
		/// a duration. The escape hatch that keeps <see cref="type"/> from having to grow a field per kind.
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	// ─── The offer ─────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// The description of a trade: pay <see cref="cost"/>, receive <see cref="rewards"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="offerId"/> is the only required field. Everything else is optional, which is what lets a
	/// provider with no storefront, no catalog and no Beamable commerce describe an offer without inventing
	/// concepts it does not have.
	/// </remarks>
	[Serializable]
	public class CampaignOfferItem
	{
		/// <summary>
		/// The store's own reference for this offer, and the value written to a campaign send node's
		/// <c>Offer</c>. Opaque to everything outside the store that issued it — do not parse it, show it as
		/// a name, or key anything durable on its shape.
		///
		/// <para>
		/// A store <em>may</em> encode its own routing into it (Beamable's default provider mints
		/// <c>"{store}/{listing}"</c> and parses it back), which is why this contract carries no separate
		/// storefront fields: the information a provider needs to fulfil is already in the id it minted.
		/// </para>
		/// </summary>
		public string offerId;

		public string title;
		public string description;
		public string imageUrl;

		/// <summary>
		/// What the player pays. Several entries are an <b>AND</b> — all of them together.
		///
		/// <para>
		/// Alternative pricing ("100 gold OR 5 gems") is deliberately not expressible: it would nest the
		/// type for a case a campaign grants with one price anyway. A store that needs it publishes two
		/// offers, or reads <c>CampaignOfferRedeemRequest.params</c> at redeem to learn which the
		/// player chose.
		/// </para>
		///
		/// <para>Empty means free — and is how "this grant has no cost" is expressed, rather than by the
		/// absence of some other collection.</para>
		/// </summary>
		public List<CampaignOfferAmount> cost = new List<CampaignOfferAmount>();

		/// <summary>
		/// What the player gets — the bundle's contents, itemised.
		///
		/// <para>
		/// <b>Disclosure, not a fulfilment instruction.</b> The store still fulfils however it fulfils; this
		/// exists so a surface can tell the player what they are about to buy. Nothing consumes it to grant
		/// anything, and a client must never reconcile it against what actually landed — a loot roll, a VIP
		/// multiplier or a store-side promotion can legitimately make the two differ.
		/// </para>
		///
		/// <para>
		/// Empty is legitimate and must render: a provider that cannot enumerate its payout (an opaque
		/// third-party bundle) leaves it empty and a client falls back to <see cref="description"/>. Do not
		/// treat empty as "this offer gives nothing".
		/// </para>
		/// </summary>
		public List<CampaignOfferAmount> rewards = new List<CampaignOfferAmount>();

		/// <summary>
		/// Already formatted for display ("1200 Gems"), for surfaces that only ever print it.
		///
		/// <para>
		/// <b>Never the only representation of a price.</b> It is neither localizable nor comparable, so a
		/// client deciding whether the player can afford this needs <see cref="cost"/> instead.
		/// </para>
		/// </summary>
		public string priceLabel;

		/// <summary>
		/// Whether this offer can be granted or bought right now, as far as the <em>store</em> is concerned —
		/// sold out, outside its schedule, a listing requirement unmet.
		///
		/// <para>
		/// <b>Campaign conditions are not answered here.</b> Those are evaluated by the gateway and reported
		/// on <see cref="CampaignOffer.available"/>. When both say something, the grant-level answer wins;
		/// see that field.
		/// </para>
		/// </summary>
		public bool available = true;

		/// <summary>Why <see cref="available"/> is false. Empty when it is true.</summary>
		public List<CampaignOfferReason> unavailableReasons = new List<CampaignOfferReason>();

		/// <summary>
		/// Every language this offer has text for, keyed by language code. <see cref="title"/> and
		/// <see cref="description"/> hold the resolved one, so a caller that does not care about
		/// localization can ignore this entirely.
		///
		/// <para>
		/// Carried in full rather than collapsed because a client that switches language at runtime cannot
		/// get the other translations back without a second round trip.
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
	/// Why something is not available, in a machine-readable and a human-readable form.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="code"/> lets a client branch or re-localize; <see cref="message"/> is what to show when it
	/// does neither. A client that can only render a disabled row with no explanation produces a support
	/// ticket, which is what this exists to avoid.
	/// </para>
	/// <para>
	/// <b>For a campaign condition, prefer <see cref="properties"/> over <see cref="message"/>.</b> A
	/// condition is authored by an operator, so its prose is operator-facing and is usually not fit to show
	/// a player. Structured facts (the attribute, the target, and the current value <em>when it is safe to
	/// expose</em>) let the client write its own copy, and let it decide whether to show the offer locked or
	/// hide it entirely.
	/// </para>
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
		/// The structured form of what <see cref="message"/> says in prose — the requirement's id, the
		/// threshold, the reset time — so a client can build its own copy without parsing English.
		///
		/// <para>
		/// <b>Never put a private value here.</b> A player's own client-visible stat is fine; a value from a
		/// namespace the client could not otherwise read is not, and only the fact that the gate is unmet
		/// may travel.
		/// </para>
		/// </summary>
		public Dictionary<string, string> properties = new Dictionary<string, string>();
	}

	// ─── The grant ─────────────────────────────────────────────────────────────────────────────────

	/// <summary>The lifecycle of a grant. Closed: the platform owns this state machine, not a provider.</summary>
	/// <remarks>
	/// Serialized <b>by name</b>, never by ordinal — a provider reads and writes these as the strings in
	/// <see cref="CampaignOfferContract"/>, and inserting a member anywhere but the end must not reinterpret
	/// stored or in-flight data.
	/// </remarks>
	public enum CampaignOfferState
	{
		/// <summary>Granted and still claimable.</summary>
		Granted,

		/// <summary>Claimed by the player.</summary>
		Redeemed,

		/// <summary>Withdrawn before it was claimed.</summary>
		Revoked,

		/// <summary>Passed its expiry unclaimed.</summary>
		Expired
	}

	/// <summary>
	/// A ledger row: this store owes this player the chance to take this offer, until this time.
	/// </summary>
	/// <remarks>
	/// It is not the goods, and claiming it is not delivery. Two consequences catch people: expiry is folded
	/// in <b>on read</b> rather than swept, so an entitlement list must never be cached across a session; and
	/// <see cref="state"/> is the source of truth, not the presence of a row — redeemed and revoked rows stay
	/// in the list forever.
	/// </remarks>
	[Serializable]
	public class CampaignOffer
	{
		public string grantId;
		public string offerId;

		public CampaignOfferState state;

		public long grantedAtUnixSeconds;

		/// <summary>0 when the grant does not expire.</summary>
		public long expiresAtUnixSeconds;

		/// <summary>
		/// The offer this grant is for, in full.
		///
		/// <para>
		/// Carried here so that one call is enough to render a store UI — without it every client fans out a
		/// lookup per row. A provider that finds this expensive may leave it null and let the client fall
		/// back to <see cref="offerId"/>; the gateway also leaves it null when a catalog is unreachable,
		/// which is why a client must always write that fallback.
		/// </para>
		/// </summary>
		public CampaignOfferItem offer;

		/// <summary>
		/// Whether the player can act on this <b>right now</b> — the store's rules and the campaign's gate
		/// combined.
		///
		/// <para>
		/// Distinct from <see cref="state"/>: a grant can be <see cref="CampaignOfferState.Granted"/> and
		/// still unavailable because a campaign condition is unmet or a store requirement is not satisfied.
		/// A provider fills this from its own rules; the gateway then ANDs the campaign's condition into it
		/// and appends the corresponding reason.
		/// </para>
		/// </summary>
		public bool available;

		/// <summary>
		/// Why <see cref="available"/> is false. Empty when it is true, and may carry more than one reason
		/// when the store and the campaign both have something to say.
		/// </summary>
		public List<CampaignOfferReason> unavailableReasons = new List<CampaignOfferReason>();

		/// <summary>
		/// The campaign gate this grant was made under, echoed back verbatim from
		/// <see cref="CampaignOfferGrantContext.conditionToken"/>. Empty for an ungated grant.
		///
		/// <para>
		/// <b>Store it opaquely and hand it back; never parse it.</b> The gateway is what evaluates a
		/// campaign condition, and it needs the gate at read time while being stateless for offers itself.
		/// The store is the only thing that persists per-grant, so the token rides along the way a cookie
		/// does. The gateway strips it before a client ever sees it.
		/// </para>
		/// </summary>
		public string conditionToken;
	}

	/// <summary>What to return from <see cref="IFederatedCampaignVirtualOffer{T}.GetCampaignOffers"/>.</summary>
	/// <remarks>
	/// A list of states rather than a boolean, because "unredeemed" is ambiguous: it literally includes
	/// revoked and expired, while a store screen wants only <see cref="CampaignOfferState.Granted"/>. A flag
	/// would make the provider guess which the caller meant.
	/// </remarks>
	[Serializable]
	public class CampaignOfferFilter
	{
		/// <summary>
		/// Which states to return. <b>Empty means all.</b> Honouring this is required, not advisory — a
		/// filter a caller cannot rely on is a filter no caller can use.
		///
		/// <para>
		/// Applies to the <em>effective</em> state: expiry is folded in on read, so a stored-as-granted row
		/// that is past its expiry answers to <see cref="CampaignOfferState.Expired"/>.
		/// </para>
		/// </summary>
		public List<CampaignOfferState> states = new List<CampaignOfferState>();
	}

	[Serializable]
	public class CampaignOffersResponse
	{
		public string playerId;

		public List<CampaignOffer> offers = new List<CampaignOffer>();

		/// <summary>
		/// The contract version this response was produced against —
		/// <see cref="CampaignOfferContract.Version"/>.
		///
		/// <para>
		/// Present so skew is detectable rather than discovered as a parse bug: this contract has been
		/// reshaped more than once, and a provider compiled against an older shape otherwise fails in ways
		/// that look like data corruption.
		/// </para>
		/// </summary>
		public int contractVersion = CampaignOfferContract.Version;
	}

	// ─── Grant / revoke / redeem ───────────────────────────────────────────────────────────────────

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
		/// itself</b>, and <see cref="IFederatedCampaignVirtualOffer{T}.GrantOffer"/> has nothing to grant
		/// without it.
		/// </para>
		/// </summary>
		public Dictionary<string, string> extraDataFed = new Dictionary<string, string>();

		/// <summary>When the grant should stop being claimable. 0 = the store's own default.</summary>
		public long expiresAtUnixSeconds;

		/// <summary>
		/// The campaign offer group this grant belongs to, or empty. Bookkeeping the store may record — it is
		/// not what decides anything; see <see cref="invalidatesOfferIds"/>.
		/// </summary>
		public string groupId;

		/// <summary>
		/// The offer ids this grant forfeits when it is purchased, already resolved by the campaign runtime.
		/// Empty means "forfeits nothing".
		///
		/// <para>
		/// <b>A store's whole obligation here is one sentence: on purchase, revoke exactly these.</b> The
		/// campaign's grouping vocabulary — whether the offers stack or are alternatives, whether taking one
		/// forfeits a named sibling or the entire group — is resolved campaign-side and never reaches this
		/// contract. A group only ever spans one federation, so every id here belongs to the store being
		/// asked.
		/// </para>
		/// </summary>
		public List<string> invalidatesOfferIds = new List<string>();

		/// <summary>
		/// The campaign's gate on this grant, as an opaque blob, or empty when the offer is ungated.
		///
		/// <para>
		/// <b>Store it verbatim and hand it back; never parse it.</b> A campaign condition is re-evaluated on
		/// every read, so whoever evaluates it needs it at read time — and the campaign is not in that path.
		/// The store is the only thing that persists per-grant, so it carries the blob the way a cookie is
		/// carried. The gateway strips it before a client ever sees it.
		/// </para>
		///
		/// <para>
		/// Snapshotted at grant, deliberately: editing a campaign afterwards must not retroactively change
		/// the gate a player was granted under, for the same reason the offer itself is stored rather than
		/// re-resolved.
		/// </para>
		/// </summary>
		public string conditionToken;
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

	/// <summary>One entry of a <see cref="IFederatedCampaignVirtualOffer{T}.RevokeOffer"/> batch.</summary>
	[Serializable]
	public class CampaignOfferRevokeRequest
	{
		public string playerId;
		public string grantId;
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

	// ─── Shared vocabulary ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Shared wire vocabulary for the campaign-offer federation contract, so every implementation, the
	/// Beamable backend, and the Portal agree on the exact strings. Mirrors <see cref="MessageRailContract"/>'s
	/// role for the message rail.
	/// </summary>
	public static class CampaignOfferContract
	{
		/// <summary>
		/// The version of this wire contract, echoed on <see cref="CampaignOffersResponse.contractVersion"/>.
		///
		/// <para>
		/// Distinct from the Portal's <c>offerContractVersion</c>, which versions the parent/child protocol
		/// between the Campaigns extension and a provider's authoring UI. The two are unrelated and do not
		/// move together.
		/// </para>
		/// </summary>
		public const int Version = 1;

		/// <summary>
		/// The campaign payload key carrying the authored offer reference. Reserved by the campaign — mirrors
		/// <c>CampaignSendPayload.ReservedKeys</c> in the Beamable backend.
		/// </summary>
		public const string OfferKey = "offer";

		/// <summary>
		/// The campaign payload key carrying the <see cref="CampaignOfferGrantResponse.grantId"/> of the first
		/// grant made for this send, so the message rail can deep-link the player straight to what they were
		/// given. Also reserved — a rail must not emit it.
		/// </summary>
		public const string GrantKey = "beam_offer_grant";

		/// <summary>Every grant id for this send, comma-separated. Also reserved.</summary>
		public const string GrantsKey = "beam_offer_grants";

		/// <summary>
		/// The namespace every offer provider's authored fields sit in inside a campaign send's payload.
		///
		/// <para>
		/// Load-bearing, not cosmetic: a lane's message rail and its offer provider spread their authored
		/// data into the <b>same</b> <c>customProperties</c> map, and nothing else in a stored graph tells the
		/// two apart. This prefix is what routes each half back to the extension that wrote it when a
		/// campaign is reopened, and what lets the campaign runtime hand a store its own fields — and only
		/// its own — in <see cref="CampaignOfferGrantContext.extraDataFed"/>.
		/// </para>
		/// </summary>
		public const string KeyPrefix = "offer_";

		// --- Grant / revoke / redeem failure statuses -----------------------

		/// <summary>The offer exists but cannot be granted right now (sold out, region-locked, expired).</summary>
		public const string UnavailableStatus = "unavailable";

		/// <summary>
		/// This outreach was already granted. Not an error — the expected answer to a retried grant, and
		/// implementations should return the original <see cref="CampaignOfferGrantResponse.grantId"/>
		/// alongside it.
		/// </summary>
		public const string AlreadyGrantedStatus = "already-granted";

		/// <summary>The store is rate-limiting or shedding load. Retriable.</summary>
		public const string OverCapacityStatus = "over-capacity";

		/// <summary>No such grant, or it does not belong to this player.</summary>
		public const string UnknownGrantStatus = "unknown-grant";

		// --- Entitlement states, as they appear on the wire -----------------
		//
		// CampaignOfferState serializes by name. These constants exist so a provider written against raw
		// JSON, or a non-C# implementation, has the exact strings.

		public const string StateGranted = "Granted";
		public const string StateRedeemed = "Redeemed";
		public const string StateRevoked = "Revoked";
		public const string StateExpired = "Expired";

		// --- Unavailable reason codes ---------------------------------------
		//
		// The codes a client can branch on or re-localize. A store may emit its own instead; a client that
		// does not recognise a code falls back to CampaignOfferReason.message, which is why the message is
		// never optional.

		/// <summary>A player stat requirement is unmet.</summary>
		public const string ReasonStatRequirement = "stat-requirement";

		/// <summary>Already bought, and the offer does not allow buying it again.</summary>
		public const string ReasonAlreadyPurchased = "already-purchased";

		/// <summary>A purchase limit has been reached.</summary>
		public const string ReasonPurchaseLimit = "purchase-limit";

		/// <summary>Forfeited by purchasing an offer this one was an alternative to.</summary>
		public const string ReasonForfeited = "forfeited";

		/// <summary>Past its expiry.</summary>
		public const string ReasonExpired = "expired";

		/// <summary>Outside the offer's active period or schedule.</summary>
		public const string ReasonNotActive = "not-active";

		/// <summary>
		/// The campaign gate on this grant is not satisfied yet. The player keeps the grant and it unlocks
		/// when they qualify, so a client should say "not yet" rather than "not for you".
		/// </summary>
		public const string ReasonConditionUnmet = "condition-unmet";

		/// <summary>The player cannot afford the cost. A store with its own economy reports this itself.</summary>
		public const string ReasonInsufficientFunds = "insufficient-funds";

		// --- Well-known reason properties -----------------------------------
		//
		// So a client can render progress ("Level 7 / 10") and decide whether to show an offer locked or
		// hide it, instead of only being able to print an operator-authored sentence.

		/// <summary>Which attribute the gate is about, qualified as <c>namespace/key</c>.</summary>
		public const string ReasonAttributeKey = "attribute";

		/// <summary>The value the gate requires.</summary>
		public const string ReasonTargetKey = "target";

		/// <summary>
		/// The player's value now. <b>Omitted whenever it is not safe to expose</b> — see
		/// <see cref="CampaignOfferReason.properties"/>.
		/// </summary>
		public const string ReasonCurrentKey = "current";

		/// <summary>Operator-authored copy to show a player while the gate is closed.</summary>
		public const string ReasonLockedMessageKey = "lockedMessage";

		// ── Amount types (CampaignOfferAmount.type) ─────────────────────────
		//
		// The four kinds Beamable's own commerce can move. A store is NOT limited to these — the field is an
		// open string precisely so a third-party provider can name its own — so a client must render an
		// unknown type rather than reject it.

		/// <summary>A soft-currency amount. <c>symbol</c> is the currency content id.</summary>
		public const string AmountCurrency = "currency";

		/// <summary>An inventory item. <c>symbol</c> is the item content id.</summary>
		public const string AmountItem = "item";

		/// <summary>
		/// A granted right — DLC, a coupon, tier membership. <c>symbol</c> is the entitlement symbol; a
		/// specialization travels in <c>properties</c>.
		/// </summary>
		public const string AmountEntitlement = "entitlement";

		/// <summary>
		/// One roll against a loot table. <c>amount</c> is the number of rolls — its <em>contents</em> are
		/// not known until fulfilment, and are simply absent rather than encoded as a zero.
		/// </summary>
		public const string AmountLootRoll = "lootRoll";
	}
}
