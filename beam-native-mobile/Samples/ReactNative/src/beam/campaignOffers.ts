/**
 * App-specific bindings for the **virtual offer federation** (`IFederatedCampaignVirtualOffer`).
 *
 * A campaign lane can attach an offer to the message it sends: the operator authors it in the
 * Portal, the campaign runtime grants it to each recipient as the send goes out, and the player
 * claims it here. Which store the offer comes from is a *federation* — an extension point, not a
 * Beamable feature. `beamable_virtual_store` is the default provider Beamable ships; a game with
 * its own virtual economy implements the same interface under its own id and is reached through
 * these same two calls.
 *
 * **Virtual means soft currency.** A price here is a currency symbol and an amount, never real
 * money — a real-money offer is a separate federation, with its own contract, and it is not
 * reachable from this module. That is why there is no receipt, no product id and no native
 * purchase flow anywhere in this file.
 *
 * Two rules this module exists to keep:
 *
 *  - **The federation id is always a parameter.** Nothing here may branch on which store it is
 *    talking to — that is why the screen offers it as an input rather than hardcoding the default.
 *  - **The offer id is opaque.** Only the store that minted it can interpret it, which is why the
 *    federation id travels with it everywhere. Never parse it, show it as a name, or key anything
 *    durable on its shape.
 *
 * Only two of the seven campaign-offer routes are reachable from a player token — list what I hold,
 * and claim one of them. Granting, revoking, the catalog and purchase settlement are all
 * operator or server-to-server concerns and are permission-scoped away from a game client.
 *
 * **Claiming is not how the player gets the goods.** An offer IS a storefront listing: the price
 * and the bundle both move through the platform's commerce flow, and the claim only settles the
 * grant afterwards (and forfeits any siblings it was an alternative to). So the flow this app
 * performs is buy-then-claim — see `commerce.ts`. A different store's provider is free to fulfil
 * at claim time instead; nothing here may assume either way, which is why the screen reads the
 * inventory back rather than trusting the claim's response.
 *
 * `CampaignOfferService` is registered in `beamClient.ts`.
 */
import type { CampaignOfferFederationId } from '@beamable/sdk';
import type {
  CampaignOffer,
  CampaignOfferAmount,
} from '@beamable/sdk/schema';
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

function requireBeam() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

/** The provider Beamable ships. It is the screen's default, never an assumption in the code. */
export const DEFAULT_STORE: CampaignOfferFederationId = 'beamable_virtual_store';

/**
 * The reserved campaign-payload key carrying the grant id for a send.
 *
 * The campaign writes it (`CampaignSendPayload.ReservedKeys`) so a rail can deep-link the player
 * straight to what they were given — the offer *ref* alone is not something a player can be sent
 * to. A client only ever **reads** it.
 */
export const OFFER_GRANT_KEY = 'beam_offer_grant';

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
  /** `0` means "not known until fulfilment" (a loot roll) — never "nothing". */
  amount: number;
  /** Display name where the store has one, else the symbol's tail. */
  label: string;
  /** The store's own extras — item properties, a rarity, a duration. */
  properties: Record<string, string>;
};

/** Why an entitlement cannot be acted on. */
export type Reason = { code: string; message: string; detail: string };

/**
 * An entitlement, with the offer the store embedded in it.
 *
 * The store sends the whole offer inline so that listing entitlements is enough to render a
 * store screen — there is no second call per row. Every field below `expiresAt` may be absent:
 * the contract allows a provider to omit the offer entirely, and a third-party store legitimately
 * will, so each one has a fallback rather than a guard at the call site.
 */
export type Entitlement = {
  grantId: string;
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

/**
 * Every grant this store holds for the current player, in every state, newest first.
 *
 * This is not a to-do list: `revoked` and `redeemed` grants stay in it, so read `state` rather
 * than treating a row's presence as "claimable". Expiry is evaluated when this is read, not swept
 * in the background, so a grant past its expiry reports `expired` even though nothing has touched
 * it — which is also why the result must not be cached across a session.
 */
export async function listEntitlements(
  federationId: CampaignOfferFederationId,
): Promise<Entitlement[]> {
  // No state filter: this screen shows history too, and a locked row is exactly what the campaign
  // gate is for. A store screen that only wants claimable rows would pass ['Granted'].
  const held = await requireBeam().campaignOffer.getCampaignOffers(federationId);
  return held.map(normalize).sort((a, b) => b.grantedAt - a.grantedAt);
}

/**
 * Claims a grant — which for a virtual offer IS the purchase — returning the message to show.
 *
 * The provider spends on the player's behalf: commerce debits the price and credits the payout in
 * one inventory transaction. So this is **one call, not a buy followed by a settle**. There is
 * nothing for this client to purchase first, and doing so would charge the player twice.
 *
 * The endpoint answers **200 with `success: false`** for an expired, revoked, already-claimed or
 * unknown grant, or one whose campaign gate is not met yet — a resolved promise is not a success.
 * Throwing the server's own `message` is deliberate: it is the only sentence that says what to fix.
 */
export async function claimGrant(
  federationId: CampaignOfferFederationId,
  grantId: string,
): Promise<string> {
  const res = await requireBeam().campaignOffer.redeem(federationId, grantId);
  if (!res.success) {
    throw new Error(res.message || `The store refused this claim (${res.status ?? 'no status'})`);
  }
  // A repeat claim answers "Already redeemed." and is a success — the SDK reuses one transaction
  // id per grant so a double-tap cannot be read as a double-claim.
  return res.message || `Claimed ${grantId}`;
}

/** Only a `granted` entitlement can be claimed; every other state is terminal. */
export function isClaimable(e: Entitlement): boolean {
  return e.state === 'Granted';
}

/** Plain-language label for the four states the contract defines. */
export function describeState(state: string): string {
  switch (state) {
    case 'Granted':
      return 'Ready to claim';
    case 'Redeemed':
      return 'Claimed';
    case 'Revoked':
      return 'Revoked by the store';
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
 * that sends a bare entitlement renders as an id-only row instead of blanking the screen.
 */
function normalize(e: CampaignOffer): Entitlement {
  return {
    grantId: e.grantId ?? '',
    offerId: e.offerId ?? '',
    state: e.state ?? 'Granted',
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

function normalizeAmount(r: CampaignOfferAmount): Reward {
  const symbol = r.symbol ?? '';
  return {
    type: r.type ?? '',
    symbol,
    amount: Number(r.amount ?? 0),
    // The store's own title wins; the symbol's tail is the fallback, since an opaque id is a poor
    // thing to show a player who is about to pay for it.
    label: r.title || shortSymbol(symbol),
    properties: r.properties ?? {},
  };
}

/** `currency.gems` -> `gems`. Presentation only — never key anything on this. */
function shortSymbol(symbol: string): string {
  const i = symbol.lastIndexOf('.');
  return i >= 0 && i < symbol.length - 1 ? symbol.slice(i + 1) : symbol;
}

/**
 * Whether the sample can buy this itself.
 *
 * `available` rather than merely `state`, because a Granted offer can still be gated — by a store
 * requirement or by the campaign's own condition — and that gate is re-evaluated on every read, so
 * a locked row here unlocks on its own once the player qualifies.
 *
 * Asking about COST rather than about some collection's length is the point: "does this have a
 * price" is the actual question, and it used to be answered by counting storefront listings.
 */
export function isPurchasable(e: Entitlement): boolean {
  return isClaimable(e) && e.available && e.offer !== null;
}
