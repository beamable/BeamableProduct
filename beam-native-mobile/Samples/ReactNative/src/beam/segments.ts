/**
 * App-specific binding for Beamable **segments**.
 *
 * A segment is a rule authored in the Portal over a player's *stats* — e.g.
 * `PLAYER_LEVEL >= 10`. The backend watches the stat keys a segment's rule mentions
 * (`SegmentResponse.watchedKeys`); when one of them changes for a player, it re-evaluates the
 * rule and moves the player IN or OUT of the segment. Campaigns, announcements and listings
 * then target the segment rather than individual players.
 *
 * **The stats segments read are GAME PRIVATE** — object id `game.private.player.{playerId}`.
 * A client token cannot touch that domain (the Web SDK spells this out: "Game domain stats can
 * only be fetched by the game server"), so `beam.stats` is NOT the write path here. Both the
 * read and the write go through the **`PlayerStatsService`** microservice, which runs with a
 * privileged identity and only ever acts on the calling player. Its source lives in the
 * agentic-portal workspace at `services/PlayerStatsService`.
 *
 * So the client half of "segments" is:
 *  - write a stat  → `PlayerStatsService.AddToMyStat` / `SetMyStat`   (this is what moves the player)
 *  - read back     → `realmsGetPlayersSegments()` / `…SegmentsTransitions()`
 *
 * There is no `beam.segments` service in the Web SDK — the two membership reads below are raw
 * generated REST bindings from `@beamable/sdk/api`, called with `beam.requester` (the same
 * pattern the SDK Explorer catalog uses for endpoints without a high-level service).
 */
import {
  realmsGetPlayersSegments,
  realmsGetPlayersSegmentsTransitions,
} from '@beamable/sdk/api';
import type { Beam } from '@beamable/sdk';

import { getBeam, getPlayerStatsService } from './beamClient';
import type { PlayerStatsServiceClient } from './beamable/clients/PlayerStatsServiceClient';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

/**
 * The stats object id the segment engine evaluates, shown in the UI. Writes land here via the
 * microservice; the client SDK cannot address this domain at all.
 */
export const STAT_OBJECT = 'game.private.player.{playerId}';

/** One demo stat this screen can bump, and the segment rule it is meant to drive. */
export interface DemoStat {
  /** The stat key, exactly as it must be typed in the Portal segment rule. */
  key: string;
  label: string;
  /** How much one press of the card's add button adds. */
  step: number;
  /** What the stat is supposed to mean in a game. */
  description: string;
  /** An example Portal rule over this key, and the segment it would produce. */
  rule: string;
}

/**
 * The stat the Segments tab exposes as a card.
 *
 * One key on purpose: a key of your own, which nothing else writes. The platform's starter
 * aggregates (`SPEND_*`, `PURCHASES_*`, `SESSIONS_*`) are maintained from real session and
 * purchase records and recomputed at session start, so a hand-written value there is transient —
 * this one sticks.
 *
 * Nothing is pre-provisioned: author a rule over `PLAYER_LEVEL` in Portal → Segments and the card
 * starts moving the player. Until then it writes fine and moves nobody, which is the most common
 * reason this screen looks like it "does nothing". Use "Any stat key" below for whatever your own
 * realm's rules already watch.
 */
export const DEMO_STATS: readonly DemoStat[] = [
  {
    key: 'PLAYER_LEVEL',
    label: 'Player level',
    step: 1,
    description:
      'A game key of your own — no platform job recomputes it, so the value survives the next launch.',
    rule: 'author e.g. PLAYER_LEVEL >= 10 → "Veterans"',
  },
];

function requireBeam(): Beam {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

function requireStatsService(): PlayerStatsServiceClient {
  const svc = getPlayerStatsService();
  if (!svc) throw new Error(NOT_CONNECTED);
  return svc;
}

/**
 * Runs one call against the service, translating the one failure this screen is far more likely
 * to hit than any other.
 *
 * `BindingNotFoundException` inside an HTTP 500 means the PLATFORM has no route for
 * `micro_PlayerStatsService` in this realm. It reads like a bug in the call, so spell out the two
 * things it actually is — every card on this screen fails identically, and the raw message names
 * neither cause.
 */
async function callService<T>(
  run: (svc: PlayerStatsServiceClient) => Promise<T>,
): Promise<T> {
  const svc = requireStatsService();
  try {
    return await run(svc);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    if (msg.includes('BindingNotFoundException')) {
      throw new Error(
        'PlayerStatsService is not reachable in this realm — the platform has no binding for it.' +
          ' Either it is not deployed here (`beam deploy release`), or it is running locally' +
          ' (`beam project run`), in which case the call needs that machine\'s routing key and this' +
          ' sample does not send one.',
      );
    }
    throw e instanceof Error ? e : new Error(msg);
  }
}

/** Reads a stat map as numbers, treating "unset" and "not a number" alike as 0. */
export function statNumber(stats: Record<string, string>, key: string): number {
  const n = Number(stats[key]);
  return Number.isFinite(n) ? n : 0;
}

/**
 * Every game-private stat on this player, via `PlayerStatsService.GetMyStats`.
 *
 * The service returns a list rather than a map (the client generator types a C# dictionary
 * poorly), so flatten it here — the UI wants lookups by key.
 */
export async function readStats(): Promise<Record<string, string>> {
  const { items } = await callService((svc) => svc.getMyStats());
  // `String(...)` is not redundant: the SDK's JSON reviver turns numeric strings longer than 10
  // digits into BigInt, so timestamp stats (DATE_SESSION, LAST_PURCHASE_TS) arrive as bigint
  // despite the declared type. Normalising here keeps that out of the UI.
  return Object.fromEntries(items.map((s) => [s.key, String(s.value)]));
}

/**
 * Writes one game-private stat to an absolute value. This is the call that can move the player.
 *
 * The endpoint answers 200 with `success: false` for a rejected key/value, so a resolved
 * promise is not on its own a success.
 */
export async function setStat(key: string, value: string | number): Promise<string> {
  const res = await callService((svc) => svc.setMyStat({ key, value: String(value) }));
  if (!res.success) throw new Error(res.message || `Setting ${key} was rejected by the service`);
  return res.message;
}

/**
 * Adds `by` to a game-private stat and returns its new value.
 *
 * The read-modify-write happens inside the microservice — one round trip, and the client never
 * needs access to the game domain. `amount` is a C# `long`, which the generated client types as
 * `bigint | string`; a plain decimal string is what the SDK would put on the wire for either, so
 * that is what we send.
 */
export async function addToStat(key: string, by: number): Promise<number> {
  const res = await callService((svc) => svc.addToMyStat({ key, amount: String(by) }));
  if (!res.success) throw new Error(res.message || `Adding to ${key} was rejected by the service`);
  const next = Number(res.value);
  return Number.isFinite(next) ? next : 0;
}

/** Removes one game-private stat from this player. Idempotent. */
export async function deleteStat(key: string): Promise<string> {
  const res = await callService((svc) => svc.deleteMyStat({ key }));
  if (!res.success) throw new Error(res.message || `Deleting ${key} was rejected by the service`);
  return res.message;
}

/** The ceiling the service enforces on one bulk create — mirrored here for the input's hint. */
export const MAX_BULK_PLAYERS = 25;

export type CreatedPlayer = Awaited<
  ReturnType<PlayerStatsServiceClient['createPlayersWithStat']>
>['players'][number];

/**
 * Creates `count` brand-new players and sets one game-private stat on each, via
 * `PlayerStatsService.CreatePlayersWithStat`.
 *
 * This is how you get a segment with MEMBERS rather than a rule tested against one player: every
 * new player is a fresh anonymous account whose only distinguishing mark is the stat, so a rule
 * over `key` should pick up all of them.
 *
 * The accounts are real and permanent — the service caps the count and there is no delete. It is
 * a QA fixture, which is why the service's docs say not to ship a client that can reach it.
 */
export async function createPlayersWithStat(
  count: number,
  key: string,
  value: string,
): Promise<{ created: CreatedPlayer[]; message: string }> {
  const res = await callService((svc) => svc.createPlayersWithStat({ count, key, value }));
  // `success: false` covers a rejected key/value/count and "every account failed" alike; both
  // carry their reason in `message`.
  if (!res.success) throw new Error(res.message || 'The service rejected the bulk create');
  return { created: res.players, message: res.message };
}

export type PlayerSegment = Awaited<
  ReturnType<typeof realmsGetPlayersSegments>
>['body'][number];

export type SegmentTransition = NonNullable<
  Awaited<ReturnType<typeof realmsGetPlayersSegmentsTransitions>>['body']['records']
>[number];

/**
 * The segments this player is currently in
 * (`GET /api/realms/{realmId}/players/{playerId}/segments`).
 *
 * `realmId` / `customerId` are the pid / cid the SDK connected with, and the player id is this
 * player — a client can only read its own membership.
 */
export async function listMySegments(): Promise<PlayerSegment[]> {
  const beam = requireBeam();
  const { body } = await realmsGetPlayersSegments(
    beam.requester,
    beam.player.id,
    beam.pid,
    beam.cid,
  );
  return body ?? [];
}

/**
 * The player's recent segment enter/exit history
 * (`GET /api/realms/{realmId}/players/{playerId}/segments/transitions`).
 *
 * This is the proof that a stat write did something: bump a stat that a rule watches, refresh,
 * and an `Enter` / `Exit` row with `cause: Rule` shows up.
 */
export async function listMyTransitions(): Promise<SegmentTransition[]> {
  const beam = requireBeam();
  const { body } = await realmsGetPlayersSegmentsTransitions(
    beam.requester,
    beam.player.id,
    beam.pid,
    undefined, // cursor — first page only
    beam.cid,
  );
  return body.records ?? [];
}

/**
 * Formats a read failure for display.
 *
 * The two membership reads are the only calls on this screen that may be closed to a plain
 * player token depending on how the realm is configured, and `BeamError` reports that as a bare
 * "failed with status 403" — which reads like a bug rather than a permission. Annotate it.
 */
export function describeSegmentError(e: unknown): string {
  const msg = e instanceof Error ? e.message : String(e);
  if (/status 40[13]/.test(msg)) {
    return `${msg}\n(this realm does not let a player token read segment membership — the stat writes above still work, and membership is visible in the Portal)`;
  }
  return msg;
}

/** `Date | string` → a short local timestamp. JSON gives strings; the schema types say Date. */
export function formatWhen(when: Date | string | null | undefined): string {
  if (!when) return 'unknown';
  const d = when instanceof Date ? when : new Date(when);
  return Number.isNaN(d.getTime()) ? String(when) : d.toLocaleString();
}
