/**
 * Everything driven by an incoming notification, in one place.
 *
 * These subscriptions used to be split across `app/_layout.tsx` and `app/index.tsx`, which
 * meant `notificationOpened` and `BeamLaunchNotification` were each handled TWICE — once for
 * deep-link routing, once for funnel coordinates. Consolidating them here also means the
 * funnel coordinates survive tab switches and are filled in even when the Analytics tab isn't
 * the active one.
 */
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import * as Linking from 'expo-linking';

import {
  BeamLaunchNotification,
  BeamNotificationEvent,
  BeamNotifications,
} from '@beamable/notifications-react-native';
import type { NotificationData } from '@beamable/notifications-react-native';

import { getBeam } from '../beam/beamClient';
import { registerDevice } from '../beam/pushNotifications';
import { OFFER_GRANT_KEY } from '../beam/storeOffers';
import { useLogActions } from './logContext';

/** A URL-scheme VIEW intent captured by the native deeplink module. */
export type CapturedDeepLink = { url: string; isColdStart: boolean; at: string };

type NotificationContextValue = {
  /** Funnel coordinates — user-editable, auto-filled from a campaign push. */
  campaignId: string;
  nodeId: string;
  setCampaignId: (v: string) => void;
  setNodeId: (v: string) => void;
  /**
   * The attribution stamp of the campaign push that filled the coordinates above, if any. Only a
   * funnel event carrying this can be counted against a send node — a hand-typed campaign/node has
   * no stamp, so it reaches the warehouse but never moves the portal's funnel columns.
   */
  outreachId: string | null;
  trackId: string | null;
  /** The most recent native deep link (Android VIEW intents), for the Deep links page. */
  lastDeepLink: CapturedDeepLink | null;
  /** The notification the app was cold-started from, if any. */
  launchNotification: NotificationData | null;
  /** The deep link resolved off `launchNotification`, if it carried one. */
  launchDeepLink: string | null;
  /**
   * The offer grant id carried by the most recent campaign push, if it carried one.
   *
   * A campaign that attaches an offer to a send writes the grant id into the payload under the
   * reserved `beam_offer_grant` key, so the message can deep-link the player straight to what
   * they were given. This is the read side of that: the Offers tab claims it in one press.
   * Null until a push carrying one arrives — most pushes do not.
   */
  lastOfferGrantId: string | null;
};

const NotificationContext = createContext<NotificationContextValue | null>(null);

export function NotificationProvider({ children }: { children: ReactNode }) {
  const { append } = useLogActions();
  const [campaignId, setCampaignId] = useState('');
  const [nodeId, setNodeId] = useState('');
  const [outreachId, setOutreachId] = useState<string | null>(null);
  const [trackId, setTrackId] = useState<string | null>(null);
  const [lastDeepLink, setLastDeepLink] = useState<CapturedDeepLink | null>(null);
  const [lastOfferGrantId, setLastOfferGrantId] = useState<string | null>(null);

  /**
   * Override the funnel coordinates from a notification that carries them. Notifications
   * without campaignId/nodeId (e.g. the local test notifications) leave the user's typed
   * values untouched.
   */
  const applyCampaignCoords = useCallback(
    (n: NotificationData) => {
      // An offer attached to the send rides the same push under its reserved key. Read it
      // before the coords so a push that carries only an offer still registers.
      const grantId = offerGrantFromNotification(n);
      if (grantId) {
        setLastOfferGrantId(grantId);
        append(`Offer grant on this push: ${grantId} — claim it on the Offers tab`);
      }

      const coords = BeamNotifications.campaignCoordsFromNotification(n);
      if (coords.campaignId) setCampaignId(coords.campaignId);
      if (coords.nodeId) setNodeId(coords.nodeId);
      // Replace the stamp wholesale (including clearing it) whenever new coordinates arrive, so a
      // later push can never be tracked under the previous one's outreach.
      if (coords.campaignId || coords.nodeId) {
        setOutreachId(coords.outreachId ?? null);
        setTrackId(coords.trackId ?? null);
        append(
          `Funnel coords from notification: campaignId=${coords.campaignId ?? '—'} nodeId=${coords.nodeId ?? '—'} ` +
            `outreachId=${coords.outreachId ?? '—'} trackId=${coords.trackId ?? '—'}`,
        );
        if (!coords.outreachId || !coords.trackId) {
          append(
            'No attribution stamp on this push — the funnel will be recorded but the portal’s campaign funnel will not count it.',
          );
        }
      }
    },
    [append],
  );

  // ── Warm start: a notification (body OR an action button) tapped while running ──────
  // One handler does both jobs: seed the funnel coordinates, and route the deep link through
  // the OS exactly like a real server push would.
  BeamNotificationEvent('notificationOpened', (n) => {
    // `actionId` is set only when an action button was tapped (vs the body) — the app decides
    // what each button does. Here we just log it so the behavior is visible on-device.
    if (n.actionId) append(`Action button tapped: ${n.actionId}`);
    applyCampaignCoords(n);
    const url = BeamNotifications.deepLinkFromNotification(n);
    if (url) Linking.openURL(url).catch(() => {});
  });

  // ── Cold start: app launched by tapping a notification (local OR remote) ───────────
  const launchNotification = BeamLaunchNotification();
  const launchDeepLink = launchNotification
    ? BeamNotifications.deepLinkFromNotification(launchNotification)
    : null;

  useEffect(() => {
    if (!launchNotification) return;
    applyCampaignCoords(launchNotification);
    const url = BeamNotifications.deepLinkFromNotification(launchNotification);
    if (url) Linking.openURL(url).catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [launchNotification]);

  // ── Push token arrived → register this device with the backend `push` message rail ──
  // So the realm can target it. (`BeamPushNotifications().token` reflects the token itself;
  // here we run the registration side effect.)
  BeamNotificationEvent('tokenReceived', async ({ token }) => {
    if (!getBeam()) return append('Token not registered — Beamable is not connected yet');
    try {
      const res = await registerDevice(token, BeamNotifications.devicePushPlatform());
      append(
        `Device registered via message-rail (push): ${res.success ? 'ok' : 'failed'}${res.message ? ` — ${res.message}` : ''}`,
      );
    } catch (e) {
      append(`Token register error: ${e instanceof Error ? e.message : String(e)}`);
    }
  });

  // ── Android-only: URL-scheme VIEW intents captured by the native deeplink module ────
  // expo-router already navigates for these, so we only record them (routing again would
  // double-navigate). Inert stub on iOS / web.
  useEffect(() => {
    const sub = BeamNotifications.addDeepLinkListener((e) => {
      setLastDeepLink({
        url: e.url,
        isColdStart: e.isColdStart,
        at: new Date().toLocaleTimeString(),
      });
      append(`Native deep link captured: ${e.url} (coldStart=${e.isColdStart})`);
    });
    return () => sub.remove();
  }, [append]);

  const value = useMemo<NotificationContextValue>(
    () => ({
      campaignId,
      nodeId,
      setCampaignId,
      setNodeId,
      outreachId,
      trackId,
      lastDeepLink,
      launchNotification,
      launchDeepLink,
      lastOfferGrantId,
    }),
    [
      campaignId,
      nodeId,
      outreachId,
      trackId,
      lastDeepLink,
      launchNotification,
      launchDeepLink,
      lastOfferGrantId,
    ],
  );

  return (
    <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>
  );
}

/**
 * Digs the reserved `beam_offer_grant` key out of a received push.
 *
 * The push rail passes every unreserved `extraDataFed` key straight through into the device
 * payload, and the native module surfaces those arbitrary extras under `userInfo` (with
 * `campaignData` carrying the campaign's own block). Read both and take the first hit rather
 * than betting on one: the key is not part of `NotificationData`'s typed surface, so which
 * bucket it lands in is the native layer's business, not this app's.
 *
 * Returns null for the overwhelmingly common case of a push with no offer attached.
 */
function offerGrantFromNotification(n: NotificationData): string | null {
  const buckets: Array<Record<string, unknown> | undefined> = [n.userInfo, n.campaignData];
  for (const bucket of buckets) {
    const raw = bucket?.[OFFER_GRANT_KEY];
    if (typeof raw === 'string' && raw.trim()) return raw.trim();
  }
  return null;
}

export function useNotifications(): NotificationContextValue {
  const ctx = useContext(NotificationContext);
  if (!ctx) throw new Error('useNotifications must be used inside <NotificationProvider>');
  return ctx;
}
