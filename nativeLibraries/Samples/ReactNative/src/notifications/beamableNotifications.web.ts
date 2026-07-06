/**
 * Web build of the app's notifications entry (Metro resolves `.web.ts` for web).
 *
 * Exposes the SAME `BeamNotifications` object shape as the package, but every native
 * call is routed over the Unity bridge (src/unity/unityBridge.ts). When the web build
 * runs inside a **Unity WebView** whose host has `com.beamable.notifications` installed,
 * calls reach the real iOS/Android library; in a plain browser they are inert.
 *
 * Support is DYNAMIC on web: false at startup, true once the Unity handshake reports a
 * native-capable host. Screens read `BeamNotifications.isSupported` and subscribe via
 * `BeamNotifications.addSupportListener(...)`, exactly like on native.
 */
import {
  BEAMABLE_EVENTS,
  deepLinkFromNotification,
  campaignCoordsFromNotification,
} from '@beamable/notifications-react-native';
import type {
  BeamableEvent,
  ConfigureAuthOptions,
  LocalRequest,
  NotificationData,
  NotificationIntentData,
  NotificationOffer,
  PushPlatform,
} from '@beamable/notifications-react-native';
import {
  addUnityEventListener,
  addUnityPlatformListener,
  callUnity,
  getUnityHostPlatform,
  requestUnity,
} from '../unity/unityBridge';

export type {
  BeamableEvent,
  NotificationData,
  NotificationIntentData,
  NotificationOffer,
} from '@beamable/notifications-react-native';

// Live support flag, flipped by the Unity platform handshake.
let supported = false;
const supportListeners = new Set<(supported: boolean) => void>();
addUnityPlatformListener((platform) => {
  if (platform.nativeSupported !== supported) {
    supported = platform.nativeSupported;
    supportListeners.forEach((l) => l(supported));
  }
});

/** Unity-routed `BeamNotifications` — same surface the package exposes on native. */
export const BeamNotifications = {
  get isSupported(): boolean {
    return supported;
  },
  events: BEAMABLE_EVENTS,
  /** Fires immediately with the current value, and again when the Unity handshake lands. */
  addSupportListener(listener: (s: boolean) => void): { remove: () => void } {
    supportListeners.add(listener);
    listener(supported);
    return { remove: () => supportListeners.delete(listener) };
  },
  hostPlatformLabel(): string {
    const host = getUnityHostPlatform();
    if (!host) return 'web';
    const os = host.os === 'ios' ? 'iOS' : host.os === 'android' ? 'Android' : host.os;
    return `${os} (Unity host)`;
  },
  devicePushPlatform(): PushPlatform {
    return getUnityHostPlatform()?.os === 'ios' ? 'apns' : 'fcm';
  },

  // Pure parsers — reused from the package (no native code).
  deepLinkFromNotification,
  campaignCoordsFromNotification,

  addListener(event: BeamableEvent, handler: (payload: never) => void): { remove: () => void } {
    return addUnityEventListener(event, handler as (p: unknown) => void);
  },
  /** Raw URL-scheme deep links are Android-native only — inert stub on web. */
  addDeepLinkListener(
    _handler: (event: { url: string; isColdStart: boolean }) => void,
  ): { remove: () => void } {
    return { remove: () => {} };
  },

  initialize(): void {
    callUnity('initialize');
  },
  requestPermission(options: { alert?: boolean; badge?: boolean; sound?: boolean } = {
    alert: true,
    badge: true,
    sound: true,
  }): void {
    callUnity('requestPermission', options);
  },
  getPermissionStatus(): void {
    callUnity('getPermissionStatus');
  },
  scheduleLocal(request: LocalRequest): void {
    callUnity('scheduleLocal', request);
  },
  scheduleLocalWithDeepLink(opts: {
    id: string;
    title: string;
    body: string;
    url: string;
    seconds?: number;
  }): void {
    callUnity('scheduleLocal', {
      id: opts.id,
      title: opts.title,
      body: opts.body,
      trigger:
        opts.seconds && opts.seconds > 0
          ? { type: 'timeInterval', seconds: opts.seconds }
          : { type: 'immediate' },
      userInfo: { deepLink: opts.url },
    } as LocalRequest);
  },
  cancelLocal(id: string): void {
    callUnity('cancelLocal', { id });
  },
  cancelAllLocal(): void {
    callUnity('cancelAllLocal');
  },
  getPending(): void {
    callUnity('getPending');
  },
  registerForRemote(): void {
    callUnity('registerForRemote');
  },
  unregisterForRemote(): void {
    callUnity('unregisterForRemote');
  },
  getDeliveryReceipts(): void {
    callUnity('getDeliveryReceipts');
  },
  setBadge(count: number): void {
    callUnity('setBadge', { count });
  },
  clearDelivered(): void {
    callUnity('clearDelivered');
  },
  async getLaunchNotification(): Promise<NotificationData | null> {
    if (!supported) return null;
    try {
      return await requestUnity<NotificationData | null>('getLaunchNotification');
    } catch {
      return null;
    }
  },
  trackOfferClicked(intent: NotificationIntentData, offer?: NotificationOffer): void {
    callUnity('trackOfferClicked', { intent, offer });
  },
  trackOfferConverted(intent: NotificationIntentData, offer?: NotificationOffer): void {
    callUnity('trackOfferConverted', { intent, offer });
  },
  configureAuth(options: ConfigureAuthOptions): void {
    callUnity('configureAuth', options);
  },
  clearAuth(): void {
    callUnity('clearAuth');
  },
};
