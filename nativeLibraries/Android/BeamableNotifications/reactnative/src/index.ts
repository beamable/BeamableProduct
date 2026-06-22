/**
 * Beamable Notifications — native **Android** SDK wrapper (the
 * `beamable-notifications-android` package).
 *
 * It presents the SAME public surface as the iOS package
 * (`beamable-notifications-ios`): the same `EventMap`, the same `NotificationData`
 * shape, the same `addListener`, and a default `BeamableNotifications` object — so the
 * sample's cross-platform façade (`src/notifications/beamableNotifications.ts`) can treat
 * both platforms identically.
 *
 * Under the hood it bridges the two native modules shipped in the `.aar`:
 *   - `BeamablePush`     (com.beamable.push.react.ReactPushModule)
 *   - `BeamableDeeplink` (com.beamable.deeplink.react.ReactDeepLinkModule)
 *
 * The Android bridges emit lower-level events (`onTokenReceived`, `onMessageForeground`,
 * `onNotificationOpened`, `onPermissionResult`, …) carrying raw strings/JSON. This wrapper
 * normalizes them to the cross-platform event names + DTOs. Capabilities iOS has but
 * Android doesn't (templates, categories, NSE analytics, badge, pending list) are kept as
 * no-ops so the shared façade stays identical.
 */
import { NativeEventEmitter, NativeModules } from 'react-native';

const LINKING_ERROR =
  `The package 'beamable-notifications-android' doesn't seem to be linked. Make sure:\n` +
  '- You rebuilt the app after installing the package (npx expo run:android)\n' +
  '- This package only supports Android.';

function requireModule(name: string): any {
  const mod = (NativeModules as Record<string, unknown>)[name];
  if (mod) return mod;
  return new Proxy(
    {},
    {
      get() {
        throw new Error(LINKING_ERROR);
      },
    },
  );
}

const BeamablePush = requireModule('BeamablePush');
const BeamableDeeplink = requireModule('BeamableDeeplink');

// ---------------------------------------------------------------------------
// Types — kept structurally identical to the iOS package.
// ---------------------------------------------------------------------------

export interface PermissionOptions {
  alert?: boolean;
  badge?: boolean;
  sound?: boolean;
  provisional?: boolean;
  criticalAlert?: boolean;
  carPlay?: boolean;
}

export interface TriggerSpec {
  type: 'immediate' | 'timeInterval' | 'calendar';
  seconds?: number;
  repeats?: boolean;
  year?: number;
  month?: number;
  day?: number;
  hour?: number;
  minute?: number;
  second?: number;
  weekday?: number;
}

export interface AttachmentSpec {
  identifier?: string;
  url: string;
  typeHint?: string;
}

export interface LocalRequest {
  id: string;
  title?: string;
  body?: string;
  subtitle?: string;
  badge?: number;
  sound?: string;
  categoryId?: string;
  threadId?: string;
  interruptionLevel?: 'passive' | 'active' | 'timeSensitive' | 'critical';
  trigger?: TriggerSpec;
  attachments?: AttachmentSpec[];
  userInfo?: Record<string, unknown>;
  templateId?: string;
  templateValues?: Record<string, string>;
}

export interface TemplateSpec {
  id: string;
  titleFormat?: string;
  bodyFormat?: string;
  subtitleFormat?: string;
  sound?: string;
  categoryId?: string;
  badge?: number;
  defaultAttachments?: AttachmentSpec[];
}

export interface ActionSpec {
  id: string;
  title: string;
  foreground?: boolean;
  destructive?: boolean;
  authenticationRequired?: boolean;
}

export interface CategorySpec {
  id: string;
  actions: ActionSpec[];
  hiddenPreviewsBodyPlaceholder?: string;
}

export interface AnalyticsConfig {
  enabled: boolean;
  endpoint: string;
  headers?: Record<string, string>;
  commonParams?: Record<string, unknown>;
}

export interface PermissionResult {
  status:
    | 'notDetermined'
    | 'denied'
    | 'authorized'
    | 'provisional'
    | 'ephemeral'
    | string;
  granted: boolean;
  alert?: boolean;
  badge?: boolean;
  sound?: boolean;
}

export interface NotificationData {
  id: string;
  title?: string;
  body?: string;
  subtitle?: string;
  deepLink?: string;
  actionId?: string;
  wasLaunch?: boolean;
  userInfo?: Record<string, unknown>;
}

export interface DeliveryReceipt {
  id: string;
  timestamp: number;
  source: string;
  userInfo?: Record<string, unknown>;
}

/** Android-only: payload of the native `onDeepLink` event (URL-scheme VIEW intents). */
export interface DeepLinkEvent {
  url: string;
  isColdStart: boolean;
}

// ---------------------------------------------------------------------------
// Events — same map as iOS.
// ---------------------------------------------------------------------------

type EventMap = {
  permissionResult: PermissionResult;
  tokenReceived: { token: string };
  tokenError: { error: string };
  notificationPresented: NotificationData;
  notificationReceived: NotificationData;
  notificationTapped: NotificationData;
  pendingNotifications: NotificationData[];
  deliveryReceipts: DeliveryReceipt[];
};

const pushEmitter = new NativeEventEmitter(BeamablePush);
const deepLinkEmitter = new NativeEventEmitter(BeamableDeeplink);

/** Default channel id — matches the `.aar`'s `beamable_default_channel` string resource. */
const DEFAULT_CHANNEL = 'deeplink_channel';

// ---------------------------------------------------------------------------
// Normalization helpers
// ---------------------------------------------------------------------------

/** Parse the flat Android message/launch JSON into the cross-platform NotificationData. */
function toNotificationData(json: unknown, wasLaunch = false): NotificationData {
  let o: Record<string, unknown> = {};
  if (typeof json === 'string') {
    try {
      o = (JSON.parse(json) as Record<string, unknown>) ?? {};
    } catch {
      o = {};
    }
  } else if (json && typeof json === 'object') {
    o = json as Record<string, unknown>;
  }
  const rawDeep =
    (o.deepLink as string) ??
    (o.deeplink as string) ??
    ((o.userInfo as Record<string, unknown> | undefined)?.deepLink as string);
  const deepLink =
    typeof rawDeep === 'string' && rawDeep.length > 0 ? rawDeep : undefined;
  const id = (o.id as string) ?? (o.messageId as string) ?? '';
  return {
    id: String(id),
    title: o.title as string | undefined,
    body: o.body as string | undefined,
    subtitle: o.subtitle as string | undefined,
    deepLink,
    wasLaunch,
    userInfo: o,
  };
}

/** Stable positive 32-bit int from a string id (Android notification ids are ints). */
function stableIntId(id: string): number {
  let hash = 0;
  for (let i = 0; i < id.length; i++) {
    hash = (hash << 5) - hash + id.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

/** Convert an iOS-style trigger into an Android delay (ms). Only immediate/timeInterval. */
function triggerToDelayMillis(trigger?: TriggerSpec): number {
  if (!trigger) return 0;
  if (trigger.type === 'timeInterval' && trigger.seconds && trigger.seconds > 0) {
    return Math.round(trigger.seconds * 1000);
  }
  return 0;
}

/** Android's NotificationTemplate.dataPayload is Map<String,String>; stringify values. */
function toStringMap(
  obj?: Record<string, unknown>,
): Record<string, string> {
  const out: Record<string, string> = {};
  if (!obj) return out;
  for (const [k, v] of Object.entries(obj)) {
    if (v == null) continue;
    out[k] = typeof v === 'string' ? v : JSON.stringify(v);
  }
  return out;
}

// ---------------------------------------------------------------------------
// addListener — maps cross-platform events onto the Android bridge events.
// ---------------------------------------------------------------------------

type Subscription = { remove: () => void };

export function addListener<K extends keyof EventMap>(
  event: K,
  handler: (payload: EventMap[K]) => void,
): Subscription {
  const on = (name: string, map: (raw: any) => unknown) =>
    pushEmitter.addListener(name, (raw: unknown) =>
      handler(map(raw) as EventMap[K]),
    );

  switch (event) {
    case 'tokenReceived':
      return on('onTokenReceived', (token: string) => ({ token }));
    case 'tokenError':
      return on('onTokenRefreshError', (error: string) => ({ error }));
    case 'permissionResult':
      return on('onPermissionResult', (granted: boolean) => ({
        granted,
        status: granted ? 'authorized' : 'denied',
      }));
    case 'notificationReceived':
    case 'notificationPresented':
      // Foreground delivery — surfaced as both, mirroring iOS.
      return on('onMessageForeground', (json: string) =>
        toNotificationData(json),
      );
    case 'notificationTapped':
      return on('onNotificationOpened', (json: string) =>
        toNotificationData(json, true),
      );
    // Not supported on Android — return an inert subscription.
    case 'pendingNotifications':
    case 'deliveryReceipts':
    default:
      return { remove: () => {} };
  }
}

/**
 * Android-only: subscribe to native URL-scheme deep links (VIEW intents) captured by the
 * Beamable deeplink module. The iOS package has no equivalent (it routes deep links via
 * notification payloads); the shared façade feature-detects this.
 */
export function addDeepLinkListener(
  handler: (event: DeepLinkEvent) => void,
): Subscription {
  return deepLinkEmitter.addListener('onDeepLink', (e: DeepLinkEvent) =>
    handler(e),
  );
}

// ---------------------------------------------------------------------------
// API — same method names as iOS; iOS-only ones are no-ops.
// ---------------------------------------------------------------------------

export const BeamableNotifications = {
  /** Initialize push + deeplink, register the default channel. Call once at app start. */
  initialize(): void {
    BeamablePush.initialize(true);
    BeamablePush.registerChannel(
      DEFAULT_CHANNEL,
      'Notifications',
      'Beamable notifications',
      4, // NotificationManager.IMPORTANCE_HIGH
    );
    BeamableDeeplink.initializeDeepLinks();
  },

  // Permission
  requestPermission(_options: PermissionOptions = {}): void {
    BeamablePush.requestPermission();
  },
  getPermissionStatus(): void {
    // No bridged status query on Android; permission result arrives via requestPermission.
  },

  // Local notifications
  scheduleLocal(request: LocalRequest): void {
    const deepLink = (request.userInfo?.deepLink as string) ?? undefined;
    const template = {
      id: stableIntId(request.id),
      title: request.title ?? '',
      body: request.body ?? '',
      channelId: DEFAULT_CHANNEL,
      deepLinkUrl: deepLink ?? null,
      dataPayload: toStringMap(request.userInfo),
    };
    // Returns a Promise<int> (the notification id); fire-and-forget to match iOS.
    Promise.resolve(
      BeamablePush.scheduleLocal(
        JSON.stringify(template),
        triggerToDelayMillis(request.trigger),
      ),
    ).catch(() => {});
  },
  cancelLocal(id: string): void {
    BeamablePush.cancel(stableIntId(id));
  },
  cancelAllLocal(): void {
    BeamablePush.cancelAll();
  },
  getPending(): void {
    // Not supported on Android.
  },

  // Remote (FCM). registerForRemote → fetch the FCM token (arrives on `tokenReceived`).
  registerForRemote(): void {
    BeamablePush.fetchToken();
  },
  unregisterForRemote(): void {
    // No-op on Android (FCM tokens are not explicitly unregistered here).
  },

  // iOS-only features — no-ops on Android so the shared façade stays identical.
  registerTemplate(_template: TemplateSpec): void {},
  registerCategory(_category: CategorySpec): void {},
  configureAnalytics(_config: AnalyticsConfig): void {},
  getDeliveryReceipts(): void {},
  setBadge(_count: number): void {},
  clearDelivered(): void {},

  /**
   * Cold-start "get launch notification": if the app was launched by tapping a notification,
   * resolves its payload (with `deepLink`); otherwise null. Backed by ReactPushModule's
   * `getLaunchNotification` @ReactMethod. Warm-start taps arrive via `notificationTapped`
   * (ReactPushModule's `onNewIntent` → `onNotificationOpened`) instead.
   */
  getLaunchNotification(): Promise<NotificationData | null> {
    if (typeof BeamablePush.getLaunchNotification === 'function') {
      return Promise.resolve(BeamablePush.getLaunchNotification()).then(
        (json: string | null) => (json ? toNotificationData(json, true) : null),
      );
    }
    return Promise.resolve(null);
  },

  addListener,
  addDeepLinkListener,
};

export default BeamableNotifications;
