import type {
  OfferEntitlement,
  OfferRedeemResponse,
} from '@/__generated__/schemas';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import {
  storeOfferGetEntitlements,
  storeOfferPostRedeem,
} from '@/__generated__/apis';

/**
 * Identifies which store an offer came from.
 * @remarks
 * A store is backed by a customer microservice implementing `IFederatedStoreOffer`, so the
 * valid ids are whatever the realm has deployed. `'beamable_store'` is the default provider
 * Beamable ships; a game selling through Steam, a console store, or its own web shop
 * implements the same interface under its own id and is reached through these same calls.
 *
 * Never branch on this value — if client code special-cases a store, the extension point is
 * broken. Pass it through.
 */
export type StoreOfferFederationId = 'beamable_store' | (string & {});

/** Optional per-call overrides for {@link StoreOfferService.redeem}. */
export interface RedeemOfferOptions {
  /**
   * The idempotency key for this claim. Omit it and the service supplies a stable one — see
   * {@link StoreOfferService.redeem}. Provide your own only if you are persisting it yourself
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
 * These are the only two store-offer routes a player token can reach. Listing a catalog,
 * granting, revoking and settling a purchase are all operator or server-to-server concerns and
 * are permission-scoped away from a game client.
 *
 * The offer id on an entitlement is **opaque**: only the store that minted it can interpret it,
 * which is why `federationId` travels alongside it everywhere. Do not parse it, show it as a
 * name, or key anything durable on its shape.
 */
export class StoreOfferService extends ApiService {
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
    return 'storeOffer';
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
   * @example
   * ```ts
   * const held = await beam.storeOffer.getEntitlements('beamable_store');
   * const claimable = held.filter((e) => e.state === 'granted');
   * ```
   * @throws {BeamError} If the request fails, or if no store is deployed for `federationId`.
   */
  async getEntitlements(
    federationId: StoreOfferFederationId,
  ): Promise<OfferEntitlement[]> {
    const { body } = await storeOfferGetEntitlements(
      this.requester,
      federationId,
      this.accountId,
      this.accountId,
    );
    return body.entitlements ?? [];
  }

  /**
   * Claims a grant, moving whatever it holds into the player's inventory.
   *
   * @remarks
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
   * const res = await beam.storeOffer.redeem('beamable_store', grantId);
   * if (!res.success) throw new Error(res.message ?? 'Could not claim this offer');
   * ```
   * @throws {BeamError} If the request fails, or if no store is deployed for `federationId`.
   */
  async redeem(
    federationId: StoreOfferFederationId,
    grantId: string,
    options: RedeemOfferOptions = {},
  ): Promise<OfferRedeemResponse> {
    const { body } = await storeOfferPostRedeem(
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
