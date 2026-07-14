/**
 * Web build of `@beamable/notifications-react-native` (Metro resolves this `.web.ts` on the
 * `web` platform automatically — consumers just `import … from '@beamable/notifications-react-native'`
 * and get the native module on iOS/Android and this on web; no per-app file).
 *
 * It exposes the SAME public surface as the native `index.ts` (façade, hooks, helpers, types,
 * parsers) but routes every native call over a pluggable {@link WebTransport}. The bundled
 * default transport is the gree/unity-webview bridge (`./unity/unityBridge`), so a web build
 * hosted inside a Unity WebView reaches the real iOS/Android library; in a plain browser it is
 * inert (`isSupported` stays false). Override the transport for other hosts via
 * `BeamNotifications.setWebTransport(...)`.
 *
 * Support is DYNAMIC on web: false at startup, true once the transport's host handshake reports
 * native support.
 */
import { campaignCoordsFromNotification, deepLinkFromNotification } from './parsing';
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
  WebTransport,
} from './types';
import type { PushPlatform } from './pushDevice';
import { unityTransport } from './unity/unityBridge';

// Re-export the shared surface so the package root exposes types + BEAMABLE_EVENTS on web too.
export * from './types';

// Parity contract: the web façade must structurally match the native façade (they share the
// published `types`). `NativeFacade` is a type-only import — fully erased, no runtime dependency
// on the native module, and it makes any drift a compile error via the `satisfies` below.
type NativeFacade = typeof import('./index').BeamableNotifications;

// ---------------------------------------------------------------------------
// Active transport + dynamic support state.
// ---------------------------------------------------------------------------

let activeTransport: WebTransport = unityTransport;
let supported = activeTransport.isSupported();
const supportListeners = new Set<(supported: boolean) => void>();

function emitSupport(next: boolean): void {
  if (next === supported) return;
  supported = next;
  supportListeners.forEach((l) => l(next));
}

let transportSub: Subscription = activeTransport.addSupportListener(emitSupport);

/**
 * On web there is always a transport-backed implementation available (the bridge), so this is
 * `true` — it gates whether the hooks/helpers wire up their subscriptions. Whether a native
 * host is actually reachable is the DYNAMIC `BeamNotifications.isSupported` getter below.
 */
export const isBeamableNotificationsSupported = true;

// ---------------------------------------------------------------------------
// Event subscription + promise adapter over the active transport.
// ---------------------------------------------------------------------------

export function addListener<K extends keyof EventMap>(
  event: K,
  handler: (payload: EventMap[K]) => void,
): Subscription {
  return activeTransport.addEventListener(event, (payload) =>
    handler(payload as EventMap[K]),
  );
}

/** Raw URL-scheme deep links are an Android-native concept — inert on web. */
export function addDeepLinkListener(
  _handler: (event: DeepLinkEvent) => void,
): Subscription {
  return { remove: () => {} };
}

/** Web mirror of the native `awaitEvent` (P0): fire a call, resolve on the next matching event. */
function awaitWebEvent<K extends keyof EventMap>(
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
          reject(
            new Error(String((payload as { error?: string })?.error ?? 'error')),
          );
        }),
      );
    }
    if (opts.timeoutMs && opts.timeoutMs > 0) {
      timer = setTimeout(() => {
        if (settled) return;
        settled = true;
        cleanup();
        reject(new Error(`Timed out waiting for '${String(event)}'.`));
      }, opts.timeoutMs);
    }
    fire();
  });
}

// ---------------------------------------------------------------------------
// The web façade — same surface as native, routed over the active transport.
// ---------------------------------------------------------------------------

export const BeamableNotifications = {
  get isSupported(): boolean {
    return supported;
  },
  events: BEAMABLE_EVENTS,
  addSupportListener(listener: (supported: boolean) => void): Subscription {
    supportListeners.add(listener);
    listener(supported);
    return { remove: () => supportListeners.delete(listener) };
  },
  hostPlatformLabel(): string {
    const host = activeTransport.getHost();
    if (!host) return 'web';
    const os =
      host.os === 'ios' ? 'iOS' : host.os === 'android' ? 'Android' : host.os;
    return `${os} (web host)`;
  },
  devicePushPlatform(): PushPlatform {
    return activeTransport.getHost()?.os === 'ios' ? 'apns' : 'fcm';
  },

  deepLinkFromNotification,
  campaignCoordsFromNotification,

  initialize(): void {
    activeTransport.call('initialize');
  },
  requestPermission(
    options: PermissionOptions = { alert: true, badge: true, sound: true },
  ): Promise<PermissionResult> {
    if (!supported) return Promise.resolve({ status: 'denied', granted: false });
    return awaitWebEvent('permissionResult', () =>
      activeTransport.call('requestPermission', options),
    );
  },
  getPermissionStatus(): Promise<PermissionResult> {
    if (!supported) {
      return Promise.resolve({ status: 'notDetermined', granted: false });
    }
    return awaitWebEvent(
      'permissionResult',
      () => activeTransport.call('getPermissionStatus'),
      { timeoutMs: 5000 },
    );
  },
  scheduleLocal(request: LocalRequest): void {
    activeTransport.call('scheduleLocal', request);
  },
  cancelLocal(id: string): void {
    activeTransport.call('cancelLocal', { id });
  },
  cancelAllLocal(): void {
    activeTransport.call('cancelAllLocal');
  },
  getPending(): Promise<NotificationData[]> {
    if (!supported) return Promise.resolve([]);
    return awaitWebEvent(
      'pendingNotifications',
      () => activeTransport.call('getPending'),
      { timeoutMs: 5000 },
    ).catch(() => []);
  },
  registerForRemote(
    options: { timeoutMs?: number } = {},
  ): Promise<{ token: string }> {
    if (!supported) {
      return Promise.reject(
        new Error('Remote push is not supported on this host.'),
      );
    }
    return awaitWebEvent(
      'tokenReceived',
      () => activeTransport.call('registerForRemote'),
      { rejectOn: 'tokenError', timeoutMs: options.timeoutMs ?? 30000 },
    );
  },
  unregisterForRemote(): void {
    activeTransport.call('unregisterForRemote');
  },
  registerTemplate(template: TemplateSpec): void {
    activeTransport.call('registerTemplate', template);
  },
  registerCategory(category: CategorySpec): void {
    activeTransport.call('registerCategory', category);
  },
  configureAuth(auth: ConfigureAuthOptions): void {
    activeTransport.call('configureAuth', auth);
  },
  clearAuth(): void {
    activeTransport.call('clearAuth');
  },
  getDeliveryReceipts(): Promise<DeliveryReceipt[]> {
    if (!supported) return Promise.resolve([]);
    return awaitWebEvent(
      'deliveryReceipts',
      () => activeTransport.call('getDeliveryReceipts'),
      { timeoutMs: 5000 },
    ).catch(() => []);
  },
  setBadge(count: number): void {
    activeTransport.call('setBadge', { count });
  },
  clearDelivered(): void {
    activeTransport.call('clearDelivered');
  },
  getLaunchNotification(): Promise<NotificationData | null> {
    if (!supported) return Promise.resolve(null);
    return activeTransport
      .request<NotificationData | null>('getLaunchNotification')
      .catch(() => null);
  },
  trackOfferClicked(
    intent: NotificationIntentData,
    offer?: NotificationOffer,
  ): void {
    activeTransport.call('trackOfferClicked', { intent, offer });
  },
  trackOfferConverted(
    intent: NotificationIntentData,
    offer?: NotificationOffer,
  ): void {
    activeTransport.call('trackOfferConverted', { intent, offer });
  },
  scheduleLocalWithDeepLink(opts: {
    id: string;
    title: string;
    body: string;
    url: string;
    seconds?: number;
  }): void {
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
   * Swap the transport the web build uses to reach a native host (default: the bundled
   * gree/unity-webview bridge). Pass `null` to restore the default. Re-subscribes support
   * tracking to the new transport.
   */
  setWebTransport(transport: WebTransport | null): void {
    transportSub.remove();
    activeTransport = transport ?? unityTransport;
    transportSub = activeTransport.addSupportListener(emitSupport);
    emitSupport(activeTransport.isSupported());
  },

  addListener,
  addDeepLinkListener,
} satisfies NativeFacade;

export default BeamableNotifications;

/** The recommended single entry point (alias of `BeamableNotifications`). */
export const BeamNotifications = BeamableNotifications;

// ---------------------------------------------------------------------------
// Shared re-exports — identical surface to `index.ts`.
// ---------------------------------------------------------------------------

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

export { deepLinkFromNotification, campaignCoordsFromNotification } from './parsing';

export {
  BeamNotificationEvent,
  BeamLaunchNotification,
  BeamPushNotifications,
} from './hooks';

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

export {
  unityTransport,
  isUnityWebView,
  sendToUnity,
  addUnityMessageListener,
  addUnityPlatformListener,
  getUnityHostPlatform,
} from './unity/unityBridge';
export type { UnityHostPlatform } from './unity/unityBridge';
