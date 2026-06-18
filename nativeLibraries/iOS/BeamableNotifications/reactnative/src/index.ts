import { NativeModules, NativeEventEmitter, Platform } from 'react-native';

const LINKING_ERROR =
  `The package 'beamable-notifications' doesn't seem to be linked. Make sure:\n` +
  Platform.select({ ios: "- You have run 'pod install'\n", default: '' }) +
  '- This package only supports iOS.';

const Native = NativeModules.BeamableNotificationsModule
  ? NativeModules.BeamableNotificationsModule
  : new Proxy(
      {},
      {
        get() {
          throw new Error(LINKING_ERROR);
        },
      }
    );

const emitter = new NativeEventEmitter(Native);

// ---------------------------------------------------------------------------
// Types — mirror the Swift core's Codable models.
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
  status: 'notDetermined' | 'denied' | 'authorized' | 'provisional' | 'ephemeral' | string;
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

// ---------------------------------------------------------------------------
// Events (feature 3)
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

export function addListener<K extends keyof EventMap>(
  event: K,
  handler: (payload: EventMap[K]) => void
) {
  return emitter.addListener(event, handler as (p: unknown) => void);
}

// ---------------------------------------------------------------------------
// API
// ---------------------------------------------------------------------------

export const BeamableNotifications = {
  initialize(): void {
    Native.initialize();
  },

  // Permission (feature 5)
  requestPermission(options: PermissionOptions = {}): void {
    Native.requestPermission(options);
  },
  getPermissionStatus(): void {
    Native.getPermissionStatus();
  },

  // Local notifications (feature 1)
  scheduleLocal(request: LocalRequest): void {
    Native.scheduleLocal(request);
  },
  cancelLocal(id: string): void {
    Native.cancelLocal(id);
  },
  cancelAllLocal(): void {
    Native.cancelAllLocal();
  },
  getPending(): void {
    Native.getPending();
  },

  // Remote notifications (feature 2)
  registerForRemote(): void {
    Native.registerForRemote();
  },
  unregisterForRemote(): void {
    Native.unregisterForRemote();
  },

  // Templates / categories / analytics (features 4, 7, 8)
  registerTemplate(template: TemplateSpec): void {
    Native.registerTemplate(template);
  },
  registerCategory(category: CategorySpec): void {
    Native.registerCategory(category);
  },
  configureAnalytics(config: AnalyticsConfig): void {
    Native.configureAnalytics(config);
  },
  getDeliveryReceipts(): void {
    Native.getDeliveryReceipts();
  },

  // Badge
  setBadge(count: number): void {
    Native.setBadge(count);
  },
  clearDelivered(): void {
    Native.clearDelivered();
  },

  // Get intent (feature 6)
  getLaunchNotification(): Promise<NotificationData | null> {
    return Native.getLaunchNotification();
  },

  addListener,
};

export default BeamableNotifications;
