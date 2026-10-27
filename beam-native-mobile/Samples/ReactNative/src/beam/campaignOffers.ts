/**
 * App-specific bindings for the **virtual offer federation** (`IFederatedVirtualOffer`).
 *
 * A campaign lane can attach an offer to the message it sends: the operator authors it in the
 * Portal, Beamable grants it to each recipient as the send goes out, and the player claims it
 * here. Which provider the offer comes from is a *federation* — an extension point, not a Beamable
 * feature. Beamable ships three (see `KNOWN_FEDERATIONS`): a store whose offers are bought, a free
 * currency/item reward, and a free badge. A game with its own virtual economy implements the same
 * interface under its own id and is reached through these same two calls.
 *
 * **Virtual means soft currency.** A price here is a currency symbol and an amount, never real
 * money — a real-money offer is a separate federation, with its own contract, and it is not
 * reachable from this module. That is why there is no receipt, no product id and no native
 * purchase flow anywhere in this file.
 *
 * Two rules this module exists to keep:
 *
 *  - **Nothing branches on the federation id.** Beamable keeps every grant and knows its provider,
 *    so one list returns every provider's offers and a claim needs only the grant id; the id is a
 *    filter at most. What differs between offers is read off the OFFER (a cost or none, a reward's
 *    `type`), so a third-party provider that sends the same shapes renders the same way.
 *  - **The offer id is opaque.** Only the provider that minted it can interpret it. Never parse it,
 *    show it as a name, or key anything durable on its shape.
 *
 * Only two of the offer routes are reachable from a player token — list what I hold, and claim one
 * of them. Granting and revoking are operator or server-to-server concerns and are
 * permission-scoped away from a game client.
 *
 * **Claiming is how the player gets the goods** — the provider delivers at redeem time, as the
 * player. For the store that means the claim IS the purchase (commerce debits the price and credits
 * the bundle in one transaction); for the reward it is one inventory update; for the badge it is a
 * `game.public` stat write. The response carries none of what moved, which is why the screen reads
 * the wallet (or the stat) back rather than trusting it.
 *
 * `OfferService` is registered in `beamClient.ts`.
 */
import type { OfferFederationId } from '@beamable/sdk';
import {
  OfferGrantState,
  type OfferAmount,
  type PlayerOffer,
} from '@beamable/sdk/schema';
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

function requireBeam() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

/**
 * The providers Beamable ships, for the screen's filter — a convenience, not a whitelist: any id
 * works, which is why the filter also takes a typed one.
 *
 *  - `beamable_virtual_store` — an offer is a commerce listing; claiming BUYS it with soft currency.
 *  - `beamable_reward` — free (`cost = []`); pays out `currency.*` / `items.*` amounts.
 *  - `beamable_badge` — free; pays out one `badge` reward, delivered as a `game.public` stat.
 */
export const KNOWN_FEDERATIONS = [
  { id: 'beamable_virtual_store', label: 'Store' },
  { id: 'beamable_reward', label: 'Reward' },
  { id: 'beamable_badge', label: 'Badge' },
] as const satisfies readonly { id: OfferFederationId; label: string }[];

/**
 * The reward `type` the badge provider describes its payout with — a type of its own, which the
 * open `type` string allows. The stat key is the `symbol`; where it lands is in `properties`.
 */
export const BADGE_REWARD_TYPE = 'badge';

/**
 * The reserved per-recipient key carrying every grant id made for a send, comma-separated.
 *
 * The campaign writes it (`OfferContract.GrantsKey`) so a rail can deep-link the player
 * straight to what they were given — the offer *ref* alone is not something a player can be sent
 * to. A send node can carry several offers, hence a list. A client only ever **reads** it.
 */
export const OFFER_GRANTS_KEY = 'beam_offer_grants';

/** Splits a `beam_offer_grants` value into its grant ids, dropping blanks and duplicates. */
export function parseOfferGrantIds(raw: unknown): string[] {
  if (typeof raw !== 'string') return [];
  return [...new Set(raw.split(',').map((id) => id.trim()).filter(Boolean))];
}

/**
 * The redeem status for "the delivery is still being resolved" — the provider could not tell
 * whether it went through, or another claim of the grant is in flight. Claim again later: it is the
 * same delivery, never a second one.
 */
export const PENDING = 'pending';

/** One thing an offer gives the player. A bundle is a list of these. */
export type Reward = {
  /**
   * An OPEN string — `currency`, `item`, `entitlement`, `lootRoll`, or whatever a third-party
   * store invents. Never switch on it exhaustively; render what you know and fall back to the
   * label for the rest, or the extension point is broken.
   */
  type: string;
  /** The store's own opaque reference for the thing granted. */
  symbol: string;
  /** A real quantity. A badge is `1`; its stat value is a property, not an amount. */
  amount: number;
  /** Display name where the store has one, else the symbol's tail. */
  label: string;
  /** Empty when the store has none — the card draws a fallback. */
  imageUrl: string;
  /** The store's own extras — item properties, a rarity, a duration. */
  properties: Record<string, string>;
};

/** Why a campaign offer cannot be acted on. */
export type Reason = { code: string; message: string; detail: string };

/**
 * One campaign offer granted to this player, with the offer the store embedded in it.
 *
 * This is the app's own model, which is why it takes the good name over the wire's `PlayerOffer`:
 * everything above `offer` is the *grant* (who holds it, from which provider, in what state, until
 * when), and the provider's offer hangs off it.
 *
 * The store sends the whole offer inline so that listing a player's campaign offers is enough to
 * render a store screen — there is no second call per row. Every field below `expiresAt` may be
 * absent: the contract allows a provider to omit the offer entirely, and a third-party store
 * legitimately will, so each one has a fallback rather than a guard at the call site.
 */
export type CampaignOffer = {
  grantId: string;
  /** Which provider made it. Shown, never branched on. */
  federationId: string;
  offerId: string;
  state: string;
  grantedAt: number;
  /** 0 means it never expires. */
  expiresAt: number;
  /** Null when the store did not embed the offer; fall back to the opaque `offerId`. */
  offer: {
    title: string;
    description: string;
    imageUrl: string;
    priceLabel: string;
    /**
     * What the player pays. Several entries are an AND. Empty means free — and is how "no cost" is
     * said, rather than by the absence of some other collection.
     */
    cost: Reward[];
    /**
     * What the offer pays out. Empty means the store cannot enumerate its payout — NOT that the
     * offer gives nothing. Disclosure only: it is never reconciled against what actually landed.
     */
    rewards: Reward[];
    /** The store's own extras, rendered generically so a provider needs no client change. */
    properties: Record<string, string>;
    tags: string[];
  } | null;
  /**
   * Whether the player can act on this NOW. Distinct from `state`: a `Granted` offer can still be
   * unavailable because a store requirement or the campaign's own gate is unmet — and that gate is
   * re-evaluated on every read, so a locked row unlocks by itself once the player qualifies.
   */
  available: boolean;
  /** Non-empty whenever `available` is false. */
  reasons: Reason[];
};

/** Every state a player can see. The list's default is only what can still be used. */
const HISTORY: OfferGrantState[] = [
  OfferGrantState.Granted,
  OfferGrantState.Claiming,
  OfferGrantState.Redeemed,
  OfferGrantState.Revoked,
  OfferGrantState.Expired,
];

/** Most pages one read follows. A sample screen, not an archive: past this the oldest are left out. */
const MAX_PAGES = 5;

/**
 * Every grant the current player holds, from every provider or only `federationId`'s, in every state,
 * newest first.
 *
 * This is not a to-do list: `revoked` and `redeemed` grants are asked for too, so read `state`
 * rather than treating a row's presence as "claimable". A grant past its expiry reports `Expired` as
 * soon as it is read, before the backend records it, so the result must not be cached across a
 * session.
 */
export async function listCampaignOffers(federationId?: OfferFederationId): Promise<CampaignOffer[]> {
  const held: PlayerOffer[] = [];
  let cursor: string | undefined;
  for (let page = 0; page < MAX_PAGES; page++) {
    const { offers, next } = await requireBeam().offers.getOffers({
      states: HISTORY,
      federationId,
      cursor,
    });
    held.push(...(offers ?? []));
    if (!next) break;
    cursor = next;
  }
  return held.map(normalize);
}

/**
 * Claims a grant, returning the message to show. The provider delivers during this call. The grant id
 * is all it takes: Beamable knows which provider made it.
 *
 * For the store it IS the purchase: the provider spends on the player's behalf, and commerce
 * debits the price and credits the payout in one inventory transaction. So this is **one call,
 * not a buy followed by a settle** — buying the listing first would charge the player twice. For
 * a free offer (reward, badge) it is simply the claim.
 *
 * The endpoint answers **200 with `success: false`** for an expired, revoked, already-claimed or
 * unknown grant, or one whose campaign gate is not met yet — a resolved promise is not a success.
 * Throwing the server's own `message` is deliberate: it is the only sentence that says what to fix.
 *
 * The one exception is `pending`: the provider could not tell whether the delivery went through,
 * and the claim is kept open. That is not a refusal — pressing the button again retries the SAME
 * delivery, and it is answered once the provider knows. So it gets its own "still confirming"
 * message instead of the server's.
 */
export async function claimGrant(grantId: string): Promise<string> {
  const res = await requireBeam().offers.redeem(grantId);
  if (res.status === PENDING) {
    throw new Error('Still confirming this claim with the provider — try again in a moment.');
  }
  if (!res.success) {
    throw new Error(res.message || `The store refused this claim (${res.status ?? 'no status'})`);
  }
  // A repeat claim of a redeemed grant answers success again, so a double-tap is never a failure.
  return res.message || `Claimed ${grantId}`;
}

/** Only a `granted` offer can be claimed; every other state is terminal. */
export function isClaimable(e: CampaignOffer): boolean {
  return e.state === 'Granted';
}

/** Plain-language label for the states a player can see. */
export function describeState(state: string): string {
  switch (state) {
    case 'Granted':
      return 'Ready to claim';
    case 'Claiming':
      return 'Being delivered';
    case 'Redeemed':
      return 'Claimed';
    case 'Revoked':
      return 'Withdrawn';
    case 'Expired':
      return 'Expired unclaimed';
    default:
      // A provider may report a state this sample predates. Show it rather than hiding it.
      return state || 'unknown';
  }
}

/** `0`/absent means "never expires"; anything else is a unix-seconds instant. */
export function formatExpiry(expiresAt: number): string {
  if (!expiresAt) return 'never expires';
  return `expires ${new Date(expiresAt * 1000).toLocaleString()}`;
}

export function formatWhen(unixSeconds: number): string {
  if (!unixSeconds) return '—';
  return new Date(unixSeconds * 1000).toLocaleString();
}

/**
 * The timestamps are C# `long`s, so the SDK types them `bigint | string` and its JSON reviver can
 * hand back either — `Number(...)` here keeps that out of the UI, the same way `segments.ts` does
 * for its stat values. (Currency AMOUNTS are handled differently: see `inventory.ts`, where money
 * stays `bigint` because a balance is subtracted to produce a receipt.)
 *
 * Everything the store embeds is defaulted rather than guarded at the call site, so a provider
 * that sends a bare grant renders as an id-only row instead of blanking the screen.
 */
function normalize(e: PlayerOffer): CampaignOffer {
  return {
    grantId: e.grantId ?? '',
    federationId: e.federationId ?? '',
    offerId: e.offerId ?? '',
    // No default: a missing state is unknown, not claimable. `describeState` shows it as such.
    state: e.state ?? '',
    grantedAt: Number(e.grantedAtUnixSeconds ?? 0),
    expiresAt: Number(e.expiresAtUnixSeconds ?? 0),
    offer: e.offer
      ? {
          title: e.offer.title ?? '',
          description: e.offer.description ?? '',
          imageUrl: e.offer.imageUrl ?? '',
          priceLabel: e.offer.priceLabel ?? '',
          // Cost and rewards are the same shape at the same level — the two halves of one trade.
          cost: (e.offer.cost ?? []).map(normalizeAmount),
          rewards: (e.offer.rewards ?? []).map(normalizeAmount),
          properties: e.offer.properties ?? {},
          tags: e.offer.tags ?? [],
        }
      : null,
    available: e.available ?? false,
    reasons: (e.unavailableReasons ?? []).map((r) => ({
      code: r.code ?? '',
      message: r.message ?? '',
      detail: r.detail ?? '',
    })),
  };
}

function normalizeAmount(r: OfferAmount): Reward {
  const symbol = r.symbol ?? '';
  return {
    type: r.type ?? '',
    symbol,
    amount: Number(r.amount ?? 0),
    // The store's own title wins; the symbol's tail is the fallback, since an opaque id is a poor
    // thing to show a player who is about to pay for it.
    label: r.title || shortSymbol(symbol),
    imageUrl: r.imageUrl ?? '',
    properties: r.properties ?? {},
  };
}

/** `currency.gems` -> `gems`. Presentation only — never key anything on this. */
function shortSymbol(symbol: string): string {
  const i = symbol.lastIndexOf('.');
  return i >= 0 && i < symbol.length - 1 ? symbol.slice(i + 1) : symbol;
}

/**
 * A free offer: embedded, with nothing to pay. The contract says "free" with an empty `cost`, so
 * this is the Claim-vs-Buy switch — read off the offer, never off the federation id. A grant with
 * no embedded offer is neither: its price is unknown, so the card shows a plain Claim.
 */
export function isFree(e: CampaignOffer): boolean {
  return !!e.offer && e.offer.cost.length === 0;
}

/** The badge payouts on an offer — what the screen reads back from stats after a claim. */
export function badgeRewards(e: CampaignOffer): Reward[] {
  return (e.offer?.rewards ?? []).filter((r) => r.type === BADGE_REWARD_TYPE);
}

/**
 * Reads a badge's stat back after a claim, via `beam.stats.get`.
 *
 * Where it lives comes from the reward's own `statDomain` / `statAccess` properties rather than
 * being assumed (the badge provider writes `game.public`). A `game.public` stat of the CURRENT
 * player is client-readable — it is the private game domain that needs a service (`segments.ts`).
 * Returns `undefined` when the stat is not there (yet).
 */
export async function readBadgeStat(reward: Reward): Promise<string | undefined> {
  const domainType = reward.properties.statDomain === 'client' ? 'client' : 'game';
  const accessType = reward.properties.statAccess === 'private' ? 'private' : 'public';
  const stats = await requireBeam().stats.get({ domainType, accessType, stats: [reward.symbol] });
  return stats[reward.symbol];
}

/**
 * Whether the sample can act on this grant — the single Buy / Claim button on the card.
 *
 * `available` rather than merely `state`, because a Granted offer can still be gated — by a store
 * requirement or by the campaign's own condition — and that gate is re-evaluated on every read, so
 * a locked row here unlocks on its own once the player qualifies.
 *
 * The embedded offer is deliberately NOT required. Redeem takes the grant id alone; the offer is
 * what the card renders, not what the call needs. Requiring it would leave a store that sends no
 * offer with an actionable grant and no way to act on it.
 */
export function isPurchasable(e: CampaignOffer): boolean {
  return isClaimable(e) && e.available;
}
