import { useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { useRouter } from 'expo-router';

import { BEAM_CONFIG, isConfigured } from '../src/beam/config';
import {
  getBeam,
  getPushService,
  initBeam,
  type BeamStatus,
} from '../src/beam/beamClient';
import { BeamNotifications } from '../src/notifications/beamableNotifications';
import type {
  BeamableEvent,
  NotificationData,
  NotificationIntentData,
  NotificationOffer,
} from '@beamable/notifications-react-native';
import { listDevices, registerDevice } from '../src/beam/pushNotifications';
import { detailsPath, detailsUrl, openUrl } from '../src/linking/links';
import UnityBridgeSection from '../src/unity/UnityBridgeSection';

// A colour per native event so firings are easy to tell apart in the event log.
const EVENT_COLOR: Record<BeamableEvent, string> = {
  permissionResult: '#2563eb',
  tokenReceived: '#16a34a',
  tokenError: '#dc2626',
  notificationPresented: '#7c3aed',
  notificationReceived: '#0891b2',
  notificationOpened: '#ea580c',
  pendingNotifications: '#ca8a04',
  deliveryReceipts: '#0d9488',
  funnelResult: '#db2777',
};

// One captured native-event firing.
type EventEntry = { key: number; time: string; event: BeamableEvent; data: unknown };

// Loose signature for subscribing in a loop (the typed `addListener`
// correlates payload to event name, which a generic loop can't express).
const subscribe = BeamNotifications.addListener as unknown as (
  event: BeamableEvent,
  handler: (data: unknown) => void,
) => { remove: () => void };

export default function Home() {
  const router = useRouter();
  const [beam, setBeam] = useState<BeamStatus>({ state: 'idle' });
  // Free-text feedback from button presses (connect, register, list, funnel).
  const [log, setLog] = useState<string[]>([]);
  // Every native SDK event, captured raw with its payload (the merged callbacks view).
  const [events, setEvents] = useState<EventEntry[]>([]);
  const eventCounter = useRef(0);
  // The device push token (APNs on iOS, FCM on Android) from the native SDK's
  // `tokenReceived` event. Needed to register this device with the microservice.
  const [pushToken, setPushToken] = useState<string | null>(null);
  // §4 funnel coordinates. Editable by the user; auto-filled from the campaign push that
  // opened (or was tapped in) the app — see `applyCampaignCoords`.
  const [campaignId, setCampaignId] = useState('');
  const [nodeId, setNodeId] = useState('');
  // Native support is static on iOS/Android but DYNAMIC on web: it flips to
  // true once a Unity WebView host reports a native-capable platform.
  const [nativeSupported, setNativeSupported] = useState(
    BeamNotifications.isSupported,
  );
  useEffect(() => {
    const sub = BeamNotifications.addSupportListener(setNativeSupported);
    return () => sub.remove();
  }, []);
  const platformLabel = BeamNotifications.hostPlatformLabel();
  const isAndroidHost = BeamNotifications.devicePushPlatform() === 'fcm';
  const remoteProvider = isAndroidHost ? 'FCM' : 'APNs';

  const append = (msg: string) =>
    setLog((prev) => [`${time()}  ${msg}`, ...prev].slice(0, 40));

  // Override the §4 funnel coordinates from a notification that carries them. Notifications
  // without campaignId/nodeId (e.g. the local test notifications) leave the user's typed
  // values untouched.
  const applyCampaignCoords = (n: NotificationData) => {
    const coords = BeamNotifications.campaignCoordsFromNotification(n);
    if (coords.campaignId) setCampaignId(coords.campaignId);
    if (coords.nodeId) setNodeId(coords.nodeId);
    if (coords.campaignId || coords.nodeId) {
      append(
        `Funnel coords from notification: campaignId=${coords.campaignId ?? '—'} nodeId=${coords.nodeId ?? '—'}`,
      );
    }
  };

  // ── Native events → the color-coded event log ─────────────────────────────
  // Subscribe to EVERY event the SDK emits and capture each firing with its full
  // payload. This is the raw view of the native library at work (permission,
  // token, opened/received/presented, delivery receipts, funnel results).
  useEffect(() => {
    if (!nativeSupported) return;
    BeamNotifications.initialize();
    const subs = BeamNotifications.events.map((event) =>
      subscribe(event, (data) =>
        setEvents((prev) =>
          [
            { key: (eventCounter.current += 1), time: time(), event, data },
            ...prev,
          ].slice(0, 100),
        ),
      ),
    );
    return () => subs.forEach((s) => s.remove());
  }, [nativeSupported]);

  // ── Native events → app side effects ──────────────────────────────────────
  // The token registration flow and campaign-coordinate capture. (Display of the
  // raw events happens in the effect above; here we only react to them.)
  useEffect(() => {
    if (!nativeSupported) return;
    const subs = [
      // Push token arrived → remember it and register this device with Beamable
      // so the realm can target it. This is the core token-registration flow.
      BeamNotifications.addListener('tokenReceived', async ({ token }) => {
        setPushToken(token);
        if (!getPushService()) return append('Token not registered — connect to Beamable first');
        try {
          const res = await registerDevice(token, BeamNotifications.devicePushPlatform());
          append(`Device registered with CampaignService (${res.deviceCount} total)`);
        } catch (e) {
          append(`Token register error: ${e instanceof Error ? e.message : String(e)}`);
        }
      }),
      // App already running: a tapped campaign push replaces the funnel coordinates.
      BeamNotifications.addListener('notificationOpened', (n) => applyCampaignCoords(n)),
    ];
    return () => subs.forEach((s) => s.remove());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [nativeSupported]);

  // Cold start: if the app was launched by tapping a campaign push, seed §4's funnel
  // coordinates from its payload. `getLaunchNotification()` reads the cached launch payload
  // (also consumed for routing in app/_layout.tsx), so reading it here has no side effect.
  useEffect(() => {
    if (!nativeSupported) return;
    BeamNotifications.getLaunchNotification().then((launch) => {
      if (launch) applyCampaignCoords(launch);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [nativeSupported]);

  // 1) Web SDK ------------------------------------------------------------
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

  // 2) Permission & remote registration ----------------------------------
  const beamAskPermission = () => {
    BeamNotifications.requestPermission();
    append('Requested notification permission (result on permissionResult event)');
  };

  const beamRegisterRemote = () => {
    BeamNotifications.registerForRemote();
    append(`Registered for remote (${remoteProvider}). Token arrives on the tokenReceived event (physical device only).`);
  };

  const beamFireLocal = () => {
    BeamNotifications.scheduleLocalWithDeepLink({
      id: 'beam-local',
      title: 'Beamable (local)',
      body: 'Tap me to deep-link into Details #777',
      url: detailsUrl(777),
    });
    append('Local notification posted (immediate). Tap it to see notificationOpened + deep link.');
  };

  const beamFireDelayed = () => {
    BeamNotifications.scheduleLocalWithDeepLink({
      id: 'beam-delayed',
      title: 'Beamable (local, delayed)',
      body: 'Background the app — tap this in 10s to deep-link to Details #888',
      url: detailsUrl(888),
      seconds: 10,
    });
    append('Local notification scheduled in 10s. Background the app & tap it.');
  };

  // 3) Device registration via the CampaignService microservice ----------
  // Devices auto-register on the `tokenReceived` event above. These actions
  // register this device manually and list the player's registrations.
  const registerThisDevice = async () => {
    if (!getPushService()) return append('Register: connect to Beamable first');
    if (!pushToken) return append(`No push token yet — tap "Register for remote (${remoteProvider})" first (physical device).`);
    append('RegisterDeviceToken …');
    try {
      const res = await registerDevice(pushToken, BeamNotifications.devicePushPlatform());
      append(`RegisterDeviceToken → ${res.success ? 'ok' : 'failed'}: ${res.message} (${res.deviceCount} device(s))`);
    } catch (e) {
      append(`Register error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  const showMyDevices = async () => {
    if (!getPushService()) return append('List devices: connect to Beamable first');
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

  // 4) Analytics (funnel) ------------------------------------------------
  // Emits native Clicked/Converted funnel analytics. Auth (cid.pid scope) is
  // configured automatically on Connect; these just fire the events.
  const buildFunnelIntent = (): NotificationIntentData => ({
    campaignId: campaignId.trim(),
    nodeId: nodeId.trim(),
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
      BeamNotifications.trackOfferClicked(buildFunnelIntent(), funnelOffer);
      append('Funnel: trackOfferClicked emitted (result on funnelResult event)');
    } catch (e) {
      append(`Funnel error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  const trackOfferConvertedTest = () => {
    if (!getBeam()) return append('Funnel: connect to Beamable first');
    try {
      BeamNotifications.trackOfferConverted(buildFunnelIntent(), funnelOffer);
      append('Funnel: trackOfferConverted emitted (result on funnelResult event)');
    } catch (e) {
      append(`Funnel error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  const clearNativeFunnelAuth = () => {
    try {
      BeamNotifications.clearAuth();
      append('Native funnel auth cleared');
    } catch (e) {
      append(`Funnel error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  // 5) Deep links ---------------------------------------------------------
  const simulateDeepLink = async () => {
    const url = detailsUrl(123);
    append(`Opening URL: ${url}`);
    await openUrl(url); // routed by the OS, like an external link/push
  };

  const navigateDirect = () => router.push(detailsPath(55) as never);

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <StatusBar style="auto" />

      <Text style={styles.h1}>Beamable · React Native</Text>
      <Text style={styles.subtitle}>
        Push notifications: token registration, devices, analytics & native events
      </Text>

      {/* 1 · Web SDK connect */}
      <Section title="1 · Beamable Web SDK">
        <StatusRow status={beam} />
        {!isConfigured() && (
          <Text style={styles.warn}>
            cid/pid not set — edit src/beam/config.ts. (env:{' '}
            {String(BEAM_CONFIG.environment)})
          </Text>
        )}
        <Button label="Connect to Beamable" onPress={connectBeam} />
      </Section>

      {/* 2 · Permission & remote registration (native SDK) */}
      <Section title={`2 · Permission & remote registration (native ${platformLabel})`}>
        {nativeSupported ? (
          <>
            <Text style={styles.hint}>
              The unified `@beamable/notifications-react-native` SDK (
              {isAndroidHost ? 'Android AAR' : 'iOS xcframework'} via autolinking).
              Request permission, then register for remote push — the device token
              arrives on the tokenReceived event and is auto-registered with the
              CampaignService microservice below. Remote push needs a physical device
              + {remoteProvider} credentials on your realm.
            </Text>
            <Button label="Request permission" onPress={beamAskPermission} />
            <Button label={`Register for remote (${remoteProvider})`} onPress={beamRegisterRemote} />
            <Button label="Fire local now → Details #777" onPress={beamFireLocal} />
            <Button label="Fire local in 10s (background & tap) → #888" onPress={beamFireDelayed} />
          </>
        ) : (
          <Text style={styles.hint}>Native module not available on this platform.</Text>
        )}
      </Section>

      {/* 3 · Device registration via the CampaignService microservice */}
      <Section title="3 · Device registration (microservice)">
        {nativeSupported ? (
          <>
            <Text style={styles.hint}>
              Registers this device's {remoteProvider} token with the CampaignService
              microservice so the realm can target it. Steps: Connect to Beamable →
              Register for remote (section 2) → Register this device. Delivery is driven
              from the Portal Campaign Builder.
            </Text>
            <Text style={styles.hint}>
              {remoteProvider} token: {pushToken ? `${pushToken.slice(0, 12)}…` : 'none yet (Register for remote in section 2)'}
            </Text>
            <Button label="Register this device (microservice)" onPress={registerThisDevice} />
            <Button label="List my registered devices" onPress={showMyDevices} />
          </>
        ) : (
          <Text style={styles.hint}>Device registration is not available on this platform.</Text>
        )}
      </Section>

      {/* 4 · Analytics (funnel) */}
      <Section title="4 · Analytics (funnel): clicked / converted">
        <Text style={styles.hint}>
          Emits native Clicked / Converted funnel analytics for a test offer (iOS &
          Android). Auth is configured automatically on Connect; the result lands on
          the funnelResult event.{'\n'}
          Type any Campaign / Node ID below — or open the app from a campaign push and
          these fields auto-fill from its payload.
        </Text>
        <TextInput
          style={styles.input}
          placeholder="Campaign ID (e.g. test_campaign)"
          autoCapitalize="none"
          autoCorrect={false}
          value={campaignId}
          onChangeText={setCampaignId}
        />
        <TextInput
          style={styles.input}
          placeholder="Node ID (e.g. test_node)"
          autoCapitalize="none"
          autoCorrect={false}
          value={nodeId}
          onChangeText={setNodeId}
        />
        <Button label="Track offer clicked" onPress={trackOfferClickedTest} />
        <Button label="Track offer converted" onPress={trackOfferConvertedTest} />
        <Button label="Clear native auth" onPress={clearNativeFunnelAuth} />
      </Section>

      {/* 5 · Deep links */}
      <Section title="5 · Deep links">
        <Button label="Simulate deep link → Details #123" onPress={simulateDeepLink} />
        <Button label="Navigate directly → Details #55" onPress={navigateDirect} />
        <Text style={styles.hint}>
          Or from a terminal:{'\n'}
          xcrun simctl openurl booted "beamrnsample://details/42"{'\n'}
          adb shell am start -a android.intent.action.VIEW -d "beamrnsample://details/42" com.beamable.rnsample
        </Text>
      </Section>

      {/* Unity ↔ React bridge (web build hosted inside a Unity WebView) */}
      {Platform.OS === 'web' && <UnityBridgeSection />}

      {/* Native events — the raw SDK event stream */}
      <View style={styles.logHeader}>
        <Text style={styles.sectionTitle}>Native events ({events.length})</Text>
        {events.length > 0 && (
          <Pressable onPress={() => setEvents([])}>
            <Text style={styles.clear}>Clear</Text>
          </Pressable>
        )}
      </View>
      {events.length === 0 ? (
        <Text style={styles.hint}>
          No native events yet. Trigger an action above (permission, remote register,
          fire a local notification and tap it).
        </Text>
      ) : (
        events.map((e) => (
          <View key={e.key} style={styles.eventCard}>
            <View style={styles.eventTop}>
              <View style={[styles.badge, { backgroundColor: EVENT_COLOR[e.event] }]}>
                <Text style={styles.badgeText}>{e.event}</Text>
              </View>
              <Text style={styles.eventTime}>{e.time}</Text>
            </View>
            <Text style={styles.json}>{pretty(e.data)}</Text>
          </View>
        ))
      )}

      {/* Activity log — outcomes of button presses */}
      <Section title="Activity log">
        {log.length === 0 ? (
          <Text style={styles.hint}>No activity yet.</Text>
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
  return new Date().toLocaleTimeString();
}

function pretty(data: unknown): string {
  try {
    return JSON.stringify(data, null, 2);
  } catch {
    return String(data);
  }
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
  input: {
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 8,
    paddingVertical: 10,
    paddingHorizontal: 12,
    fontSize: 14,
    color: '#111827',
    backgroundColor: 'white',
  },
  buttonText: { color: 'white', fontWeight: '600', textAlign: 'center' },
  statusRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  dot: { width: 10, height: 10, borderRadius: 5 },
  statusText: { fontSize: 14, color: '#374151', flexShrink: 1 },
  warn: { color: '#b45309', fontSize: 12 },
  hint: { color: '#6b7280', fontSize: 12, fontFamily: 'Courier' },
  logLine: { fontSize: 12, color: '#374151', fontFamily: 'Courier' },
  logHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: 2,
  },
  clear: { color: '#dc2626', fontWeight: '600', fontSize: 13 },
  eventCard: { backgroundColor: '#0b1021', borderRadius: 10, padding: 12, gap: 8 },
  eventTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  badgeText: { color: 'white', fontWeight: '700', fontSize: 12 },
  eventTime: { color: '#9ca3af', fontSize: 12, fontFamily: 'Courier' },
  json: { color: '#e5e7eb', fontSize: 12, fontFamily: 'Courier' },
});
