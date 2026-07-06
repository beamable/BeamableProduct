/**
 * Native entry for the app's notifications import. Screens import `BeamNotifications`
 * from here (not straight from the package) so Metro can platform-swap to the
 * Unity-WebView variant (`beamableNotifications.web.ts`) for the web build. On
 * iOS/Android this is just the package's real `BeamNotifications` façade.
 */
export { BeamNotifications } from '@beamable/notifications-react-native';
export type {
  BeamableEvent,
  NotificationData,
  NotificationIntentData,
  NotificationOffer,
} from '@beamable/notifications-react-native';
