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
import { DEVICE_PLATFORM } from './pushDevice';
import type { PushPlatform } from './pushDevice';
import {
  deepLinkFromNotification,
  campaignCoordsFromNotification,
} from './parsing';
import { BEAMABLE_EVENTS } from './types';
import type {
  CategorySpec,
  ConfigureAuthOptions,
  DeepLinkEvent,
  DeliveryReceipt,
  EventMap,
  LocalRequest,
  NotificationData,
  NotificationIntentData,
  NotificationOffer,
  PermissionOptions,
  PermissionResult,
  Subscription,
  TemplateSpec,
  TriggerSpec,
  WebTransport,
} from './types';

// Re-export the whole shared surface so consumers keep importing types + BEAMABLE_EVENTS
// from the package root. (`index.web.ts` re-exports the same, so the surface is identical
// on every platform even though only `index.ts`'s declarations back the published `types`.)
export * from './types';

const IS_IOS = Platform.OS === 'ios';
const IS_ANDROID = Platform.OS === 'android';

/**
 * True on iOS/Android (where a native module exists); false on web / unsupported.
 * Every `BeamNotifications` method is gated on this, so calling the façade on an
 * unsupported platform is a safe no-op.
 */
export const isBeamableNotificationsSupported = IS_IOS || IS_ANDROID;

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

// All shared type declarations + the `BEAMABLE_EVENTS` constant now live in `./types`
// (imported above, re-exported via `export * from './types'`).

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
  if (!isBeamableNotificationsSupported) return { remove: () => {} };
  if (IS_IOS) {
    // iOS funnel-result is a follow-up: the native side doesn't emit `onFunnelResult` yet,
    // so return an inert subscription rather than binding to a non-existent native event.
    if (event === 'funnelResult') return { remove: () => {} };
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
    case 'funnelResult':
      // Android emits the native `onFunnelResult` event with the funnel-send outcome.
      return on('onFunnelResult', (raw: any) => {
        const o = asObject(raw);
        return {
          funnelType: String(o.funnelType ?? ''),
          ok: Boolean(o.ok),
          statusCode: Number(o.statusCode ?? 0),
          message: String(o.message ?? ''),
        };
      });
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
  if (!isBeamableNotificationsSupported) return { remove: () => {} };
  if (IS_IOS) return { remove: () => {} };
  return getDeepLinkEmitter()!.addListener('onDeepLink', (e: DeepLinkEvent) =>
    handler(e),
  );
}

// ---------------------------------------------------------------------------
// Promise adapter for solicited calls (P0 ergonomics).
//
// Historically every solicited action (requestPermission, registerForRemote, getPending,
// …) was fire-and-forget and the caller had to pre-subscribe to a separate event and
// correlate the result by hand. `awaitEvent` wraps that pattern: subscribe to the result
// event FIRST, fire the native call, then resolve with the next matching payload. The
// event still fires for any standalone `addListener` subscriber, so this is purely
// additive — unsolicited pushes keep arriving on the events as before.
// ---------------------------------------------------------------------------

function awaitEvent<K extends keyof EventMap>(
  event: K,
  fire: () => void,
  opts: { rejectOn?: keyof EventMap; timeoutMs?: number } = {},
): Promise<EventMap[K]> {
  return new Promise<EventMap[K]>((resolve, reject) => {
    let settled = false;
    const subs: Subscription[] = [];
    let timer: ReturnType<typeof setTimeout> | undefined;
    const cleanup = () => {
      subs.forEach((s) => s.remove());
      if (timer !== undefined) clearTimeout(timer);
    };
    // Subscribe BEFORE firing so a fast synchronous native emit is never missed.
    subs.push(
      addListener(event, (payload) => {
        if (settled) return;
        settled = true;
        cleanup();
        resolve(payload);
      }),
    );
    if (opts.rejectOn) {
      subs.push(
        addListener(opts.rejectOn, (payload) => {
          if (settled) return;
          settled = true;
          cleanup();
          const message =
            (payload as { error?: string })?.error ??
            `Received '${String(opts.rejectOn)}'`;
          reject(new Error(String(message)));
        }),
      );
    }
    if (opts.timeoutMs && opts.timeoutMs > 0) {
      timer = setTimeout(() => {
        if (settled) return;
        settled = true;
        cleanup();
        reject(
          new Error(
            `Timed out after ${opts.timeoutMs}ms waiting for '${String(event)}'.`,
          ),
        );
      }, opts.timeoutMs);
    }
    try {
      fire();
    } catch (error) {
      if (settled) return;
      settled = true;
      cleanup();
      reject(error instanceof Error ? error : new Error(String(error)));
    }
  });
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
  // ── Support / platform info (safe on every platform) ─────────────────────
  /** True on iOS/Android; false on web / unsupported. */
  get isSupported(): boolean {
    return isBeamableNotificationsSupported;
  },
  /** Every event name the SDK can emit (subscribe via `addListener`). */
  events: BEAMABLE_EVENTS,
  /**
   * Subscribe to native-support changes. On iOS/Android support is static, so the
   * listener fires once with the current value and never again; returned handle's
   * `remove()` is a no-op. (A reactive shape screens can use uniformly.)
   */
  addSupportListener(
    listener: (supported: boolean) => void,
  ): { remove: () => void } {
    listener(isBeamableNotificationsSupported);
    return { remove: () => {} };
  },
  /** Human label for where the native library runs ('iOS' / 'Android'). */
  hostPlatformLabel(): string {
    return IS_IOS ? 'iOS' : 'Android';
  },
  /** Which push provider this device's token belongs to ('apns' | 'fcm'). */
  devicePushPlatform(): PushPlatform {
    return DEVICE_PLATFORM;
  },

  // ── Pure payload parsers (safe on every platform) ────────────────────────
  deepLinkFromNotification,
  campaignCoordsFromNotification,

  /** Initialize push + deeplink. Call once at app start. */
  initialize(): void {
    if (!isBeamableNotificationsSupported) return;
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
  /**
   * Request notification permission and RESOLVE with the outcome — no need to pre-subscribe
   * to `permissionResult` (that event still fires too). No timeout: the OS dialog waits on
   * the user. Resolves `{ granted: false }` on unsupported platforms.
   */
  requestPermission(
    options: PermissionOptions = { alert: true, badge: true, sound: true },
  ): Promise<PermissionResult> {
    if (!isBeamableNotificationsSupported) {
      return Promise.resolve({ status: 'denied', granted: false });
    }
    return awaitEvent('permissionResult', () => {
      if (IS_IOS) IosNative.requestPermission(options);
      else BeamablePush.requestPermission();
    });
  },
  /**
   * Read the current permission status. Resolves with the status (iOS). Android has no
   * bridged status query — the status only arrives via {@link requestPermission} — so this
   * resolves `{ status: 'notDetermined' }` there.
   */
  getPermissionStatus(): Promise<PermissionResult> {
    if (IS_IOS) {
      return awaitEvent(
        'permissionResult',
        () => IosNative.getPermissionStatus(),
        { timeoutMs: 5000 },
      );
    }
    return Promise.resolve({ status: 'notDetermined', granted: false });
  },

  // Local notifications
  scheduleLocal(request: LocalRequest): void {
    if (!isBeamableNotificationsSupported) return;
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
    if (!isBeamableNotificationsSupported) return;
    if (IS_IOS) {
      IosNative.cancelLocal(id);
      return;
    }
    BeamablePush.cancel(stableIntId(id));
  },
  cancelAllLocal(): void {
    if (!isBeamableNotificationsSupported) return;
    if (IS_IOS) {
      IosNative.cancelAllLocal();
      return;
    }
    BeamablePush.cancelAll();
  },
  /**
   * Resolve the pending (scheduled, not-yet-delivered) local notifications. iOS only;
   * resolves `[]` on Android. The `pendingNotifications` event still fires too (iOS).
   */
  getPending(): Promise<NotificationData[]> {
    if (IS_IOS) {
      return awaitEvent('pendingNotifications', () => IosNative.getPending(), {
        timeoutMs: 5000,
      });
    }
    return Promise.resolve([]);
  },

  // Remote (FCM/APNs).
  /**
   * Register for remote push and RESOLVE with the device token — no need to pre-subscribe
   * to `tokenReceived` (that event still fires too). Rejects on `tokenError`, and times out
   * (default 30s) because a token never arrives on a simulator/emulator or without realm
   * push credentials. Rejects on unsupported platforms.
   */
  registerForRemote(
    options: { timeoutMs?: number } = {},
  ): Promise<{ token: string }> {
    if (!isBeamableNotificationsSupported) {
      return Promise.reject(
        new Error('Remote push is not supported on this platform.'),
      );
    }
    return awaitEvent(
      'tokenReceived',
      () => {
        if (IS_IOS) IosNative.registerForRemote();
        else BeamablePush.fetchToken();
      },
      { rejectOn: 'tokenError', timeoutMs: options.timeoutMs ?? 30000 },
    );
  },
  unregisterForRemote(): void {
    if (!isBeamableNotificationsSupported) return;
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
    if (!isBeamableNotificationsSupported) return;
    const json = JSON.stringify(auth);
    const mod = IS_IOS ? IosNative : BeamablePush;
    if (mod && typeof mod.configureAuth === 'function') mod.configureAuth(json);
  },
  /** Clear the player auth previously written via {@link configureAuth}. */
  clearAuth(): void {
    if (!isBeamableNotificationsSupported) return;
    const mod = IS_IOS ? IosNative : BeamablePush;
    if (mod && typeof mod.clearAuth === 'function') mod.clearAuth();
  },
  /**
   * Resolve stored delivery receipts. iOS only; resolves `[]` on Android. The
   * `deliveryReceipts` event still fires too (iOS).
   */
  getDeliveryReceipts(): Promise<DeliveryReceipt[]> {
    if (IS_IOS) {
      return awaitEvent(
        'deliveryReceipts',
        () => IosNative.getDeliveryReceipts(),
        { timeoutMs: 5000 },
      );
    }
    return Promise.resolve([]);
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
    if (!isBeamableNotificationsSupported) return Promise.resolve(null);
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
    if (!isBeamableNotificationsSupported) return;
    trackOffer('clicked', intent, offer);
  },
  /** §4.7 — record that an offer click converted. Emits a `Converted` funnel event. */
  trackOfferConverted(
    intent: NotificationIntentData,
    offer?: NotificationOffer,
  ): void {
    if (!isBeamableNotificationsSupported) return;
    trackOffer('converted', intent, offer);
  },

  /**
   * Schedule a local notification whose tap carries a deep link. Generic: pass the
   * full URL to open (the app owns its scheme/routes) — it's stored under
   * `userInfo.deepLink`, which `deepLinkFromNotification` and the tap handler read.
   * Omit/zero `seconds` to fire immediately.
   */
  scheduleLocalWithDeepLink(opts: {
    id: string;
    title: string;
    body: string;
    url: string;
    seconds?: number;
  }): void {
    if (!isBeamableNotificationsSupported) return;
    this.scheduleLocal({
      id: opts.id,
      title: opts.title,
      body: opts.body,
      trigger:
        opts.seconds && opts.seconds > 0
          ? { type: 'timeInterval', seconds: opts.seconds }
          : { type: 'immediate' },
      userInfo: { deepLink: opts.url },
    });
  },

  /**
   * Web-only escape hatch: swap the transport the WEB build uses to reach a native host
   * (default is the bundled gree/unity-webview bridge). No-op on native iOS/Android, where
   * the real native module is used directly — present only for cross-platform parity.
   */
  setWebTransport(_transport: WebTransport | null): void {
    // intentional no-op on native
  },

  addListener,
  addDeepLinkListener,
};

export default BeamableNotifications;

/**
 * The recommended single entry point. `BeamNotifications.<method>()` covers the whole
 * notifications surface — lifecycle, permission, remote registration, scheduling, events,
 * funnel analytics, support/platform info, and the pure payload parsers — with the
 * platform gate baked in. (`BeamableNotifications` and the `default` export are aliases.)
 */
export const BeamNotifications = BeamableNotifications;

// ---------------------------------------------------------------------------
// Generic, app-agnostic extras (relocated from the RN sample, §6).
// ---------------------------------------------------------------------------
//
// NOTE: AsyncStorage-backed token storage for the Beamable Web SDK is now built
// into `@beamable/sdk` itself (its native `react-native` build target) — it is an
// SDK concern, not a notifications one, so it lives there. This package only adds
// the Metro helper, `@beamable/notifications-react-native/metro`.

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

// Pure payload parsers (also available on the BeamNotifications façade).
export { deepLinkFromNotification, campaignCoordsFromNotification } from './parsing';

// React hooks (P0 ergonomics) — the idiomatic way to consume the SDK from components.
// (`BeamNotificationsState` is re-exported via `export * from './types'` above.)
export {
  BeamNotificationEvent,
  BeamLaunchNotification,
  BeamPushNotifications,
} from './hooks';

// Web/Unity transport surface. Inert on native iOS/Android (the helpers no-op there);
// functional when the WEB build runs inside a Unity WebView. Exposed so a host app can build
// diagnostics UI or supply a custom transport to `BeamNotifications.setWebTransport(...)`.
export {
  unityTransport,
  isUnityWebView,
  sendToUnity,
  addUnityMessageListener,
  addUnityPlatformListener,
  getUnityHostPlatform,
} from './unity/unityBridge';
export type { UnityHostPlatform } from './unity/unityBridge';

// Back-compat convenience wrappers (the BeamNotifications façade is the recommended API).
// `isBeamableNotificationsSupported`, `BEAMABLE_EVENTS`, and `BeamableEvent` are defined in
// this module (above) — the façade and the flat wrappers share that single source.
export {
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
