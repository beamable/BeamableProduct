/**
 * Firing arbitrary analytics events, for validating campaign objectives from a real device.
 *
 * A campaign lane's objective ("watched analytics") converts when the enrolled player emits a named
 * event whose params satisfy the goal's conditions. Everything else in this sample emits events as a
 * side effect of doing something — reading mail, tapping a push — which is useless for testing a
 * condition like `amount >= 10`, because you cannot choose the params. This is the deliberate,
 * parameterised version.
 *
 * It sends through `beam.analytics`, the same service the SDK's own implicit reporting uses, so an
 * event fired here travels the exact path a game's would.
 */
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected - Beamable connects automatically on launch; wait for it, or use Retry connection.';

/**
 * The namespace every platform-emitted event lives in (`beam_sent`, `beam_opened`, `beam_goal_*`...).
 * Mirrors `CampaignNodeEvents.ReservedPrefix` on the server.
 */
export const RESERVED_EVENT_PREFIX = 'beam_';

/** Param values are string-valued on the wire, which is how they arrive from a real client. */
export type EventParams = Record<string, unknown>;

/**
 * Emits one analytics event for the current player.
 *
 * What keeps a test event out of the campaign funnel is its NAME: every funnel stage is a reserved
 * `beam_` name only the platform may emit, and the platform refuses to publish a campaign whose
 * goals or triggers watch one — so an authored objective event can never be mistaken for a stage.
 * That rule is enforced on the GRAPH, not on the event: firing `beam_opened` from here would still be
 * accepted (`202`) and quietly miscount a live funnel, so this refuses reserved names locally.
 *
 * @throws When Beamable is not connected, the name is reserved, or the request fails.
 */
export async function emitObjectiveEvent(name: string, params: EventParams): Promise<void> {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);

  const trimmed = name.trim();
  if (!trimmed) throw new Error('Event name is required.');
  if (trimmed.startsWith(RESERVED_EVENT_PREFIX)) {
    throw new Error(
      `"${trimmed}" is reserved: names starting with "${RESERVED_EVENT_PREFIX}" are platform funnel ` +
        'stages. Firing one here would miscount a live campaign funnel, and no objective can watch it.',
    );
  }

  // `track`, not `trackSafely`: this screen exists to tell you whether the event landed, and a
  // swallowed failure would render as a successful send that never shows up in the catalog.
  await beam.analytics.track({ name: trimmed, params });
}

/**
 * Parses the raw-JSON param editor.
 *
 * Raw JSON is the only way to send a genuinely NESTED object, which matters because nested params
 * take a different server-side path than a flat one: `ParamFlattener` walks them into dot-notation
 * keys (`details.price`), and a goal condition is evaluated against those flattened keys. Typing
 * `details.price` as a literal key reaches the matcher identically but never exercises that walk.
 *
 * @throws With a readable message when the text is not a JSON object.
 */
export function parseParamsJson(text: string): EventParams {
  const trimmed = text.trim();
  if (!trimmed) return {};

  let parsed: unknown;
  try {
    parsed = JSON.parse(trimmed);
  } catch (error) {
    throw new Error(`Params are not valid JSON: ${(error as Error).message}`);
  }

  // Arrays and scalars are rejected rather than coerced: the platform binds params as an object, and
  // silently wrapping `[1,2]` into something else would send an event that cannot match anything.
  if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
    throw new Error('Params must be a JSON object, e.g. {"currency":"USD","amount":25}.');
  }
  return parsed as EventParams;
}

/** One row of the key/value param editor. */
export interface ParamRow {
  key: string;
  value: string;
}

/**
 * Collapses editor rows into a params object, dropping rows with no key.
 *
 * Dotted keys are passed through verbatim: `ParamFlattener` produces exactly that key shape, so
 * `details.price` here is the same key a condition authored against a nested schema compares to.
 */
export function paramsFromRows(rows: ParamRow[]): EventParams {
  const params: EventParams = {};
  for (const { key, value } of rows) {
    const name = key.trim();
    if (name) params[name] = value;
  }
  return params;
}

/** A one-line summary of what was sent, for the inline button outcome and the activity log. */
export function describeSend(name: string, params: EventParams): string {
  const count = Object.keys(params).length;
  return `${name} · ${count} param${count === 1 ? '' : 's'}`;
}
