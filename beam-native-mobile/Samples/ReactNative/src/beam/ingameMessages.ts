/**
 * App-specific binding for the "in-game messages" rail.
 *
 * The agentic-portal `InGameRailService` (federation id `ingame`) is the last-mile rail for
 * in-game delivery: when a campaign targets it, the backend worker hands the batch to the
 * service, which writes one Beamable mail per recipient (`POST /basic/mail/bulk`). So on the
 * client, "in-game messages" are just the player's mailbox.
 *
 * This goes through `beam.mail` rather than the low-level Mail API. The difference that matters:
 * marking a message read through the service reports the campaign funnel's `Opened` stage
 * **automatically**, so a campaign delivered over this rail shows real engagement in Campaign
 * Analytics. Calling the raw endpoint yourself skips that, and the campaign then looks as though
 * nobody ever read it.
 */
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected - Beamable connects automatically on launch; wait for it, or use Retry connection.';

function requireBeam() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

/**
 * Fetches the current player's in-game messages (their Beamable mailbox), newest first.
 * Each entry has `subject` / `body` / `state` / `sent` (see the SDK `Message` schema).
 */
export async function listInGameMessages() {
  return await requireBeam().mail.list({ limit: 20 });
}

/**
 * Marks a message as read.
 *
 * There is deliberately no analytics call here. The SDK reports the campaign funnel's `Opened` for
 * you on the Unread -> Read transition, exactly as the native SDKs already do when a player taps a
 * push notification. A game should not have to know that campaigns exist in order to be measured by
 * one.
 */
export async function markInGameMessageRead(messageId: bigint | string) {
  await requireBeam().mail.markAsRead(messageId);
}
