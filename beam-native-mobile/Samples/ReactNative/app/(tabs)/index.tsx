import { BeamNotifications, BeamPushNotifications } from '@beamable/notifications-react-native';

import { listDevices, registerDevice, unregisterDevice } from '../../src/beam/pushNotifications';
import { detailsUrl } from '../../src/linking/links';
import { useBeam } from '../../src/state/beamContext';
import { useLogActions } from '../../src/state/logContext';
import AsyncButton from '../../src/ui/AsyncButton';
import Button from '../../src/ui/Button';
import Collapsible from '../../src/ui/Collapsible';
import { Hint, Value } from '../../src/ui/Hint';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';

/**
 * Push tab: notification permission, remote (FCM/APNs) registration, the backend `push`
 * message rail, and a collapsed Debug group for the local-only demos.
 */
export default function PushTab() {
  const { append } = useLogActions();
  const { isReady } = useBeam();

  // Initializes the SDK on mount and tracks support / permission / token / last-opened as
  // reactive state. `isSupported` is DYNAMIC on web — it flips true once a Unity WebView host
  // reports native support.
  const push = BeamPushNotifications();
  const platformLabel = BeamNotifications.hostPlatformLabel();
  const isAndroidHost = BeamNotifications.devicePushPlatform() === 'fcm';
  const remoteProvider = isAndroidHost ? 'FCM' : 'APNs';
  // Live Activities are ActivityKit (iOS 16.1+) — there is no Android equivalent, so the
  // controls below are hidden entirely rather than left to no-op. Derived from the push
  // platform rather than `Platform.OS` so a web build hosted in an iOS Unity WebView still
  // reports correctly, consistent with `remoteProvider` above.
  const supportsLiveActivities = !isAndroidHost;

  if (!push.isSupported) {
    return (
      <Screen>
        <Section title={`Native push (${platformLabel})`}>
          <Hint>Native module not available on this platform.</Hint>
        </Section>
      </Screen>
    );
  }

  const askPermission = async () => {
    const result = await push.requestPermission();
    if (!result.granted) throw new Error(`Permission ${result.status} — not granted`);
    return `Permission granted (${result.status})`;
  };

  const registerRemote = async () => {
    try {
      const { token } = await push.registerForRemote();
      return `Token received: ${token.slice(0, 12)}… (auto-registering with CampaignService)`;
    } catch (e) {
      throw new Error(
        `${errText(e)} — needs a physical device + ${remoteProvider} credentials on your realm.`,
      );
    }
  };

  const optInToPush = async () => {
    if (!isReady) throw new Error('Beamable is not connected yet');
    if (!push.token)
      throw new Error(
        `No push token yet — tap "Register for remote (${remoteProvider})" first (physical device).`,
      );
    const res = await registerDevice(push.token, BeamNotifications.devicePushPlatform());
    // The rail answers 200 with `success: false` on a rejected registration.
    if (!res.success) throw new Error(res.message || 'Push registration rejected by the backend');
    return `Push opt-in ok${res.message ? ` — ${res.message}` : ''}`;
  };

  // Opt out removes this player's push registration (mirrors the email / in-game opt-out).
  // Unlike opt-in, no token is needed — the backend unregisters the player from the `push`
  // federation by playerId.
  const optOutOfPush = async () => {
    if (!isReady) throw new Error('Beamable is not connected yet');
    const res = await unregisterDevice();
    if (!res.success) throw new Error(res.message || 'Push opt-out rejected by the backend');
    return `Push opt-out ok${res.message ? ` — ${res.message}` : ''}`;
  };

  const showMyDevices = async () => {
    if (!isReady) throw new Error('Beamable is not connected yet');
    const res = await listDevices();
    // The per-device detail is scrollback, so it goes to the Activity log; the button itself
    // just reports the count.
    res.devices.forEach((d) =>
      append(`  · ${d.token} [${d.platform ?? 'apns'}]${d.environment ? ` (${d.environment})` : ''}`),
    );
    return `Registered devices: ${res.devices.length}`;
  };

  const fireLocal = () => {
    BeamNotifications.scheduleLocalWithDeepLink({
      id: 'beam-local',
      title: 'Beamable (local)',
      body: 'Tap me to deep-link into Details #777',
      url: detailsUrl(777),
    });
    append('Local notification posted (immediate). Tap it to see notificationOpened + deep link.');
  };

  const fireDelayed = () => {
    BeamNotifications.scheduleLocalWithDeepLink({
      id: 'beam-delayed',
      title: 'Beamable (local, delayed)',
      body: 'Background the app — tap this in 10s to deep-link to Details #888',
      url: detailsUrl(888),
      seconds: 10,
    });
    append('Local notification scheduled in 10s. Background the app & tap it.');
  };

  // A countdown Live Activity — the no-tap, always-visible card on the Lock Screen / Dynamic
  // Island (iOS 16.1+). Unlike the `countdown` notification (custom UI only on expand), this
  // ticks down on its own with nothing tapped. Uses the Android-style relative
  // `expiresInSeconds`; anchored to an absolute expiry once at start, so it never restarts.
  const startCountdown = () => {
    BeamNotifications.startCountdownLiveActivity({
      title: 'Flash Sale',
      body: 'Save 30% — offer ends soon',
      expiresInSeconds: 30,
    });
    append('Live Activity started (iOS 16.1+). Lock the screen (Cmd+L) — the countdown ticks with no tap.');
  };

  const endCountdown = () => {
    BeamNotifications.endCountdownLiveActivity();
    append('Live Activity ended.');
  };

  // Local-start demos for the two push-driven Live Activities (Actions + Animated).
  // Push-to-start can't run on the Simulator, so these exercise the widget UIs + interactive
  // buttons locally; the real end-to-end path is portal → push rail → APNs push-to-start (see
  // the token forwarding wired on connect in BeamProvider).
  const startActions = () => {
    BeamNotifications.startActionsLiveActivity({
      title: 'Daily Reward',
      headline: 'Your reward is ready',
      body: 'Claim 500 coins before they expire.',
      buttons: [
        { id: 'claim', title: 'Claim' },
        { id: 'dismiss', title: 'Dismiss', role: 'destructive' },
      ],
    });
    append('Actions Live Activity started. Lock the screen — tap Claim/Dismiss with no app open.');
  };

  const startAnimated = () => {
    BeamNotifications.startAnimatedLiveActivity({
      title: 'Live Event',
      body: 'The arena is heating up!',
      colors: ['#3366F2', '#F25A4D', '#27B373', '#F29E26'],
      flipIntervalMs: 900,
    });
    append('Animated Live Activity started (color panels cycle; throttled on the Lock Screen).');
  };

  const endOthers = () => {
    BeamNotifications.endLiveActivities();
    append('Actions/Animated Live Activities ended.');
  };

  return (
    <Screen>
      <Section title={`Permission (native ${platformLabel})`}>
        <Hint>
          The unified `@beamable/notifications-react-native` SDK (
          {isAndroidHost ? 'Android AAR' : 'iOS xcframework'} via autolinking). Grant permission
          before registering for remote push.
        </Hint>
        {push.permission && (
          <Value label="Status">
            {push.permission.status}
            {push.permission.granted ? ' · granted' : ''}
          </Value>
        )}
        <AsyncButton label="Request permission" run={askPermission} />
      </Section>

      <Section title="Remote registration">
        <Hint>
          Register for remote push — the device token arrives on the tokenReceived event and is
          auto-registered with the backend `push` message rail below. Needs a physical device +
          {' '}{remoteProvider} credentials on your realm.
        </Hint>
        <Value label={`${remoteProvider} token`}>
          {push.token ? `${push.token.slice(0, 12)}…` : 'none yet'}
        </Value>
        <AsyncButton label={`Register for remote (${remoteProvider})`} run={registerRemote} />
      </Section>

      <Section title="Push rail">
        <Hint>
          Opt in registers this device's {remoteProvider} token with the backend `push` message
          rail (POST /api/message-rail/register) so the realm can target it; opt out unregisters
          the player (POST /api/message-rail/unregister). Delivery is driven from the Portal
          Campaign Builder.
        </Hint>
        <AsyncButton label="Opt in to push (register this device)" run={optInToPush} />
        <AsyncButton
          label="Opt out of push (unregister this device)"
          variant="secondary"
          run={optOutOfPush}
        />
        <AsyncButton
          label="List my registered devices"
          variant="secondary"
          run={showMyDevices}
        />
      </Section>

      <Collapsible
        title={`Debug · local notifications${supportsLiveActivities ? ' & Live Activities' : ''}`}
      >
        <Hint>
          These fire entirely on-device — no server, no realm credentials. Useful for checking
          deep-link routing{supportsLiveActivities ? ' and the Live Activity widget UIs' : ''}.
        </Hint>
        <Button label="Fire local now → Details #777" onPress={fireLocal} />
        <Button label="Fire local in 10s (background & tap) → #888" onPress={fireDelayed} />
        {supportsLiveActivities && (
          <>
            <Button
              label="Start countdown Live Activity (no tap, iOS 16.1+)"
              onPress={startCountdown}
            />
            <Button label="End Live Activity" variant="secondary" onPress={endCountdown} />
            <Button label="Start Actions Live Activity (buttons, local)" onPress={startActions} />
            <Button label="Start Animated Live Activity (local)" onPress={startAnimated} />
            <Button
              label="End Actions/Animated Live Activities"
              variant="secondary"
              onPress={endOthers}
            />
          </>
        )}
      </Collapsible>
    </Screen>
  );
}

function errText(e: unknown): string {
  return e instanceof Error ? e.message : String(e);
}
