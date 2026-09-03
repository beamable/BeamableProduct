/**
 * App-specific bindings for **buying a virtual offer**.
 *
 * A campaign offer is a storefront listing, and a virtual listing is bought with soft currency
 * through the platform's commerce service:
 *
 * ```
 *   POST /object/commerce/{playerId}/purchase   { purchaseId }  -> InventoryUpdateResponse
 * ```
 *
 * One call. It resolves the listing, checks it is active and that the player meets its
 * requirements and purchase limits, then **debits the price and credits the payout in a single
 * inventory transaction** and marks the offer purchased. There is nothing to verify afterwards and
 * no receipt to carry, because no external store was involved — which is the whole reason virtual
 * offers are their own federation. A real-money offer needs a native purchase flow, a signed
 * receipt and a settlement callback; none of that exists here, and none of it should.
 *
 * The route is `requireSelf` on the platform side, so a player token can only ever buy for itself.
 *
 * **Why the screen still diffs the wallet afterwards.** `InventoryUpdateResponse.deltas` is not
 * populated on this path — commerce calls the plain `updateInventory`, and only
 * `updateInventoryWithDeltas` sets `includeDeltas`. So the response confirms the purchase
 * happened but not what moved. Reading the balances either side of it is the only honest answer,
 * and it has the side benefit of capturing the price as well as the payout.
 */
import { commercePostPurchaseByObjectId } from '@beamable/sdk/api';
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

function requireBeam() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

/**
 * The id commerce knows a listing by: `"{listingSymbol}:{storeSymbol}"`.
 *
 * The platform splits on `:` and treats the store half as optional — omitting it searches every
 * store — so it is only appended when the entitlement actually carries one.
 */
export function purchaseIdFor(listingSymbol: string, storeSymbol?: string | null): string {
  return storeSymbol ? `${listingSymbol}:${storeSymbol}` : listingSymbol;
}

/**
 * Buys a listing directly, spending the player's currency.
 *
 * **Not used by the offer path, deliberately.** Claiming a campaign offer IS its purchase — the
 * provider spends on the player's behalf — so calling this before a claim would charge them twice.
 * This stays as the plain direct-commerce binding, for a storefront screen that has no grant behind
 * it.
 *
 * Commerce is the authority on whether a purchase is allowed, and its refusals are worth surfacing
 * verbatim rather than pre-empting: `Unavailable` for a listing that is not active or does not
 * exist, and a limit or requirement failure for one the player cannot buy right now. Guessing any
 * of that client-side would only produce a worse message.
 */
export async function purchaseListing(purchaseId: string): Promise<void> {
  const beam = requireBeam();
  await commercePostPurchaseByObjectId(beam.requester, beam.player.id, { purchaseId });
}
