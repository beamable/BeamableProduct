/**
 * Message-rail registration through the Beamable backend.
 *
 * The client never talks to a rail microservice directly. Instead it calls the platform
 * `/message-rail/register` (and `/unregister`) endpoint with a `federationId`, and the backend
 * forwards the call to the matching federation microservice's
 * `RegisterUserWithMessageRail` / `UnregisterUserWithMessageRail`.
 *
 * `registrationData` is opaque to Beamable — each rail defines its own keys:
 *  - `push`   → `{ token, platform, environment }` (the device push token; see pushNotifications.ts)
 *  - `email`  → `{}` (opt-in flag; the address is resolved server-side from the account)
 *  - `ingame` → `{}` (opt-in flag; every account already has a mailbox)
 *
 * CID/PID are taken from the authenticated player token, so they are never sent in the body.
 */
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — call initBeam() (Connect to Beamable) first.';

/** Federation ids the sample registers with (must match a deployed rail microservice). */
export type MessageRailFederationId = 'push' | 'email' | 'ingame';

/** Response shape for both register and unregister. */
export interface MessageRailRegistrationResponse {
  playerId: string;
  success: boolean;
  message?: string;
}

/** Registers (or refreshes) the current player with a rail federation. */
export async function registerRail(
  federationId: MessageRailFederationId,
  registrationData: Record<string, string> = {},
): Promise<MessageRailRegistrationResponse> {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  const { body } = await beam.requester.request<MessageRailRegistrationResponse>({
    method: 'POST',
    url: '/message-rail/register',
    withAuth: true,
    body: {
      federationId,
      playerId: beam.player.id,
      registrationData,
    },
  });
  return body;
}

/** Removes the current player's registration with a rail federation. */
export async function unregisterRail(
  federationId: MessageRailFederationId,
): Promise<MessageRailRegistrationResponse> {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  const { body } = await beam.requester.request<MessageRailRegistrationResponse>({
    method: 'POST',
    url: '/message-rail/unregister',
    withAuth: true,
    body: {
      federationId,
      playerId: beam.player.id,
    },
  });
  return body;
}
