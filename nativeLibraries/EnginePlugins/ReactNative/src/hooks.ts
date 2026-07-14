/**
 * React hooks for `@beamable/notifications-react-native` (P0 ergonomics).
 *
 * This is a React library, so the idiomatic way to consume it is a hook — not a manual
 * `useEffect` + `addListener` + `useState` + cleanup dance in every screen. These hooks own
 * the subscription lifecycle (subscribe on mount, remove on unmount) and expose the SDK's
 * push state as reactive React state.
 *
 * `react` is a peer dependency of this package, so importing it here resolves to the host
 * app's React instance.
 */
import { useCallback, useEffect, useRef, useState } from 'react';

// Value imports come from `./index`, which Metro platform-swaps to `./index.web.ts` on web —
// so these hooks bind to the correct per-platform façade with no web-specific copy.
import {
  BeamableNotifications,
  addListener,
  isBeamableNotificationsSupported,
} from './index';
import type {
  BeamNotificationsState,
  EventMap,
  NotificationData,
  PermissionOptions,
  PermissionResult,
} from './types';

export type { BeamNotificationsState } from './types';

/**
 * Subscribe to a Beamable notification event for the lifetime of the component. The handler
 * may change every render without re-subscribing (it's read through a ref), so you don't
 * need to memoize it. No-op on unsupported platforms.
 *
 * @example
 * BeamNotificationEvent('notificationOpened', (n) => router.navigate(n.deeplink));
 */
export function BeamNotificationEvent<K extends keyof EventMap>(
  event: K,
  handler: (payload: EventMap[K]) => void,
): void {
  const saved = useRef(handler);
  saved.current = handler;

  useEffect(() => {
    if (!isBeamableNotificationsSupported) return;
    const sub = addListener(event, (payload) => saved.current(payload));
    return () => sub.remove();
  }, [event]);
}

/**
 * Resolve the cold-start launch notification once, on mount. Returns `null` until it
 * resolves (and if the app was not launched by tapping a notification). Warm-start taps
 * arrive on the `notificationOpened` event instead — see {@link BeamPushNotifications}.
 */
export function BeamLaunchNotification(): NotificationData | null {
  const [launch, setLaunch] = useState<NotificationData | null>(null);

  useEffect(() => {
    let active = true;
    BeamableNotifications.getLaunchNotification().then((n) => {
      if (active) setLaunch(n);
    });
    return () => {
      active = false;
    };
  }, []);

  return launch;
}

/**
 * One hook for the common push lifecycle: initializes the SDK on mount (idempotent), tracks
 * permission / token / last-opened as reactive state, and returns the Promise-returning
 * actions. Replaces the multi-effect boilerplate a screen would otherwise hand-write.
 *
 * `isSupported` is sourced from `addSupportListener`, so it's correct on both platforms:
 * static `true` on iOS/Android (fires once), and DYNAMIC on web (flips `true` once a host —
 * e.g. a Unity WebView — reports native support).
 *
 * @example
 * const push = BeamPushNotifications();
 * // await push.requestPermission(); await push.registerForRemote();
 * // push.token, push.permission?.granted, push.lastOpened
 */
export function BeamPushNotifications(): BeamNotificationsState {
  const [isSupported, setIsSupported] = useState(
    BeamableNotifications.isSupported,
  );
  const [permission, setPermission] = useState<PermissionResult | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [lastOpened, setLastOpened] = useState<NotificationData | null>(null);

  useEffect(() => {
    const sub = BeamableNotifications.addSupportListener(setIsSupported);
    return () => sub.remove();
  }, []);
  useEffect(() => {
    BeamableNotifications.initialize(); // self-gates per platform
  }, []);

  BeamNotificationEvent('permissionResult', setPermission);
  BeamNotificationEvent('tokenReceived', ({ token }) => setToken(token));
  BeamNotificationEvent('notificationOpened', setLastOpened);

  const requestPermission = useCallback(
    (options?: PermissionOptions) =>
      BeamableNotifications.requestPermission(options),
    [],
  );
  const registerForRemote = useCallback(
    (options?: { timeoutMs?: number }) =>
      BeamableNotifications.registerForRemote(options),
    [],
  );

  return {
    isSupported,
    permission,
    token,
    lastOpened,
    requestPermission,
    registerForRemote,
  };
}
