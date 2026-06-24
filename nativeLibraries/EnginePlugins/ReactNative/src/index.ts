/**
 * Beamable Notifications — unified React Native package
 * (`@beamable/notifications-react-native`).
 *
 * This is the SINGLE cross-platform TypeScript façade for both native sides, replacing the
 * previously separate `beamable-notifications-android` and `beamable-notifications-ios`
 * packages. Autolinking wires the correct native code per platform:
 *   - iOS    → one RCTEventEmitter module `BeamableNotificationsModule`
 *              (ios/BeamableNotificationsModule.swift/.m), which talks to the Swift core
 *              vendored as `BeamableNotifications.xcframework` (see the podspec).
 *   - Android → two modules shipped in `beamable-notifications-release.aar`:
 *              `BeamablePush` (com.beamable.push.react.ReactPushModule) and
 *              `BeamableDeeplink` (com.beamable.deeplink.react.ReactDeepLinkModule),
 *              aggregated by com.beamable.reactnative.BeamableNotificationsPackage.
 *
 * §3.1 NAME PARITY — the public/bridge-facing event vocabulary is unified here at the TS
 * layer (native @ReactMethod / RCT_EXTERN_METHOD / @objc names are left unchanged — they are
 * an ABI). Canonical event names exposed to JS:
 *   - `notificationPresented` — foreground delivery.
 *       iOS native event `notificationPresented`; Android native event `onMessageForeground`.
 *   - `notificationOpened`    — the user tapped/opened the notification.
 *       iOS native event `notificationTapped`; Android native event `onNotificationOpened`.
 *       (The iOS-native `notificationTapped` event is mapped to `notificationOpened` at this
 *       TS layer; `notificationTapped` is not a public unified event name.)
 *   - `notificationReceived`, `permissionResult`, `tokenReceived`, `tokenError`,
 *     `pendingNotifications`, `deliveryReceipts` — unchanged across platforms.
 * Deeplink canonical key is `deeplink`; reads stay tolerant (`deeplink`/`deepLink`/`deep_link`).
 */
import { NativeEventEmitter, NativeModules, Platform } from 'react-native';

const IS_IOS = Platform.OS === 'ios';

const LINKING_ERROR =
  `The package '@beamable/notifications-react-native' doesn't seem to be linked. Make sure:\n` +
  Platform.select({ ios: "- You have run 'pod install'\n", default: '' }) +
  '- You rebuilt the app after installing the package.\n' +
  '- It is not being used in Expo Go (it requires a custom dev build).';

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

// iOS: one combined module. Android: a push module + a deeplink module.
const IosNative = IS_IOS ? requireModule('BeamableNotificationsModule') : null;
const BeamablePush = IS_IOS ? null : requireModule('BeamablePush');
const BeamableDeeplink = IS_IOS ? null : requireModule('BeamableDeeplink');

// ---------------------------------------------------------------------------
// Types — shared by both platforms.
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

/**
 * Player auth written into native shared storage so the CLOSED-APP analytics funnel can
 * authenticate when the JS runtime is not running. Canonical camelCase contract the natives
 * expect; passed to the native bridge as a single JSON string.
 */
export interface ConfigureAuthOptions {
  accessToken: string;
  refreshToken: string;
  /** Absolute expiry, epoch MILLISECONDS. */
  accessTokenExpiresAt: number;
  cid: string;
  pid: string;
  /** Beamable API base URL. */
  host: string;
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

// ---------------------------------------------------------------------------
// §3.3 — Notification Intent Data schema (shared by Android, iOS and the engines).
// Free-form fields (`offers[].customData`, `campaignData`) are typed `T` only at this
// layer; on the wire they travel stringified inside a flat string→string map (Decision Q3).
// ---------------------------------------------------------------------------

export interface NotificationOffer<TCustom = Record<string, unknown>> {
  itemId?: string;
  value?: string | number;
  customData?: TCustom;
}

export interface NotificationIntentData<
  TCampaign = Record<string, unknown>,
  TOfferCustom = Record<string, unknown>,
> {
  campaignId?: string;
  nodeId?: string;
  gamerTag?: string;
  accountId?: string;
  cidPid?: string;
  offers?: NotificationOffer<TOfferCustom>[];
  campaignData?: TCampaign;
  /** Raw deeplink — intentionally schema-less, passed through verbatim. */
  deeplink?: string;
}

export interface NotificationData {
  id: string;
  title?: string;
  body?: string;
  subtitle?: string;
  /** Canonical deeplink (read tolerantly from `deeplink`/`deepLink`/`deep_link`). */
  deeplink?: string;
  actionId?: string;
  wasLaunch?: boolean;
  // §3.3 campaign intent-data (all optional; present only for tracked campaigns).
  campaignId?: string;
  nodeId?: string;
  gamerTag?: string;
  accountId?: string;
  cidPid?: string;
  offers?: NotificationOffer[];
  campaignData?: Record<string, unknown>;
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
// Events — unified vocabulary (§3.1).
// ---------------------------------------------------------------------------

export type EventMap = {
  permissionResult: PermissionResult;
  tokenReceived: { token: string };
  tokenError: { error: string };
  /** Foreground delivery. */
  notificationPresented: NotificationData;
  notificationReceived: NotificationData;
  /** User tapped/opened the notification. Canonical name (was `notificationTapped`). */
  notificationOpened: NotificationData;
  pendingNotifications: NotificationData[];
  deliveryReceipts: DeliveryReceipt[];
};

const DEFAULT_CHANNEL = 'deeplink_channel';

// ---------------------------------------------------------------------------
// Normalization helpers (Android raw JSON → cross-platform NotificationData).
// ---------------------------------------------------------------------------

/** Parse a possibly-stringified JSON value into an object, tolerantly. */
function asObject(json: unknown): Record<string, unknown> {
  if (typeof json === 'string') {
    try {
      return (JSON.parse(json) as Record<string, unknown>) ?? {};
    } catch {
      return {};
    }
  }
  if (json && typeof json === 'object') return json as Record<string, unknown>;
  return {};
}

/** Un-stringify a §3.3 nested field (offers/campaignData arrive as JSON strings). */
function parseNested<T>(value: unknown): T | undefined {
  if (value == null) return undefined;
  if (typeof value === 'string') {
    try {
      return JSON.parse(value) as T;
    } catch {
      return undefined;
    }
  }
  return value as T;
}

/** Parse the flat Android message/launch JSON into the cross-platform NotificationData. */
function toNotificationData(json: unknown, wasLaunch = false): NotificationData {
  const o = asObject(json);
  const ui = (o.userInfo as Record<string, unknown> | undefined) ?? undefined;
  const rawDeep =
    (o.deeplink as string) ??
    (o.deepLink as string) ??
    (o.deep_link as string) ??
    (ui?.deeplink as string) ??
    (ui?.deepLink as string);
  const deeplink =
    typeof rawDeep === 'string' && rawDeep.length > 0 ? rawDeep : undefined;
  const id = (o.id as string) ?? (o.messageId as string) ?? '';
  return {
    id: String(id),
    title: o.title as string | undefined,
    body: o.body as string | undefined,
    subtitle: o.subtitle as string | undefined,
    deeplink,
    wasLaunch,
    campaignId: o.campaignId as string | undefined,
    nodeId: o.nodeId as string | undefined,
    gamerTag: o.gamerTag as string | undefined,
    accountId: o.accountId as string | undefined,
    cidPid: o.cidPid as string | undefined,
    offers: parseNested<NotificationOffer[]>(o.offers),
    campaignData: parseNested<Record<string, unknown>>(o.campaignData),
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
  if (
    trigger.type === 'timeInterval' &&
    trigger.seconds &&
    trigger.seconds > 0
  ) {
    return Math.round(trigger.seconds * 1000);
  }
  return 0;
}

/** Android's NotificationTemplate.dataPayload is Map<String,String>; stringify values. */
function toStringMap(obj?: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {};
  if (!obj) return out;
  for (const [k, v] of Object.entries(obj)) {
    if (v == null) continue;
    out[k] = typeof v === 'string' ? v : JSON.stringify(v);
  }
  return out;
}

// ---------------------------------------------------------------------------
// addListener — unified event names, dispatched onto each platform's native events.
// ---------------------------------------------------------------------------

type Subscription = { remove: () => void };

// Emitters are constructed LAZILY (on first use), never at module load. On some RN versions
// `new NativeEventEmitter(mod)` dereferences `mod` in its constructor; since the unlinked
// native modules are `LINKING_ERROR`-throwing Proxies, building them eagerly at import would
// throw during bundle evaluation (a hard app-start crash) instead of deferring the friendly
// linking error to first actual use. Memoize so behavior is otherwise identical to before.
let iosEmitterInstance: NativeEventEmitter | null | undefined;
let pushEmitterInstance: NativeEventEmitter | null | undefined;
let deepLinkEmitterInstance: NativeEventEmitter | null | undefined;

function getIosEmitter(): NativeEventEmitter | null {
  if (iosEmitterInstance === undefined) {
    iosEmitterInstance = IS_IOS ? new NativeEventEmitter(IosNative) : null;
  }
  return iosEmitterInstance;
}

function getPushEmitter(): NativeEventEmitter | null {
  if (pushEmitterInstance === undefined) {
    pushEmitterInstance = IS_IOS ? null : new NativeEventEmitter(BeamablePush);
  }
  return pushEmitterInstance;
}

function getDeepLinkEmitter(): NativeEventEmitter | null {
  if (deepLinkEmitterInstance === undefined) {
    deepLinkEmitterInstance = IS_IOS
      ? null
      : new NativeEventEmitter(BeamableDeeplink);
  }
  return deepLinkEmitterInstance;
}

export function addListener<K extends keyof EventMap>(
  event: K,
  handler: (payload: EventMap[K]) => void,
): Subscription {
  if (IS_IOS) {
    // iOS native already emits the unified vocabulary, except `notificationOpened`,
    // whose native event name is `notificationTapped`.
    const nativeName =
      event === 'notificationOpened' ? 'notificationTapped' : (event as string);
    return getIosEmitter()!.addListener(
      nativeName,
      handler as (p: unknown) => void,
    );
  }

  // Android: map unified names onto the lower-level bridge events + normalize payloads.
  const on = (name: string, map: (raw: any) => unknown) =>
    getPushEmitter()!.addListener(name, (raw: unknown) =>
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
    case 'notificationPresented':
      // Foreground delivery. iOS emits BOTH `notificationPresented` (will-present) and
      // `notificationReceived` as genuinely distinct native events. Android has a SINGLE
      // native foreground event (`onMessageForeground`), so we bind it to exactly ONE
      // unified event — the canonical `notificationPresented` — to keep parity with the
      // cross-platform name chosen in §3.1.
      return on('onMessageForeground', (json: string) =>
        toNotificationData(json),
      );
    case 'notificationReceived':
      // Android has no separate "received" native event distinct from foreground delivery;
      // `onMessageForeground` is already surfaced as `notificationPresented` above. Binding
      // `notificationReceived` to the same native event too would fire foreground handlers
      // TWICE per delivery. Return an inert subscription so subscribing to both unified
      // events on Android invokes a foreground handler at most once. On iOS the two remain
      // distinct (handled in the IS_IOS branch above).
      return { remove: () => {} };
    case 'notificationOpened':
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
 * Beamable deeplink module. iOS routes deep links via notification payloads; the listener
 * is an inert no-op there so the shared façade stays identical.
 */
export function addDeepLinkListener(
  handler: (event: DeepLinkEvent) => void,
): Subscription {
  if (IS_IOS) return { remove: () => {} };
  return getDeepLinkEmitter()!.addListener('onDeepLink', (e: DeepLinkEvent) =>
    handler(e),
  );
}

// ---------------------------------------------------------------------------
// Offer tracking (§4.7) — unified TS API over the two native bridge shapes.
//
// iOS native takes ONE OfferTrackRequest JSON (campaign fields + the single offer
// flattened together). Android native takes TWO JSON strings: the notification's
// intent-data JSON and the single offer JSON. We accept the intent context + offer at
// the TS layer and adapt to each native signature.
// ---------------------------------------------------------------------------

function buildIosOfferRequest(
  intent: NotificationIntentData,
  offer?: NotificationOffer,
): string {
  return JSON.stringify({
    campaignId: intent.campaignId ?? '',
    nodeId: intent.nodeId ?? '',
    gamerTag: intent.gamerTag,
    accountId: intent.accountId,
    cidPid: intent.cidPid,
    deeplink: intent.deeplink,
    offer,
  });
}

function trackOffer(
  kind: 'clicked' | 'converted',
  intent: NotificationIntentData,
  offer?: NotificationOffer,
): void {
  if (IS_IOS) {
    const requestJson = buildIosOfferRequest(intent, offer);
    if (kind === 'clicked') IosNative.trackOfferClicked(requestJson);
    else IosNative.trackOfferConverted(requestJson);
    return;
  }
  // Android: PushManager.trackOfferClicked(intentDataJson, offerJson)
  const intentJson = JSON.stringify(intent);
  const offerJson = offer ? JSON.stringify(offer) : null;
  if (kind === 'clicked') BeamablePush.trackOfferClicked(intentJson, offerJson);
  else BeamablePush.trackOfferConverted(intentJson, offerJson);
}

// ---------------------------------------------------------------------------
// API — unified across platforms. iOS-only features are no-ops on Android.
// ---------------------------------------------------------------------------

export const BeamableNotifications = {
  /** Initialize push + deeplink. Call once at app start. */
  initialize(): void {
    if (IS_IOS) {
      IosNative.initialize();
      return;
    }
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
  requestPermission(options: PermissionOptions = {}): void {
    if (IS_IOS) {
      IosNative.requestPermission(options);
      return;
    }
    BeamablePush.requestPermission();
  },
  getPermissionStatus(): void {
    if (IS_IOS) IosNative.getPermissionStatus();
    // Android: no bridged status query; result arrives via requestPermission.
  },

  // Local notifications
  scheduleLocal(request: LocalRequest): void {
    if (IS_IOS) {
      IosNative.scheduleLocal(request);
      return;
    }
    const deepLink =
      (request.userInfo?.deeplink as string) ??
      (request.userInfo?.deepLink as string) ??
      undefined;
    const template = {
      id: stableIntId(request.id),
      title: request.title ?? '',
      body: request.body ?? '',
      channelId: DEFAULT_CHANNEL,
      deepLinkUrl: deepLink ?? null,
      dataPayload: toStringMap(request.userInfo),
    };
    Promise.resolve(
      BeamablePush.scheduleLocal(
        JSON.stringify(template),
        triggerToDelayMillis(request.trigger),
      ),
    ).catch(() => {});
  },
  cancelLocal(id: string): void {
    if (IS_IOS) {
      IosNative.cancelLocal(id);
      return;
    }
    BeamablePush.cancel(stableIntId(id));
  },
  cancelAllLocal(): void {
    if (IS_IOS) {
      IosNative.cancelAllLocal();
      return;
    }
    BeamablePush.cancelAll();
  },
  getPending(): void {
    if (IS_IOS) IosNative.getPending();
    // Not supported on Android.
  },

  // Remote (FCM/APNs).
  registerForRemote(): void {
    if (IS_IOS) {
      IosNative.registerForRemote();
      return;
    }
    BeamablePush.fetchToken();
  },
  unregisterForRemote(): void {
    if (IS_IOS) IosNative.unregisterForRemote();
    // No-op on Android (FCM tokens are not explicitly unregistered here).
  },

  // Templates / categories / analytics — iOS only; no-ops on Android.
  registerTemplate(template: TemplateSpec): void {
    if (IS_IOS) IosNative.registerTemplate(template);
  },
  registerCategory(category: CategorySpec): void {
    if (IS_IOS) IosNative.registerCategory(category);
  },

  /**
   * Write the player's Beamable tokens into native shared storage so the closed-app
   * analytics funnel can authenticate while the JS runtime is not running. Pass the absolute
   * expiry as epoch MILLISECONDS. The auth object is JSON-stringified and handed to the
   * native bridge's `configureAuth` (same camelCase contract on both platforms). Best-effort:
   * a missing native method is a no-op.
   */
  configureAuth(auth: ConfigureAuthOptions): void {
    const json = JSON.stringify(auth);
    const mod = IS_IOS ? IosNative : BeamablePush;
    if (mod && typeof mod.configureAuth === 'function') mod.configureAuth(json);
  },
  /** Clear the player auth previously written via {@link configureAuth}. */
  clearAuth(): void {
    const mod = IS_IOS ? IosNative : BeamablePush;
    if (mod && typeof mod.clearAuth === 'function') mod.clearAuth();
  },
  getDeliveryReceipts(): void {
    if (IS_IOS) IosNative.getDeliveryReceipts();
  },
  setBadge(count: number): void {
    if (IS_IOS) IosNative.setBadge(count);
  },
  clearDelivered(): void {
    if (IS_IOS) IosNative.clearDelivered();
  },

  /**
   * Cold-start "get launch notification": resolves the payload (with `deeplink`) if the app
   * was launched by tapping a notification, else null. Warm-start taps arrive via the
   * `notificationOpened` event instead.
   */
  getLaunchNotification(): Promise<NotificationData | null> {
    if (IS_IOS) {
      return Promise.resolve(IosNative.getLaunchNotification()).then(
        (data: unknown) =>
          data
            ? toNotificationData(data as Record<string, unknown>, true)
            : null,
      );
    }
    if (typeof BeamablePush.getLaunchNotification === 'function') {
      return Promise.resolve(BeamablePush.getLaunchNotification()).then(
        (json: string | null) =>
          json ? toNotificationData(json, true) : null,
      );
    }
    return Promise.resolve(null);
  },

  /**
   * §4.7 — record that the user clicked an offer from a campaign in-app, attributed to the
   * originating notification's intent data. Emits a `Clicked` funnel event natively.
   */
  trackOfferClicked(
    intent: NotificationIntentData,
    offer?: NotificationOffer,
  ): void {
    trackOffer('clicked', intent, offer);
  },
  /** §4.7 — record that an offer click converted. Emits a `Converted` funnel event. */
  trackOfferConverted(
    intent: NotificationIntentData,
    offer?: NotificationOffer,
  ): void {
    trackOffer('converted', intent, offer);
  },

  addListener,
  addDeepLinkListener,
};

export default BeamableNotifications;

// ---------------------------------------------------------------------------
// Generic, app-agnostic extras (relocated from the RN sample, §6).
// ---------------------------------------------------------------------------

// AsyncStorage-backed `TokenStorage` for the Beamable Web SDK (generic RN session
// persistence). Requires the `@beamable/sdk` and `@react-native-async-storage/async-storage`
// peer dependencies.
export { RNTokenStorage } from './RNTokenStorage';

// Device push-token helpers over a `PushNotificationService`-style microservice client.
export {
  createPushDevice,
  DEVICE_PLATFORM,
  DEFAULT_APNS_ENVIRONMENT,
} from './pushDevice';
export type {
  PushPlatform,
  ApnsEnvironment,
  PushDevice,
  PushDeviceOptions,
  PushDeviceServiceClient,
} from './pushDevice';

// Convenience wrappers + platform gate over the unified façade.
export {
  isBeamableNotificationsSupported,
  BEAMABLE_EVENTS,
  addBeamableListener,
  addBeamableDeepLinkListener,
  initBeamableNotifications,
  requestBeamablePermission,
  getPermissionStatus,
  scheduleLocal,
  cancelLocal,
  cancelAllLocal,
  getPending,
  registerForRemote,
  unregisterForRemote,
  getDeliveryReceipts,
  configureAuth,
  clearAuth,
  setBadge,
  clearDelivered,
  getLaunchNotification,
} from './helpers';
export type { BeamableEvent } from './helpers';
