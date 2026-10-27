import type {
  OfferGrantState,
  PlayerOffersPage,
  RedeemOfferResult,
} from '@/__generated__/schemas';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import { offersGet, offersPostRedeem } from '@/__generated__/apis';

/**
 * Identifies which provider an offer comes from.
 * @remarks
 * A provider is a customer microservice implementing `IFederatedVirtualOffer`, so the valid ids are
 * whatever the realm has deployed. `'beamable_virtual_store'` is the default provider Beamable ships;
 * a game with its own virtual economy implements the same interface under its own id.
 *
 * Never branch on this value — if client code special-cases a provider, the extension point is
 * broken. Pass it through.
 */
export type OfferFederationId = 'beamable_virtual_store' | (string & {});

/** Optional filters for {@link OfferService.getOffers}. */
export interface GetOffersOptions {
  /**
   * Which states to return. Omitted means what the player can still use: `Granted` and `Claiming`.
   * Ask for `Redeemed`, `Revoked` or `Expired` to read the player's history.
   */
  states?: OfferGrantState[];
  /** Only this provider's offers. Omitted means every provider's, in one call. */
  federationId?: OfferFederationId;
  /** The `next` of the previous page. */
  cursor?: string;
}

/**
 * The offers the current player was granted — by a campaign or by an operator — and redeeming them.
 *
 * @remarks
 * These are the only offer routes a player token can reach; granting and revoking are operator or
 * server-to-server concerns, permission-scoped away from a game client.
 *
 * Every grant is named by its `grantId`, which is all a redeem needs: Beamable knows which provider
 * made it. The offer id is **opaque** — do not parse it, show it as a name, or key anything durable
 * on its shape.
 */
export class OfferService extends ApiService {
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'offers';
  }

  /**
   * One page of the current player's offers, newest first.
   * @remarks
   * Each offer carries the offer as its provider resolved it when it was granted, so one call is
   * enough to render a store screen:
   * - `offer` is the whole offer, including `cost` and `rewards`. Fall back to the opaque `offerId`
   *   if it is missing rather than blanking the row.
   * - `offer.rewards` is disclosure, not a receipt: an empty list means "this provider cannot
   *   enumerate its payout", never "this offer gives nothing".
   * - `available` is **distinct from `state`**: a `Granted` offer can be unavailable because a
   *   provider rule (stock, schedule) or the campaign's own gate is unmet. A gated offer unlocks on a
   *   later read, with no new send — so **never cache this list across a session**, which is also
   *   true because expiry is folded in on read.
   * - `unavailableReasons` is non-empty whenever `available` is false. Prefer rendering from
   *   `properties` over `message`: a campaign gate was authored for an operator, and its structured
   *   facts are what let you show "Level 7 / 10" or hide the row entirely.
   * - `Claiming` means a redeem of it is still being resolved; it cannot be redeemed again until it is.
   *
   * Pass the returned `next` as `cursor` for the following page; it is absent on the last one.
   * @example
   * ```ts
   * const { offers } = await beam.offers.getOffers();
   * const redeemable = offers?.filter((o) => o.available) ?? [];
   * ```
   * @throws {BeamError} If the request fails, or if `cursor` is not one this list returned.
   */
  async getOffers(options: GetOffersOptions = {}): Promise<PlayerOffersPage> {
    const { body } = await offersGet(
      this.requester,
      options.cursor,
      options.federationId,
      this.accountId,
      options.states,
      this.accountId,
    );
    return body;
  }

  /**
   * Redeems a grant the player has chosen.
   *
   * @remarks
   * **For Beamable's own store this is where the money moves.** The provider buys the listing on the
   * player's behalf — the price is debited and the payout credited in one inventory transaction — and
   * the other alternatives of the same choice are forfeited. Another provider may deliver
   * differently. Either way, never treat a successful redeem as proof the inventory changed: read the
   * inventory back if you need to know.
   *
   * The campaign's gate is checked before the provider is asked, so a locked offer cannot be redeemed
   * early.
   *
   * **This resolves for a refused redeem too.** The body is `success: false` with a `status` and a
   * `message` on an expired, revoked or unknown grant, or one whose gate is unmet (then `reason` says
   * why); only a transport or auth failure rejects. Check `success` before telling the player they got
   * something.
   *
   * **Retry on `status: 'pending'`.** The provider could not tell whether the delivery happened (a
   * timeout, a 5xx), or another redeem of the same grant is in progress. Redeem again later: it is the
   * same delivery, never a second one, and a redeemed grant answers `success: true` again.
   * @example
   * ```ts
   * const res = await beam.offers.redeem(grantId);
   * if (!res.success) throw new Error(res.message ?? 'Could not redeem this offer');
   * ```
   * @param params - Provider-specific extras, passed through to the provider untouched.
   * @throws {BeamError} If the request fails.
   */
  async redeem(
    grantId: string,
    params: Record<string, string> = {},
  ): Promise<RedeemOfferResult> {
    const { body } = await offersPostRedeem(
      this.requester,
      { playerId: this.accountId, grantId, params },
      this.accountId,
    );
    return body;
  }
}
