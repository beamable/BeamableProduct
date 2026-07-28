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
 * The native side emits a short `activityType` slug ('actions' | 'animated' | 'countdown'); the
 * backend + portal key on the UNQUALIFIED Swift attributes type name, so we map here — this map is
 * the single source of truth the sample owns for that contract.
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

/** activityType slug (native) → unqualified Swift `ActivityAttributes` type name (wire contract). */
const ATTRIBUTES_TYPE: Record<string, string> = {
  actions: 'BeamActionsActivityAttributes',
  animated: 'BeamAnimatedActivityAttributes',
  countdown: 'BeamCountdownActivityAttributes',
};

function attributesTypeFor(activityType: string): string {
  return ATTRIBUTES_TYPE[activityType] ?? activityType;
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

  subs.push(
    BeamNotifications.addListener('liveActivityPushToStartToken', async (p) => {
      try {
        await messageRail().optIn('push', {
          kind: 'liveActivityPushToStart',
          attributesType: attributesTypeFor(p.activityType),
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
          attributesType: attributesTypeFor(p.activityType),
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
