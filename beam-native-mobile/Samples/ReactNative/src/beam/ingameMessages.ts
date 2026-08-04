/**
 * App-specific binding for the "in-game messages" rail.
 *
 * The agentic-portal `InGameRailService` (federation id `ingame`) is the last-mile rail for
 * in-game delivery: when a campaign targets it, the backend worker hands the batch to the
 * service, which writes one Beamable mail per recipient (`POST /basic/mail/bulk`). So on the
 * client, "in-game messages" are just the player's mailbox — we read it with the low-level
 * Mail API. (There is no client-callable endpoint on the rail service itself; it is
 * federation-only and invoked server-side.)
 */
import { mailPostSearchByObjectId } from '@beamable/sdk/api';
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — call initBeam() (Connect to Beamable) first.';

/**
 * Fetches the current player's in-game messages (their Beamable mailbox), newest first.
 * Each entry has `subject` / `body` / `state` / `sent` (see the SDK `Message` schema).
 */
export async function listInGameMessages() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);

  const playerId = beam.player.id;
  const { body } = await mailPostSearchByObjectId(
    beam.requester,
    playerId,
    // A single "inbox" clause: all mail (not just a count), capped at 20.
    { clauses: [{ name: 'inbox', onlyCount: false, limit: 20 }] },
    playerId,
  );

  // The search response groups results per clause; flatten to the messages themselves.
  return body.results.flatMap((r) => r.content ?? []);
}
