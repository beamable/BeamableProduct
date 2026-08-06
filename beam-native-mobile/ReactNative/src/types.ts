/**
 * Shared, platform-agnostic declarations for `@beamable/notifications-react-native`.
 *
 * These live in their own module (no `NativeModules` / `window` imports) so BOTH platform
 * entry points — the native `index.ts` and the web `index.web.ts` — can import them without
 * pulling in the other platform's runtime. Consumers still import every one of these from the
 * package root (`index.ts` / `index.web.ts` re-export `* from './types'`).
 */

// ---------------------------------------------------------------------------
// Notification / permission / scheduling shapes (shared by iOS, Android, web).
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
// Notification Intent Data schema (shared by Android, iOS and the engines).
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
  // Campaign intent-data (all optional; present only for tracked campaigns).
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
// Events — unified vocabulary.
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
  /**
   * Result of a native analytics funnel send (Received/Opened/Sent/Clicked/Converted).
   * Android emits the native `onFunnelResult` event; iOS is a follow-up (inert for now).
   */
  funnelResult: {
    funnelType: string;
    ok: boolean;
    statusCode: number;
    message: string;
  };
  /**
   * iOS 17.2+ Live Activity **push-to-start** token for an attributes type
   * (`activityType` ∈ 'actions' | 'animated' | 'countdown'). Forward it to the push rail so the
   * backend can START a Live Activity for this player via APNs. iOS-only.
   */
  liveActivityPushToStartToken: {
    activityType: string;
    /**
     * Unqualified Swift attributes type name (e.g. `BeamActionsActivityAttributes`) — the identity the
     * rail and portal key on. Emitted by the SDK since it owns the slug→type mapping, so an app no
     * longer has to keep its own copy of that map.
     */
    attributesType: string;
    token: string;
  };
  /**
   * iOS 16.1+ per-activity **update** token for a running Live Activity (`activityId`). Forward it
   * so the backend can UPDATE/END that specific activity via APNs. iOS-only.
   */
  liveActivityUpdateToken: {
    activityType: string;
    attributesType: string;
    activityId: string;
    token: string;
  };
  /** A Live Activity started (usually via a push-to-start). iOS-only. */
  liveActivityStarted: {
    activityType: string;
    attributesType: string;
    activityId: string;
  };
  /**
   * Whether this device+build can actually DRAW a Live Activity, per attributes type. Emitted at init
   * and again whenever the player toggles Live Activities in Settings. iOS-only.
   *
   * `available: false` is the cue to **withdraw** any token already registered for that type: the rail
   * chooses Live-Activity-vs-notification purely by token presence, so a stale token means the player
   * gets an activity nothing can render AND loses the notification that would have worked.
   */
  liveActivityCapability: {
    capabilities: {
      attributesType: string;
      activityType: string;
      /** iOS 17.2+, the push-to-start floor. */
      supported: boolean;
      /** The player's Settings toggle (`areActivitiesEnabled`). */
      enabled: boolean;
      /** Listed in the app's `Info.plist` under `BMNLiveActivityTypes`. */
      declared: boolean;
      /** A WidgetKit extension is embedded in this build. */
      widgetPresent: boolean;
      /** All four of the above. Only register a token when this is true. */
      available: boolean;
      /** Human-readable cause when `available` is false; empty otherwise. */
      reason: string;
    }[];
  };
};

/** Every event the SDK can emit — the runtime list matching `keyof EventMap`. */
export const BEAMABLE_EVENTS = [
  'permissionResult',
  'tokenReceived',
  'tokenError',
  'notificationPresented',
  'notificationReceived',
  'notificationOpened',
  'pendingNotifications',
  'deliveryReceipts',
  'funnelResult',
  'liveActivityPushToStartToken',
  'liveActivityUpdateToken',
  'liveActivityStarted',
  'liveActivityCapability',
] as const;

export type BeamableEvent = (typeof BEAMABLE_EVENTS)[number];

/** Handle returned by every subscription (`addListener`, `addDeepLinkListener`, …). */
export type Subscription = { remove: () => void };

// ---------------------------------------------------------------------------
// React hook state (returned by `BeamPushNotifications`).
// ---------------------------------------------------------------------------

/** Reactive push state plus Promise-returning actions, returned by `BeamPushNotifications`. */
export interface BeamNotificationsState {
  /** True on iOS/Android; on web, true once a host (e.g. a Unity WebView) reports support. */
  isSupported: boolean;
  /** Latest permission result, or `null` until permission is requested/queried. */
  permission: PermissionResult | null;
  /** Latest device push token (APNs/FCM), or `null` until `registerForRemote` yields one. */
  token: string | null;
  /** The most recently opened (tapped) notification, or `null`. */
  lastOpened: NotificationData | null;
  /** Request permission; resolves with the outcome (also updates `permission`). */
  requestPermission: (options?: PermissionOptions) => Promise<PermissionResult>;
  /** Register for remote push; resolves with the token (also updates `token`). */
  registerForRemote: (options?: { timeoutMs?: number }) => Promise<{ token: string }>;
}

// ---------------------------------------------------------------------------
// Web transport (web build). The web build routes façade calls to a native host over a
// transport. The default transport is the bundled gree/unity-webview bridge; consumers on a
// different host can supply their own via `BeamNotifications.setWebTransport(...)`.
// ---------------------------------------------------------------------------

/** What a web host reports about itself (e.g. the Unity WebView handshake). */
export interface WebHostInfo {
  /** e.g. 'ios', 'android', 'osx-editor', 'windows-editor'. */
  os: string;
  isEditor: boolean;
  /** True when the host's native Beamable notifications are functional. */
  nativeSupported: boolean;
}

/**
 * Pluggable transport the web build uses to reach a native host. The bundled default is the
 * gree/unity-webview bridge (`./unity/unityBridge`); override with
 * `BeamNotifications.setWebTransport(...)` for other hosts.
 */
export interface WebTransport {
  /** True when the transport can currently reach a native host. */
  isSupported(): boolean;
  /** Current host info, or `null` before a handshake completes. */
  getHost(): WebHostInfo | null;
  /** Subscribe to support changes. Fires immediately with the current value. */
  addSupportListener(listener: (supported: boolean) => void): Subscription;
  /** Fire-and-forget a native method call. */
  call(method: string, args?: unknown): void;
  /** Native method call with a reply. */
  request<T>(method: string, args?: unknown, timeoutMs?: number): Promise<T>;
  /** Subscribe to a named native SDK event forwarded by the host. */
  addEventListener(name: string, listener: (payload: unknown) => void): Subscription;
}
