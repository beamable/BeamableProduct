import type { BeamEnvironmentName } from '@/configs/BeamEnvironmentConfig';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { TokenData, TokenStorage } from '@/platform/types/TokenStorage';
import { TokenStorage as TokenStorageBase } from '@/platform/types/TokenStorage';
import { createStandaloneRequester } from '@/core/BeamUtils';
import {
  BeamMicroServiceClient,
  type BeamMicroServiceClientCtor,
  type BeamMicroServiceHost,
} from '@/core/BeamMicroServiceClient';
import { BeamError } from '@/constants/Errors';
import {
  customersGetByCustomerId,
  customersGetRealms,
  customersGetZones,
  customersGetZonesByCustomerId,
} from '@/__generated__/apis';
import type { RealmView } from '@/__generated__/schemas/RealmView';
import type { ZoneView } from '@/__generated__/schemas/ZoneView';

/**
 * Configuration options for initializing the {@link BeamZoneSdk}.
 *
 * @remarks
 * The zone SDK talks to the customer-directory endpoints (`/api/customers/{cid}/...`),
 * which are **bearer-token, customer-scoped** — the caller must be an operator/account
 * with customer-level permissions. Supply that identity via {@link token} or
 * {@link tokenStorage}. This is a tooling/portal surface, not a game-client surface:
 * a browser cannot authenticate "as a zone" (that needs the zone secret), so the zone
 * SDK rides an operator token instead.
 */
export interface BeamZoneSdkConfig {
  /** Beamable Customer ID (CID). */
  cid: string;

  /**
   * The zone (ZID) this SDK is scoped to, if any. Purely informational — it lets an
   * extension answer "what zone am I part of" without threading it separately. The
   * customer-directory queries are keyed on {@link cid}, not this value.
   */
  zid?: string;

  /**
   * The Beamable environment to connect to.
   * Can be one of 'prod', 'stg', 'dev', or a custom environment name.
   * @default 'prod'
   */
  environment?: BeamEnvironmentName;

  /**
   * Custom HTTP transport used as the inner requester. Mirrors
   * `createStandaloneRequester`'s `requester` option — it is the raw transport, not a
   * pre-scoped Beam requester. Auth/scope headers are still applied on top.
   */
  requester?: HttpRequester;

  /**
   * Operator bearer token to authenticate the customer-directory calls. Convenience
   * over {@link tokenStorage} — when set, an in-memory token storage is created for it.
   */
  token?: string;

  /** Custom token storage implementation. Takes precedence over {@link token}. */
  tokenStorage?: TokenStorage;

  /** Unique tag for instance-specific token storage synchronization. */
  instanceTag?: string;
}

/**
 * Read-only directory of the customer's realms and zones, callable with an operator
 * token. The web analog of the C# `IZoneCustomerApi` exposed by a `ZoneMicroservice`.
 */
export class ZoneCustomerApi {
  constructor(
    private readonly requester: HttpRequester,
    private readonly cid: string,
  ) {}

  /**
   * Lists every realm (project) that belongs to the customer, including each realm's
   * zone binding (`zoneId`). Use this to resolve which realm to hand to `Beam.init`, or
   * to filter realms by zone.
   */
  async getRealms(): Promise<RealmView[]> {
    const { body } = await customersGetByCustomerId(this.requester, this.cid);
    return body.realms ?? [];
  }

  /**
   * Fetches a single realm, including its `zoneId` binding. Handy for the PRD's override
   * rule: when a pid is selected, that realm's zone is authoritative over any local zid.
   */
  async getRealm(pid: string): Promise<RealmView> {
    const { body } = await customersGetRealms(this.requester, this.cid, pid);
    return body;
  }

  /** Lists every zone that belongs to the customer. */
  async getZones(): Promise<ZoneView[]> {
    const { body } = await customersGetZonesByCustomerId(
      this.requester,
      this.cid,
    );
    return body.zones ?? [];
  }

  /** Fetches a single zone by id. */
  async getZone(zid: string): Promise<ZoneView> {
    const { body } = await customersGetZones(this.requester, this.cid, zid);
    return body;
  }
}

/**
 * The zone (`cid.zid`) analog of the {@link Beam} client — a thin, requester-only SDK
 * for interacting with customer realm/zone directory info. A zone runs *above* realms,
 * so this SDK deliberately has **no** player/realm surface (no guest login, tokens,
 * accounts, content, or realtime — all of which are realm-scoped). To act within a
 * realm, resolve a pid here and start a realm session with `Beam.init({ cid, pid })`.
 *
 * @example
 * ```ts
 * const zone = await BeamZoneSdk.init({ cid, zid, token: operatorToken });
 * const realms = await zone.customer.getRealms();
 * const inThisZone = realms.filter((r) => r.zoneId === zone.zid);
 * // user picks a realm, then start a player session:
 * const beam = await Beam.init({ cid, pid: inThisZone[0].realmId });
 * ```
 */
export class BeamZoneSdk implements BeamMicroServiceHost {
  /** The Beamable Customer ID. */
  readonly cid: string;
  /** The zone this SDK is scoped to, if any. */
  readonly zid?: string;
  /** The HTTP requester used by the zone SDK. */
  readonly requester: HttpRequester;
  /** Customer-directory queries (realms and zones). */
  readonly customer: ZoneCustomerApi;

  private constructor(config: BeamZoneSdkConfig, requester: HttpRequester) {
    this.cid = config.cid;
    this.zid = config.zid;
    this.requester = requester;
    this.customer = new ZoneCustomerApi(requester, config.cid);
  }

  /**
   * The routing scope segment for microservice calls — the zone id. This makes
   * `beam.<name>Client` calls route to `/basic/{cid}.{zid}.micro_{service}/...`,
   * the zone analog of a realm SDK's `{cid}.{pid}`.
   * @see BeamMicroServiceHost
   */
  get microServiceScope(): string {
    if (!this.zid) {
      throw new BeamError(
        'BeamZoneSdk was initialized without a `zid`, so it cannot call zone-running microservices. Provide `zid` in the config.',
      );
    }
    return this.zid;
  }

  /**
   * Registers zone-running microservice clients so a zone extension can call a
   * zone service the same way a realm extension calls a realm service —
   * `beam.use(MyClient)` then `beam.myClient.method()`. The zone SDK hosts only
   * microservice clients (no realm/player ApiServices).
   * @example
   * ```ts
   * const beam = await BeamZoneSdk.init({ cid, zid, token });
   * beam.use(MyZoneServiceClient);
   * await beam.myZoneServiceClient.doThing({ ... });
   * ```
   */
  use<T extends BeamMicroServiceClientCtor<any>>(ctors: readonly T[]): this;
  use<T extends BeamMicroServiceClientCtor<any>>(ctor: T): this;
  use(ctorOrCtors: any): this {
    const ctors = Array.isArray(ctorOrCtors) ? ctorOrCtors : [ctorOrCtors];
    ctors.forEach((c) => this.registerMicroClient(c));
    return this;
  }

  /** Registers a single microservice client, exposed as `beam.<serviceName>Client`. */
  private registerMicroClient<T extends BeamMicroServiceClient>(
    Ctor: BeamMicroServiceClientCtor<T>,
  ) {
    // The generated client ctor is typed to a realm `BeamBase`, but only uses
    // the `BeamMicroServiceHost` surface (cid/requester/microServiceScope) which
    // this zone SDK also provides — so the cast is safe at runtime.
    const client = new Ctor(this as any);
    const serviceName = client.serviceName;
    const identifier =
      serviceName.charAt(0).toLowerCase() + serviceName.slice(1);
    const clientName = `${identifier}Client`;
    (this as any)[clientName] = client;
  }

  /** Initialize a new zone SDK instance. */
  static async init(config: BeamZoneSdkConfig): Promise<BeamZoneSdk> {
    const tokenStorage =
      config.tokenStorage ??
      (config.token ? new InMemoryTokenStorage(config.token) : undefined);

    // Scope is cid-only (no pid): `createStandaloneRequester` emits `X-BEAM-SCOPE: <cid>`
    // when no pid is supplied, which is exactly the zone/customer-directory scope.
    const requester = createStandaloneRequester({
      cid: config.cid,
      environment: config.environment,
      requester: config.requester,
      tokenStorage,
      tokenStorageTag: config.instanceTag,
    });

    return new BeamZoneSdk(config, requester);
  }
}

/**
 * Minimal, non-persisted {@link TokenStorage} that holds a single operator access token.
 * Used for the {@link BeamZoneSdkConfig.token} convenience — there is no refresh flow for
 * an operator token here, so refresh is left unset.
 */
class InMemoryTokenStorage extends TokenStorageBase {
  constructor(accessToken: string) {
    super();
    this.accessToken = accessToken;
  }

  async getTokenData(): Promise<TokenData> {
    return {
      accessToken: this.accessToken,
      refreshToken: this.refreshToken,
      expiresIn: this.expiresIn,
    };
  }

  async setTokenData(data: Partial<TokenData>): Promise<this> {
    if ('accessToken' in data) this.accessToken = data.accessToken ?? null;
    if ('refreshToken' in data) this.refreshToken = data.refreshToken ?? null;
    if ('expiresIn' in data) this.expiresIn = data.expiresIn ?? null;
    return this;
  }

  clear(): void {
    this.accessToken = null;
    this.refreshToken = null;
    this.expiresIn = null;
  }

  dispose(): void {}
}
