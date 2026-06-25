import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { useRouter } from 'expo-router';

import { BEAM_CONFIG, isConfigured } from '../src/beam/config';
import {
  getBeam,
  getPushService,
  getSampleService,
  initBeam,
  type BeamStatus,
} from '../src/beam/beamClient';
import {
  addBeamableListener,
  isBeamableNotificationsSupported,
  registerForRemote,
  requestBeamablePermission,
  scheduleBeamableDeepLink,
} from '../src/notifications/beamableNotifications';
import {
  BeamableNotifications,
  type NotificationIntentData,
  type NotificationOffer,
} from '@beamable/notifications-react-native';
import { listDevices, registerDevice, sendToSelf } from '../src/beam/pushNotifications';
import { detailsPath, detailsUrl, openUrl } from '../src/linking/links';

export default function Home() {
  const router = useRouter();
  const [beam, setBeam] = useState<BeamStatus>({ state: 'idle' });
  const [log, setLog] = useState<string[]>([]);
  // The device push token (APNs on iOS, FCM on Android) from the native SDK's
  // `tokenReceived` event. Needed to register this device with the microservice.
  const [apnsToken, setApnsToken] = useState<string | null>(null);
  const platformLabel = Platform.OS === 'ios' ? 'iOS' : 'Android';
  const remoteProvider = Platform.OS === 'ios' ? 'APNs' : 'FCM';

  const append = (msg: string) =>
    setLog((prev) => [`${time()}  ${msg}`, ...prev].slice(0, 40));

  // Beamable Notifications events (native iOS SDK). The native methods are
  // fire-and-forget; their results land here. We log them, and forward the APNs
  // token to the Beamable backend so the realm can send this device pushes.
  useEffect(() => {
    if (!isBeamableNotificationsSupported) return;

    const subs = [
      addBeamableListener('permissionResult', (r) =>
        append(`Beamable permission: ${r.granted ? 'granted' : 'denied'} (${r.status})`),
      ),
      addBeamableListener('tokenReceived', async ({ token }) => {
        setApnsToken(token);
        append(`Push token: ${token.slice(0, 12)}…`);
        if (!getPushService()) return append('Token not registered — connect to Beamable first');
        try {
          const res = await registerDevice(token);
          append(`Device registered with PushNotificationService (${res.deviceCount} total)`);
        } catch (e) {
          append(`Token register error: ${e instanceof Error ? e.message : String(e)}`);
        }
      }),
      addBeamableListener('tokenError', ({ error }) => append(`Push token error: ${error}`)),
      addBeamableListener('notificationReceived', (n) =>
        append(`Beamable received: ${n.title ?? n.id}`),
      ),
      addBeamableListener('notificationOpened', (n) =>
        append(`Beamable opened → ${n.deeplink ?? n.id}`),
      ),
      addBeamableListener('funnelResult', (r) =>
        append(
          `Funnel ${r.funnelType}: ${r.ok ? 'OK' : 'FAILED'} (HTTP ${r.statusCode})${r.message ? ' — ' + r.message : ''}`,
        ),
      ),
    ];

    return () => subs.forEach((s) => s.remove());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // 1) Beamable -----------------------------------------------------------
  const connectBeam = async () => {
    setBeam({ state: 'connecting' });
    append('Beam.init() …');
    try {
      const b = await initBeam();
      const playerId = b.player.id;
      setBeam({ state: 'ready', playerId });
      append(`Beam ready. player.id = ${playerId}`);
    } catch (e) {
      const message = e instanceof Error ? e.message : String(e);
      setBeam({ state: 'error', message });
      append(`Beam error: ${message}`);
    }
  };

  // 2) Beamable Notifications (native iOS + Android SDK) ------------------
  const beamAskPermission = () => {
    requestBeamablePermission();
    append('Beamable: requested permission (result on event)');
  };

  const beamFireNow = () => {
    scheduleBeamableDeepLink({
      id: 'beam-now',
      title: 'Beamable (native)',
      body: 'Tap me to deep-link into Details #777',
      detailsId: 777,
    });
    append('Beamable local notification posted (immediate). Tap it!');
  };

  const beamFireDelayed = () => {
    scheduleBeamableDeepLink({
      id: 'beam-delayed',
      title: 'Beamable (native, delayed)',
      body: 'Background the app — tap this in 10s to deep-link to Details #888',
      detailsId: 888,
      seconds: 10,
    });
    append('Beamable local notification scheduled in 10s. Background the app & tap it.');
  };

  const beamRegisterRemote = () => {
    registerForRemote();
    append(`Beamable: registered for remote (${remoteProvider}). Token on event (real device only).`);
  };

  // 3) Remote push via the PushNotificationService microservice -----------
  // Devices auto-register on the `tokenReceived` event above. These actions
  // ask the microservice to deliver a real APNs push and to list registrations.
  const registerThisDevice = async () => {
    if (!getPushService()) return append('Register: connect to Beamable first');
    if (!apnsToken) return append(`No push token yet — tap "Register for remote (${remoteProvider})" in section 2 first (physical device).`);
    append('RegisterDeviceToken …');
    try {
      const res = await registerDevice(apnsToken);
      append(`RegisterDeviceToken → ${res.success ? 'ok' : 'failed'}: ${res.message} (${res.deviceCount} device(s))`);
    } catch (e) {
      append(`Register error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  const sendPushToMyself = async () => {
    if (!getPushService()) return append('Remote push: connect to Beamable first');
    append('Sending remote push to myself …');
    try {
      const res = await sendToSelf(
        'Hello from Beamable 👋',
        `This remote push came from your ${remoteProvider} microservice.`,
        detailsUrl(999),
      );
      append(`Push: attempted ${res.attempted}, sent ${res.succeeded}, failed ${res.failed}`);
      res.messages?.forEach((m) => append(`  · ${m}`));
    } catch (e) {
      append(`Send error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  const showMyDevices = async () => {
    if (!getPushService()) return append('Remote push: connect to Beamable first');
    try {
      const res = await listDevices();
      append(`Registered devices: ${res.devices.length}`);
      res.devices.forEach((d) =>
        append(`  · ${d.token} [${d.platform ?? 'apns'}]${d.environment ? ` (${d.environment})` : ''}`),
      );
    } catch (e) {
      append(`List error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  // 4) Deep links ---------------------------------------------------------
  const simulateDeepLink = async () => {
    const url = detailsUrl(123);
    append(`Opening URL: ${url}`);
    await openUrl(url); // routed by the OS, like an external link/push
  };

  const navigateDirect = () => router.push(detailsPath(55) as never);

  // 5) Sample microservice ------------------------------------------------
  // Each calls a [ClientCallable] on the SampleService C# microservice via the
  // typed SampleServiceClient. Requires "Connect to Beamable" to have run first.
  const callMicroservice = async (
    label: string,
    run: (svc: NonNullable<ReturnType<typeof getSampleService>>) => Promise<unknown>,
  ) => {
    const svc = getSampleService();
    if (!svc) return append('Microservice: connect to Beamable first');
    append(`${label} …`);
    try {
      const result = await run(svc);
      // Some results carry bigint ids; JSON.stringify throws on those.
      const text = JSON.stringify(result, (_k, v) =>
        typeof v === 'bigint' ? `${v}n` : v,
      );
      append(`${label} → ${text}`);
    } catch (e) {
      append(`${label} error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  // 6) Analytics (funnel) test -------------------------------------------
  // Emits native Clicked/Converted funnel analytics. Auth (cid.pid scope) is
  // configured automatically on Connect; these just fire the events.
  const buildFunnelIntent = (): NotificationIntentData => ({
    campaignId: 'test_campaign',
    nodeId: 'test_node',
    gamerTag: String(getBeam()!.player.id),
    cidPid: `${BEAM_CONFIG.cid}.${BEAM_CONFIG.pid}`,
    deeplink: detailsUrl(777),
  });

  const funnelOffer: NotificationOffer = {
    itemId: 'test_offer',
    value: 100,
    customData: { tier: 'gold' },
  };

  const trackOfferClickedTest = () => {
    if (!getBeam()) return append('Funnel: connect to Beamable first');
    try {
      BeamableNotifications.trackOfferClicked(buildFunnelIntent(), funnelOffer);
      append('Funnel: trackOfferClicked emitted (test_campaign/test_node)');
    } catch (e) {
      append(`Funnel error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  const trackOfferConvertedTest = () => {
    if (!getBeam()) return append('Funnel: connect to Beamable first');
    try {
      BeamableNotifications.trackOfferConverted(buildFunnelIntent(), funnelOffer);
      append('Funnel: trackOfferConverted emitted (test_campaign/test_node)');
    } catch (e) {
      append(`Funnel error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  const clearFunnelAuth = () => {
    try {
      BeamableNotifications.clearAuth();
      append('Native funnel auth cleared');
    } catch (e) {
      append(`Funnel error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <StatusBar style="auto" />

      <Text style={styles.h1}>Beamable · React Native</Text>
      <Text style={styles.subtitle}>
        Test panel for the Web SDK, deep links & local notifications
      </Text>

      {/* Beamable status */}
      <Section title="1 · Beamable Web SDK">
        <StatusRow status={beam} />
        {!isConfigured() && (
          <Text style={styles.warn}>
            cid/pid not set — edit src/beam/config.ts. (env:{' '}
            {String(BEAM_CONFIG.environment)})
          </Text>
        )}
        <Button label="Connect to Beamable" onPress={connectBeam} />
        <Button
          label="Explore all SDK features →"
          onPress={() => router.push('/sdk' as never)}
        />
      </Section>

      {/* Beamable Notifications — native SDK (iOS Swift core / Android .aar) */}
      <Section title={`2 · Beamable Notifications (native ${platformLabel})`}>
        {isBeamableNotificationsSupported ? (
          <>
            {Platform.OS === 'android' ? (
              <Text style={styles.hint}>
                The unified `@beamable/notifications-react-native` SDK (Android
                AAR via autolinking). Tap a notification to deep-link into the
                app. The local notification also triggers the native receive-time
                handler (PushNotificationReceivedHandler) — which runs even when
                the app is killed. Remote push needs a data-only FCM message.
              </Text>
            ) : (
              <Text style={styles.hint}>
                The unified `@beamable/notifications-react-native` SDK (iOS Swift
                core via the xcframework). Tap a notification to deep-link into
                the app. Remote push needs a physical device + APNs configured on
                your realm.
              </Text>
            )}
            <Button label="Request permission (native)" onPress={beamAskPermission} />
            <Button label="Fire now → Details #777" onPress={beamFireNow} />
            <Button
              label="Fire in 10s (background & tap) → #888"
              onPress={beamFireDelayed}
            />
            <Button label={`Register for remote (${remoteProvider})`} onPress={beamRegisterRemote} />
            <Button
              label="Test all callbacks →"
              onPress={() => router.push('/callbacks' as never)}
            />
          </>
        ) : (
          <Text style={styles.hint}>
            Native module not available on this platform.
          </Text>
        )}
      </Section>

      {/* Remote push via the PushNotificationService microservice */}
      <Section title="3 · Remote push (microservice)">
        {isBeamableNotificationsSupported ? (
          <>
            {Platform.OS === 'android' ? (
              <Text style={styles.hint}>
                Registers this device's FCM token with the PushNotificationService
                microservice, then has the server send a real remote push back to
                you via Firebase. Needs a physical device + FCM credentials
                (`fcm_push`) in your realm config. Steps: Connect to Beamable →
                Register for remote (section 2) → Register this device → Send.
              </Text>
            ) : (
              <Text style={styles.hint}>
                Registers this device's APNs token with the{' '}
                PushNotificationService microservice, then has the server send a
                real remote push back to you via Apple. Needs a physical device +
                APNs credentials in your realm config. Steps: Connect to Beamable →
                Register for remote (section 2) → Register this device → Send.
              </Text>
            )}
            <Text style={styles.hint}>
              {remoteProvider} token: {apnsToken ? `${apnsToken.slice(0, 12)}…` : 'none yet (Register for remote in section 2)'}
            </Text>
            <Button label="Register this device (microservice)" onPress={registerThisDevice} />
            <Button label="Send remote push to myself" onPress={sendPushToMyself} />
            <Button label="List my registered devices" onPress={showMyDevices} />
          </>
        ) : (
          <Text style={styles.hint}>
            Remote push is not available on this platform.
          </Text>
        )}
      </Section>

      {/* Deep links */}
      <Section title="4 · Deep links">
        <Button label="Simulate deep link → Details #123" onPress={simulateDeepLink} />
        <Button label="Navigate directly → Details #55" onPress={navigateDirect} />
        <Text style={styles.hint}>
          Or from a terminal:{'\n'}
          xcrun simctl openurl booted "beamrnsample://details/42"{'\n'}
          adb shell am start -a android.intent.action.VIEW -d
          "beamrnsample://details/42" com.beamable.rnsample
        </Text>
      </Section>

      {/* Sample microservice */}
      <Section title="5 · Sample microservice">
        <Text style={styles.hint}>
          Calls the SampleService C# microservice. Connect to Beamable first;
          results appear in the activity log.
        </Text>
        <Button
          label="Add → 2 + 3"
          onPress={() => callMicroservice('Add(2,3)', (s) => s.add({ a: 2, b: 3 }))}
        />
        <Button
          label="Greet → 'React Native'"
          onPress={() =>
            callMicroservice('Greet', (s) => s.greet({ name: 'React Native' }))
          }
        />
        <Button
          label="WhoAmI (server-verified identity)"
          onPress={() => callMicroservice('WhoAmI', (s) => s.whoAmI())}
        />
        <Button
          label="Visit (server-authoritative counter)"
          onPress={() => callMicroservice('Visit', (s) => s.visit())}
        />
      </Section>

      {/* Analytics (funnel) test */}
      <Section title="6 · Analytics (funnel) test">
        <Text style={styles.hint}>
          Emits native Clicked / Converted funnel analytics for a test offer.
          Auth (cid.pid scope) is configured automatically on Connect. Connect to
          Beamable first; events appear in the activity log.
        </Text>
        <Button label="Track offer clicked" onPress={trackOfferClickedTest} />
        <Button label="Track offer converted" onPress={trackOfferConvertedTest} />
        <Button label="Clear native auth" onPress={clearFunnelAuth} />
      </Section>

      {/* Activity log */}
      <Section title="Activity log">
        {log.length === 0 ? (
          <Text style={styles.hint}>No events yet.</Text>
        ) : (
          log.map((line, i) => (
            <Text key={i} style={styles.logLine}>
              {line}
            </Text>
          ))
        )}
      </Section>
    </ScrollView>
  );
}

function time(): string {
  const d = new Date();
  return d.toLocaleTimeString();
}

function Section(props: { title: string; children: React.ReactNode }) {
  return (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>{props.title}</Text>
      {props.children}
    </View>
  );
}

function Button(props: { label: string; onPress: () => void }) {
  return (
    <Pressable
      style={({ pressed }) => [styles.button, pressed && styles.buttonPressed]}
      onPress={props.onPress}
    >
      <Text style={styles.buttonText}>{props.label}</Text>
    </Pressable>
  );
}

function StatusRow({ status }: { status: BeamStatus }) {
  if (status.state === 'connecting') {
    return (
      <View style={styles.statusRow}>
        <ActivityIndicator />
        <Text style={styles.statusText}>Connecting…</Text>
      </View>
    );
  }
  const map: Record<BeamStatus['state'], { dot: string; text: string }> = {
    idle: { dot: '#9ca3af', text: 'Not connected' },
    connecting: { dot: '#f59e0b', text: 'Connecting…' },
    ready: {
      dot: '#22c55e',
      text:
        status.state === 'ready' ? `Ready · player ${status.playerId}` : 'Ready',
    },
    error: {
      dot: '#ef4444',
      text: status.state === 'error' ? `Error · ${status.message}` : 'Error',
    },
  };
  const s = map[status.state];
  return (
    <View style={styles.statusRow}>
      <View style={[styles.dot, { backgroundColor: s.dot }]} />
      <Text style={styles.statusText}>{s.text}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { padding: 20, paddingTop: 24, gap: 16 },
  h1: { fontSize: 24, fontWeight: '700' },
  subtitle: { fontSize: 14, color: '#6b7280', marginTop: -8 },
  section: {
    backgroundColor: '#f9fafb',
    borderRadius: 12,
    padding: 14,
    gap: 10,
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  sectionTitle: { fontSize: 16, fontWeight: '600', color: '#111827' },
  button: {
    backgroundColor: '#5A31F4',
    borderRadius: 10,
    paddingVertical: 12,
    paddingHorizontal: 14,
  },
  buttonPressed: { opacity: 0.7 },
  buttonText: { color: 'white', fontWeight: '600', textAlign: 'center' },
  statusRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  dot: { width: 10, height: 10, borderRadius: 5 },
  statusText: { fontSize: 14, color: '#374151', flexShrink: 1 },
  warn: { color: '#b45309', fontSize: 12 },
  hint: { color: '#6b7280', fontSize: 12, fontFamily: 'Courier' },
  logLine: { fontSize: 12, color: '#374151', fontFamily: 'Courier' },
});
