import type { MessageRailRegistrationResponse } from '@/__generated__/schemas';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import {
  messageRailPostRegister,
  messageRailPostUnregister,
} from '@/__generated__/apis';

/**
 * Identifies which message rail (outreach channel) a player is opting in to or out of.
 * @remarks
 * A rail is backed by a customer microservice implementing `IMessageRailFederation`, so the
 * valid ids are whatever the realm has deployed. `'push'`, `'email'`, and `'ingame'` are the
 * built-in rails; any other deployed federation id is accepted.
 */
export type MessageRailFederationId =
  | 'push'
  | 'email'
  | 'ingame'
  | (string & {});

export class MessageRailService extends ApiService {
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'messageRail';
  }

  /**
   * Opts the current player in to a message rail, so the realm can target them on that channel.
   * @remarks
   * `registrationData` is opaque to Beamable — each rail defines its own keys:
   * - `push` → `{ token, platform, environment }` (the device push token)
   * - `email` → `{}` (opt-in only; the address is resolved server-side from the account)
   * - `ingame` → `{}` (opt-in only; every account already has a mailbox)
   *
   * Calling this again for an already-registered player refreshes the registration, which is how
   * a rotated push token is updated. CID/PID are taken from the authenticated token.
   * @example
   * ```ts
   * // client-side
   * await beam.messageRail.optIn('push', { token, platform: 'apns', environment: 'sandbox' });
   * await beam.messageRail.optIn('email');
   * // server-side
   * await beamServer.messageRail(playerId).optIn('ingame');
   * ```
   * @throws {BeamError} If the request fails, or if no rail is deployed for `federationId`.
   */
  async optIn(
    federationId: MessageRailFederationId,
    registrationData: Record<string, string> = {},
  ): Promise<MessageRailRegistrationResponse> {
    const { body } = await messageRailPostRegister(
      this.requester,
      { federationId, playerId: this.accountId, registrationData },
      this.accountId,
    );
    return body;
  }

  /**
   * Opts the current player out of a message rail, removing their registration.
   * @remarks
   * No `registrationData` is needed — the player is unregistered from the rail by id. For the
   * `push` rail this removes the player's device registrations rather than a single token.
   * @example
   * ```ts
   * // client-side
   * await beam.messageRail.optOut('email');
   * // server-side
   * await beamServer.messageRail(playerId).optOut('push');
   * ```
   * @throws {BeamError} If the request fails, or if no rail is deployed for `federationId`.
   */
  async optOut(
    federationId: MessageRailFederationId,
  ): Promise<MessageRailRegistrationResponse> {
    const { body } = await messageRailPostUnregister(
      this.requester,
      { federationId, playerId: this.accountId },
      this.accountId,
    );
    return body;
  }
}
