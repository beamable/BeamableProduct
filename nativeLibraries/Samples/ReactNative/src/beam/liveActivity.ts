/**
 * App-specific binding for **Live Activity push-to-start** registration (iOS 17.2+).
 *
 * The native SDK observes two kinds of ActivityKit push token and emits them to JS:
 *   - `liveActivityPushToStartToken` — one per attributes type; lets the backend START a Live
 *     Activity for this player via APNs (`apns-push-type: liveactivity`, `event: "start"`).
 *   - `liveActivityUpdateToken`      — one per running activity; lets the backend UPDATE / END it.
 *
 * We forward each token to the same `push` message rail the device token uses, through the Web
 * SDK's `beam.messageRail` service (`POST /api/message-rail/register`), tagged with a `kind`
 * discriminator the push federation reads. Unlike the device token, these are keyed by
 * `attributesType` (push-to-start) or `activityId` (update).
 *
 * Native emits both a short `activityType` slug ('actions' | 'animated' | 'countdown') and the
 * UNQUALIFIED Swift `attributesType` the backend + portal key on, so we forward the latter directly
 * (the local slug map survives only as a fallback for older native builds).
 *
 * A third event, `liveActivityCapability`, reports whether this build can actually DRAW an activity
 * (iOS 17.2+, the player's Settings toggle, and a widget embedded + declared). We refuse to register a
 * token for a type that fails it: the rail chooses Live Activity vs notification purely by token
 * presence, so publishing a token we can't render would cost the player both surfaces.
 *
 * Caveat, and a known gap: `optOut('push')` removes ALL of a player's push registrations, so there is
 * no way to WITHDRAW just a stale Live Activity token from the client. If a build that shipped a widget
 * is replaced by one that doesn't, the server-side token outlives it until APNs reports it dead. A
 * per-token unregister on the rail would close this.
 */
import { BeamNotifications } from '@beamable/notifications-react-native';
import { DEFAULT_APNS_ENVIRONMENT } from '@beamable/notifications-react-native';
import type { Subscription } from '@beamable/notifications-react-native';
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — call initBeam() (Connect to Beamable) first.';

/** Resolve the message-rail service, or throw if not connected. */
function messageRail() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam.messageRail;
}

/**
 * activityType slug (native) → unqualified Swift `ActivityAttributes` type name (wire contract).
 *
 * Only a FALLBACK now: the SDK emits `attributesType` on every token event since it owns the mapping.
 * Kept so this sample still works against an older native build that emits the slug alone.
 */
const ATTRIBUTES_TYPE: Record<string, string> = {
  actions: 'BeamActionsActivityAttributes',
  animated: 'BeamAnimatedActivityAttributes',
  countdown: 'BeamCountdownActivityAttributes',
};

function attributesTypeFor(event: { activityType: string; attributesType?: string }): string {
  return (
    event.attributesType || ATTRIBUTES_TYPE[event.activityType] || event.activityType
  );
}

/**
 * Start observing Live Activity push tokens and forward them to the `push` rail. Call once after
 * connecting to Beamable (registration needs the authenticated player). Returns a subscription that
 * stops the forwarding. No-op / harmless on non-iOS and pre-17.2 (no tokens ever arrive).
 *
 * @param log optional sink for human-readable status lines (the sample's event log).
 */
export function startLiveActivityTokenForwarding(
  log?: (line: string) => void,
): Subscription {
  // Kick off native token observation (idempotent).
  BeamNotifications.startLiveActivityPushRegistration();

  const subs: Subscription[] = [];

  // Which attributes types this build can actually DRAW. The SDK gates token emission on the same
  // check, so this is belt-and-braces — but it is also the only place that can report WHY a device
  // silently takes the notification path instead, which is the first question when a Live Activity
  // doesn't appear.
  const unavailable = new Set<string>();

  subs.push(
    BeamNotifications.addListener('liveActivityCapability', (p) => {
      for (const cap of p.capabilities) {
        if (cap.available) {
          unavailable.delete(cap.attributesType);
        } else if (!unavailable.has(cap.attributesType)) {
          unavailable.add(cap.attributesType);
          log?.(`LA unavailable (${cap.activityType}): ${cap.reason} — expect a notification instead.`);
        }
      }
    }),
  );

  subs.push(
    BeamNotifications.addListener('liveActivityPushToStartToken', async (p) => {
      const attributesType = attributesTypeFor(p);
      // Never publish a token for a type this build can't render: the rail picks Live-Activity-vs-
      // notification purely by token presence, so a token we can't draw costs the player BOTH surfaces.
      if (unavailable.has(attributesType)) {
        log?.(`LA push-to-start token ignored (${p.activityType}): not renderable on this device.`);
        return;
      }
      try {
        await messageRail().optIn('push', {
          kind: 'liveActivityPushToStart',
          attributesType,
          token: p.token,
          environment: DEFAULT_APNS_ENVIRONMENT,
        });
        log?.(`LA push-to-start token registered (${p.activityType}).`);
      } catch (e) {
        log?.(`LA push-to-start register error: ${e instanceof Error ? e.message : String(e)}`);
      }
    }),
  );

  subs.push(
    BeamNotifications.addListener('liveActivityUpdateToken', async (p) => {
      try {
        await messageRail().optIn('push', {
          kind: 'liveActivityUpdate',
          attributesType: attributesTypeFor(p),
          activityId: p.activityId,
          token: p.token,
          environment: DEFAULT_APNS_ENVIRONMENT,
        });
        log?.(`LA update token registered (${p.activityType} / ${p.activityId}).`);
      } catch (e) {
        log?.(`LA update-token register error: ${e instanceof Error ? e.message : String(e)}`);
      }
    }),
  );

  subs.push(
    BeamNotifications.addListener('liveActivityStarted', (p) => {
      log?.(`Live Activity started: ${p.activityType} (${p.activityId}).`);
    }),
  );

  return { remove: () => subs.forEach((s) => s.remove()) };
}
