import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import { Platform } from 'react-native';

/** Data we attach to a notification so a tap can deep-link into the app. */
export type NotificationData = { path?: string };

const ANDROID_CHANNEL_ID = 'default';

// How notifications are presented while the app is in the FOREGROUND.
// (expo-notifications 0.29 / Expo SDK 52 uses `shouldShowAlert`; SDK 53+
// replaces it with `shouldShowBanner`/`shouldShowList`.)
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});

/** Android requires a notification channel before posting notifications. */
export async function ensureAndroidChannel(): Promise<void> {
  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync(ANDROID_CHANNEL_ID, {
      name: 'Default',
      importance: Notifications.AndroidImportance.MAX,
      vibrationPattern: [0, 250, 250, 250],
      lightColor: '#5A31F4',
    });
  }
}

/** Requests notification permission (and sets up the Android channel). */
export async function requestNotificationPermission(): Promise<boolean> {
  await ensureAndroidChannel();
  const current = await Notifications.getPermissionsAsync();
  let status = current.status;
  if (status !== 'granted') {
    const requested = await Notifications.requestPermissionsAsync();
    status = requested.status;
  }
  return status === 'granted';
}

/**
 * Schedules a LOCAL notification. When tapped, the `path` in its data is read
 * by the response listener in app/_layout.tsx and pushed onto the router,
 * demonstrating notification -> deep-link routing.
 *
 * @param seconds delay before firing. Omit/0 to fire immediately.
 */
export async function fireLocalNotification(opts: {
  title: string;
  body: string;
  path?: string;
  seconds?: number;
}): Promise<string> {
  const data: NotificationData = { path: opts.path };

  const trigger =
    opts.seconds && opts.seconds > 0
      ? {
          type: Notifications.SchedulableTriggerInputTypes.TIME_INTERVAL,
          seconds: opts.seconds,
          channelId: ANDROID_CHANNEL_ID,
        }
      : null; // null = deliver immediately

  return Notifications.scheduleNotificationAsync({
    content: { title: opts.title, body: opts.body, data },
    trigger,
  });
}

/**
 * OPTIONAL: returns the native device push token (FCM/APNS) for registering
 * with a push backend (e.g. Beamable PushApi). Requires a physical device and
 * a dev build — returns null on simulators/emulators or if permission denied.
 */
export async function getDevicePushToken(): Promise<string | null> {
  if (!Device.isDevice) return null;
  const granted = await requestNotificationPermission();
  if (!granted) return null;
  const token = await Notifications.getDevicePushTokenAsync();
  return String(token.data);
}
