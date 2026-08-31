/**
 * App-specific bindings for the **offer federation** (`IFederatedCampaignOffer`).
 *
 * A campaign lane can attach an offer to the message it sends: the operator authors it in the
 * Portal, the campaign runtime grants it to each recipient as the send goes out, and the player
 * claims it here. Which store the offer comes from is a *federation* — an extension point, not a
 * Beamable feature. `beamable_store` is the default provider Beamable ships; a game selling
 * through Steam, a console store, or its own web shop implements the same interface under its own
 * id and is reached through these same two calls.
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
 * `CampaignOfferService` is registered in `beamClient.ts`.
 */
import type { CampaignOfferFederationId } from '@beamable/sdk';
import type { CampaignOfferEntitlement } from '@beamable/sdk/schema';
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

function requireBeam() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

/** The provider Beamable ships. It is the screen's default, never an assumption in the code. */
export const DEFAULT_STORE: CampaignOfferFederationId = 'beamable_store';

/**
 * The reserved campaign-payload key carrying the grant id for a send.
 *
 * The campaign writes it (`CampaignSendPayload.ReservedKeys`) so a rail can deep-link the player
 * straight to what they were given — the offer *ref* alone is not something a player can be sent
 * to. A client only ever **reads** it.
 */
export const OFFER_GRANT_KEY = 'beam_offer_grant';

/** An entitlement with its timestamps normalised to numbers. */
export type Entitlement = {
  grantId: string;
  offerId: string;
  state: string;
  grantedAt: number;
  /** 0 means it never expires. */
  expiresAt: number;
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
  const held = await requireBeam().campaignOffer.getEntitlements(federationId);
  return held.map(normalize).sort((a, b) => b.grantedAt - a.grantedAt);
}

/**
 * Claims a grant, returning the message to show on success.
 *
 * The endpoint answers **200 with `success: false`** for an expired, revoked, already-claimed or
 * unknown grant — a resolved promise is not a success. Throwing the server's own `message` is
 * deliberate: the most likely real failure is a payout id that is not in the realm's published
 * content manifest, and the platform's `UnknownCurrencyException` surfaces *here*, at claim time.
 * A generic "could not claim" would hide the only sentence that says what to fix.
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
  return e.state === 'granted';
}

/** Plain-language label for the four states the contract defines. */
export function describeState(state: string): string {
  switch (state) {
    case 'granted':
      return 'Ready to claim';
    case 'redeemed':
      return 'Claimed';
    case 'revoked':
      return 'Revoked by the store';
    case 'expired':
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
 * for its stat values.
 */
function normalize(e: CampaignOfferEntitlement): Entitlement {
  return {
    grantId: e.grantId ?? '',
    offerId: e.offerId ?? '',
    state: e.state ?? '',
    grantedAt: Number(e.grantedAtUnixSeconds ?? 0),
    expiresAt: Number(e.expiresAtUnixSeconds ?? 0),
  };
}
