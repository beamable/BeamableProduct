import type {
  CampaignOfferEntitlement,
  CampaignOfferRedeemResponse,
} from '@/__generated__/schemas';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import {
  campaignOfferGetEntitlements,
  campaignOfferPostRedeem,
} from '@/__generated__/apis';

/**
 * Identifies which store an offer came from.
 * @remarks
 * A store is backed by a customer microservice implementing `IFederatedCampaignVirtualOffer`, so
 * the valid ids are whatever the realm has deployed. `'beamable_virtual_store'` is the default
 * provider Beamable ships; a game with its own virtual economy implements the same interface
 * under its own id and is reached through these same calls.
 *
 * Never branch on this value — if client code special-cases a store, the extension point is
 * broken. Pass it through.
 */
export type CampaignOfferFederationId = 'beamable_virtual_store' | (string & {});

/** Optional per-call overrides for {@link CampaignOfferService.redeem}. */
export interface RedeemOfferOptions {
  /**
   * The idempotency key for this claim. Omit it and the service supplies a stable one — see
   * {@link CampaignOfferService.redeem}. Provide your own only if you are persisting it yourself
   * across app launches.
   */
  transactionId?: string;
  /** Store-specific extras, passed through to the provider untouched. */
  params?: Record<string, string>;
}

/**
 * Claiming offers a campaign granted to the current player.
 *
 * @remarks
 * These are the only two campaign-offer routes a player token can reach. Listing a catalog,
 * granting and revoking are all operator or server-to-server concerns and are permission-scoped
 * away from a game client.
 *
 * Claiming is not how the player gets the goods: a virtual offer is a storefront listing bought
 * with soft currency through `POST /object/commerce/{playerId}/purchase`, and the claim only
 * settles the grant afterwards (and forfeits the siblings it was an alternative to). The provider
 * verifies that the purchase happened before settling, so claiming without buying is refused.
 *
 * The offer id on an entitlement is **opaque**: only the store that minted it can interpret it,
 * which is why `federationId` travels alongside it everywhere. Do not parse it, show it as a
 * name, or key anything durable on its shape.
 */
export class CampaignOfferService extends ApiService {
  /**
   * One transaction id per grant, for the life of this service instance.
   *
   * @remarks
   * A retry MUST reuse its predecessor's transaction id. The provider treats the same id on an
   * already-redeemed grant as a success ("Already redeemed.") but a *different* id on that grant
   * as a double-claim and refuses it — so minting a fresh id per attempt turns an innocent
   * double-tap into a failure. Keyed by federation as well as grant, because two stores may mint
   * the same grant id.
   */
  private readonly transactionIds = new Map<string, string>();

  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'campaignOffer';
  }

  /**
   * Everything the current player has been granted by this store, in every state.
   * @remarks
   * The list is not a to-do list: `revoked` and `redeemed` grants stay in it, so read `state`
   * rather than treating the presence of a row as "claimable". Expiry is evaluated when this is
   * read, not swept in the background, so a grant past `expiresAtUnixSeconds` reports `expired`
   * even though nothing has touched it — which is also why this must not be cached across a
   * session boundary.
   *
   * `expiresAtUnixSeconds` of `0` means the grant never expires.
   *
   * Each entitlement carries the offer inline, so this one call is enough to render a store
   * screen — there is no need to fan out a request per row:
   * - `offer` is the whole offer, including `rewards` (what the bundle contains) and `listings`
   *   (the storefront handles to act on it). A provider may leave it null; fall back to the
   *   opaque `offerId` rather than blanking the row.
   * - `offer.rewards` is disclosure, not a receipt. It says what the offer promises; it is not
   *   reconciled against what actually landed, and an empty list means "this store cannot
   *   enumerate its payout", never "this offer gives nothing".
   * - `available` is **distinct from `state`**: a `granted` grant can still be unavailable
   *   because a requirement on its listing is unmet.
   * - `unavailableReasons` is non-empty whenever `available` is false. Show `message`.
   * @example
   * ```ts
   * const held = await beam.campaignOffer.getEntitlements('beamable_store');
   * const claimable = held.filter((e) => e.state === 'granted');
   * ```
   * @throws {BeamError} If the request fails, or if no store is deployed for `federationId`.
   */
  async getEntitlements(
    federationId: CampaignOfferFederationId,
  ): Promise<CampaignOfferEntitlement[]> {
    const { body } = await campaignOfferGetEntitlements(
      this.requester,
      federationId,
      this.accountId,
      this.accountId,
    );
    return body.entitlements ?? [];
  }

  /**
   * Settles a grant the player has acted on, closing it out.
   *
   * @remarks
   * **This does not, on its own, give the player anything.** What a claim does is the store's
   * business: Beamable's default provider sells storefront listings, so the goods and the money
   * both move through the platform's commerce purchase flow and redeeming only settles the grant
   * (and forfeits any siblings it was an alternative to). Another provider may well fulfil here
   * instead. Either way, never treat a successful redeem as proof the inventory changed — read
   * the inventory back if you need to know.
   *
   * A player may only ever redeem their own grants — the gateway resolves the player from the
   * token and rejects a mismatch with 403 `IncorrectPlayer`.
   *
   * **This resolves for a refused claim too.** The response is `success: false` with a `status`
   * and a `message` on an expired, revoked, already-claimed or unknown grant; only a transport
   * or auth failure rejects. Check `success` before telling the player they got something.
   *
   * Surface `message` rather than a generic "could not claim": the most likely real failure is a
   * payout id that is not in the realm's published content manifest, and the server's message is
   * the only thing that says so.
   * @example
   * ```ts
   * const res = await beam.campaignOffer.redeem('beamable_store', grantId);
   * if (!res.success) throw new Error(res.message ?? 'Could not claim this offer');
   * ```
   * @throws {BeamError} If the request fails, or if no store is deployed for `federationId`.
   */
  async redeem(
    federationId: CampaignOfferFederationId,
    grantId: string,
    options: RedeemOfferOptions = {},
  ): Promise<CampaignOfferRedeemResponse> {
    const { body } = await campaignOfferPostRedeem(
      this.requester,
      {
        federationId,
        playerId: this.accountId,
        grantId,
        request: {
          transactionId:
            options.transactionId ?? this.transactionId(federationId, grantId),
          params: options.params ?? {},
        },
      },
      this.accountId,
    );
    return body;
  }

  /** The stable transaction id for a grant, minted on first use. */
  private transactionId(federationId: string, grantId: string): string {
    const key = `${federationId}:${grantId}`;
    const existing = this.transactionIds.get(key);
    if (existing) return existing;

    const minted = newTransactionId();
    this.transactionIds.set(key, minted);
    return minted;
  }
}

/**
 * `crypto.randomUUID` is not everywhere the SDK runs (older React Native runtimes, and any
 * non-secure browser context), so fall back rather than throw. The value only has to be unique
 * per claim attempt — it is never parsed.
 */
function newTransactionId(): string {
  const c = globalThis.crypto;
  if (typeof c?.randomUUID === 'function') return c.randomUUID();
  return `txn-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}
