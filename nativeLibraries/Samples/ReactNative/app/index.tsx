import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
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
  getSampleService,
  initBeam,
  type BeamStatus,
} from '../src/beam/beamClient';
import {
  fireLocalNotification,
  requestNotificationPermission,
} from '../src/notifications/notifications';
import {
  addBeamableListener,
  isBeamableNotificationsSupported,
  registerForRemote,
  requestBeamablePermission,
  scheduleBeamableDeepLink,
} from '../src/notifications/beamableNotifications';
import { registerPushToken } from '../src/beam/push';
import { detailsPath, detailsUrl, openUrl } from '../src/linking/links';

export default function Home() {
  const router = useRouter();
  const [beam, setBeam] = useState<BeamStatus>({ state: 'idle' });
  const [log, setLog] = useState<string[]>([]);

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
        append(`APNs token: ${token.slice(0, 12)}…`);
        const beam = getBeam();
        if (!beam) return append('Token not registered — connect to Beamable first');
        try {
          await registerPushToken(beam, 'apns', token);
          append('APNs token registered with Beamable');
        } catch (e) {
          append(`Token register error: ${e instanceof Error ? e.message : String(e)}`);
        }
      }),
      addBeamableListener('tokenError', ({ error }) => append(`APNs token error: ${error}`)),
      addBeamableListener('notificationReceived', (n) =>
        append(`Beamable received: ${n.title ?? n.id}`),
      ),
      addBeamableListener('notificationTapped', (n) =>
        append(`Beamable tapped → ${n.deepLink ?? n.id}`),
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

  // 2) Notifications ------------------------------------------------------
  const askPermission = async () => {
    const granted = await requestNotificationPermission();
    append(`Notification permission: ${granted ? 'granted' : 'denied'}`);
  };

  const fireNow = async () => {
    const granted = await requestNotificationPermission();
    if (!granted) return append('Cannot fire — permission denied');
    await fireLocalNotification({
      title: 'Beamable',
      body: 'Tap me to deep-link into Details #777',
      path: detailsPath(777),
    });
    append('Local notification posted (immediate). Tap it!');
  };

  const fireDelayed = async () => {
    const granted = await requestNotificationPermission();
    if (!granted) return append('Cannot fire — permission denied');
    await fireLocalNotification({
      title: 'Beamable (delayed)',
      body: 'Background the app — tap this in 10s to deep-link to Details #888',
      path: detailsPath(888),
      seconds: 10,
    });
    append('Local notification scheduled in 10s. Background the app & tap it.');
  };

  // 2b) Beamable Notifications (native iOS SDK) ---------------------------
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
    append('Beamable: registered for remote (APNs). Token on event (real device only).');
  };

  // 3) Deep links ---------------------------------------------------------
  const simulateDeepLink = async () => {
    const url = detailsUrl(123);
    append(`Opening URL: ${url}`);
    await openUrl(url); // routed by the OS, like an external link/push
  };

  const navigateDirect = () => router.push(detailsPath(55) as never);

  // 4) Sample microservice ------------------------------------------------
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

      {/* Notifications */}
      <Section title="2 · Local notifications">
        <Button label="Request permission" onPress={askPermission} />
        <Button label="Fire now → Details #777" onPress={fireNow} />
        <Button
          label="Fire in 10s (background & tap) → #888"
          onPress={fireDelayed}
        />
      </Section>

      {/* Beamable Notifications — native iOS SDK */}
      <Section title="2b · Beamable Notifications (native iOS)">
        {isBeamableNotificationsSupported ? (
          <>
            <Text style={styles.hint}>
              The native `beamable-notifications` SDK. Same deep-link routing as
              above, but through the Swift core. Remote push needs a physical
              device + APNs configured on your realm.
            </Text>
            <Button label="Request permission (native)" onPress={beamAskPermission} />
            <Button label="Fire now → Details #777" onPress={beamFireNow} />
            <Button
              label="Fire in 10s (background & tap) → #888"
              onPress={beamFireDelayed}
            />
            <Button label="Register for remote (APNs)" onPress={beamRegisterRemote} />
            <Button
              label="Test all callbacks →"
              onPress={() => router.push('/callbacks' as never)}
            />
          </>
        ) : (
          <Text style={styles.hint}>
            iOS only — the native module is not available on this platform.
          </Text>
        )}
      </Section>

      {/* Deep links */}
      <Section title="3 · Deep links">
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
      <Section title="4 · Sample microservice">
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
