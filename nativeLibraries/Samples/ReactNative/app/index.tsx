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
import {
  BeamNotifications,
  BeamPushNotifications,
  BeamNotificationEvent,
  BeamLaunchNotification,
} from '@beamable/notifications-react-native';
import type {
  BeamableEvent,
  NotificationData,
  NotificationIntentData,
  NotificationOffer,
} from '@beamable/notifications-react-native';
import { listDevices, registerDevice, unregisterDevice } from '../src/beam/pushNotifications';
import { addEmail } from '../src/beam/account';
import { listInGameMessages } from '../src/beam/ingameMessages';
import { registerRail, unregisterRail } from '../src/beam/messageRail';
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

export default function Home() {
  const router = useRouter();
  const [beam, setBeam] = useState<BeamStatus>({ state: 'idle' });
  // Free-text feedback from button presses (connect, register, list, funnel).
  // Each entry carries a stable id so prepending a new line MOUNTS one <Text> instead of
  // mutating every existing line's content (index keys would re-write them all on each append,
  // and heavy text re-serialization can trip a New-Architecture text-layout crash).
  const [log, setLog] = useState<{ id: number; text: string }[]>([]);
  const logCounter = useRef(0);
  // Every native SDK event, captured raw with its payload (the merged callbacks view).
  const [events, setEvents] = useState<EventEntry[]>([]);
  const eventCounter = useRef(0);
  // §4 funnel coordinates. Editable by the user; auto-filled from the campaign push that
  // opened (or was tapped in) the app — see `applyCampaignCoords`.
  const [campaignId, setCampaignId] = useState('');
  const [nodeId, setNodeId] = useState('');
  // Account · add-email inputs.
  const [accountEmail, setAccountEmail] = useState('');
  const [accountPassword, setAccountPassword] = useState('');
  // In-game messages (the player's Beamable mailbox — see InGameRailService).
  const [messages, setMessages] = useState<
    Awaited<ReturnType<typeof listInGameMessages>>
  >([]);
  // Quake-style activity console: always pinned to the bottom, expand/collapse on tap.
  const [logOpen, setLogOpen] = useState(false);

  // One P0 hook replaces the old nativeSupported/pushToken state and their effects: it
  // initializes on mount and tracks support (DYNAMIC on web — flips true once a Unity
  // WebView host reports native support), permission, the device push token, and the last
  // opened notification, all as reactive state. It also hands back the Promise-returning
  // actions used by the buttons below.
  const push = BeamPushNotifications();
  const nativeSupported = push.isSupported;
  const pushToken = push.token;
  const platformLabel = BeamNotifications.hostPlatformLabel();
  const isAndroidHost = BeamNotifications.devicePushPlatform() === 'fcm';
  const remoteProvider = isAndroidHost ? 'FCM' : 'APNs';

  const append = (msg: string) =>
    setLog((prev) =>
      [
        { id: (logCounter.current += 1), text: `${time()}  ${msg}` },
        ...prev,
      ].slice(0, 40),
    );

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
  // `addAllListeners` (P1) subscribes to the whole event vocabulary in one typed call and
  // returns a single subscription — no per-event loop, no `addListener` cast.
  useEffect(() => {
    if (!nativeSupported) return;
    const sub = BeamNotifications.addAllListeners((event, data) =>
      setEvents((prev) =>
        [
          { key: (eventCounter.current += 1), time: time(), event, data },
          ...prev,
        ].slice(0, 100),
      ),
    );
    return () => sub.remove();
  }, [nativeSupported]);

  // ── Native events → app side effects (via the P0 event hook) ──────────────
  // Push token arrived → register this device with Beamable (the backend `push` message
  // rail) so the realm can target it. (`push.token` reflects the token too; here we run the
  // registration side effect.) The hook owns subscribe/unsubscribe — no manual effect, no
  // exhaustive-deps disable.
  BeamNotificationEvent('tokenReceived', async ({ token }) => {
    if (!getBeam()) return append('Token not registered — connect to Beamable first');
    try {
      const res = await registerDevice(token, BeamNotifications.devicePushPlatform());
      append(`Device registered via message-rail (push): ${res.success ? 'ok' : 'failed'}${res.message ? ` — ${res.message}` : ''}`);
    } catch (e) {
      append(`Token register error: ${e instanceof Error ? e.message : String(e)}`);
    }
  });

  // App already running: a tapped campaign push replaces the funnel coordinates.
  BeamNotificationEvent('notificationOpened', (n) => applyCampaignCoords(n));

  // Cold start: if the app was launched by tapping a campaign push, seed §4's funnel
  // coordinates from its payload. `BeamLaunchNotification` resolves the cached launch payload
  // (also consumed for routing in app/_layout.tsx), so reading it here has no side effect.
  const launch = BeamLaunchNotification();
  useEffect(() => {
    if (launch) applyCampaignCoords(launch);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [launch]);

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
  // Both calls now RESOLVE with their result (the events still fire too, feeding the log
  // and `push` state) — no need to correlate a separate event by hand.
  const beamAskPermission = async () => {
    append('Requesting notification permission…');
    const result = await push.requestPermission();
    append(`Permission: ${result.status}${result.granted ? ' (granted)' : ''}`);
  };

  const beamRegisterRemote = async () => {
    append(`Registering for remote (${remoteProvider})…`);
    try {
      const { token } = await push.registerForRemote();
      append(`Token received: ${token.slice(0, 12)}… (auto-registering with CampaignService)`);
    } catch (e) {
      append(
        `Remote register failed: ${e instanceof Error ? e.message : String(e)} — needs a physical device + ${remoteProvider} credentials on your realm.`,
      );
    }
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

  // 3) Device registration via the backend `push` message rail ----------
  // Devices auto-register on the `tokenReceived` event above. These actions
  // register this device manually and list the player's registrations.
  const registerThisDevice = async () => {
    if (!getBeam()) return append('Register: connect to Beamable first');
    if (!pushToken) return append(`No push token yet — tap "Register for remote (${remoteProvider})" first (physical device).`);
    append('message-rail/register (push) …');
    try {
      const res = await registerDevice(pushToken, BeamNotifications.devicePushPlatform());
      append(`Register push → ${res.success ? 'ok' : 'failed'}${res.message ? `: ${res.message}` : ''}`);
    } catch (e) {
      append(`Register error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  // Opt out of the push rail: removes this player's push registration (mirrors the
  // email / in-game opt-out). Unlike opt-in, no token is needed — the backend unregisters
  // the player from the `push` federation by playerId.
  const optOutOfPush = async () => {
    if (!getBeam()) return append('Opt out of push: connect to Beamable first');
    append('message-rail/unregister (push) …');
    try {
      const res = await unregisterDevice();
      append(`Push opt-out → ${res.success ? 'ok' : 'failed'}${res.message ? `: ${res.message}` : ''}`);
    } catch (e) {
      append(`Push opt-out error: ${e instanceof Error ? e.message : String(e)}`);
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

  // 3b) Account: attach an email/password to the guest account -----------
  const addEmailToAccount = async () => {
    if (!getBeam()) return append('Add email: connect to Beamable first');
    const email = accountEmail.trim();
    if (!email || !accountPassword)
      return append('Add email: enter an email and a password first');
    append('addCredentials …');
    try {
      const acct = await addEmail(email, accountPassword);
      append(`Email attached to account: ${acct.email ?? email}`);
    } catch (e) {
      append(`Add email error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  // 3b-ii) Opt in / out of a message rail (email / in-game) via the backend endpoint.
  const setRailOptIn = async (
    federationId: 'email' | 'ingame',
    optIn: boolean,
  ) => {
    if (!getBeam()) return append(`${federationId}: connect to Beamable first`);
    append(`message-rail/${optIn ? 'register' : 'unregister'} (${federationId}) …`);
    try {
      const res = optIn
        ? await registerRail(federationId)
        : await unregisterRail(federationId);
      append(
        `${federationId} ${optIn ? 'opt-in' : 'opt-out'} → ${res.success ? 'ok' : 'failed'}${res.message ? `: ${res.message}` : ''}`,
      );
    } catch (e) {
      append(`${federationId} error: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  // 3c) In-game messages: read the player's mailbox (InGameRailService delivers here) --
  const refreshInbox = async () => {
    if (!getBeam()) return append('In-game messages: connect to Beamable first');
    append('Loading in-game messages …');
    try {
      const msgs = await listInGameMessages();
      setMessages(msgs);
      append(`In-game messages: ${msgs.length}`);
    } catch (e) {
      append(`Inbox error: ${e instanceof Error ? e.message : String(e)}`);
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
    <View style={styles.root}>
    <ScrollView style={styles.scroll} contentContainerStyle={styles.container}>
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
            cid/pid not set — edit src/beam/config.ts. (host:{' '}
            {String(BEAM_CONFIG.host)})
          </Text>
        )}
        <Button label="Connect to Beamable" onPress={connectBeam} />
      </Section>

      {/* Account · add email (attach a login to the guest account) */}
      <Section title="Account · add email">
        <Text style={styles.hint}>
          Connect first (guest login), then attach an email + password so the account can be
          recovered / logged into later. Calls beam.account.addCredentials → POST
          /basic/accounts/register.
        </Text>
        <TextInput
          style={styles.input}
          placeholder="Email (e.g. rn-demo@example.com)"
          autoCapitalize="none"
          autoCorrect={false}
          keyboardType="email-address"
          value={accountEmail}
          onChangeText={setAccountEmail}
        />
        <TextInput
          style={styles.input}
          placeholder="Password"
          autoCapitalize="none"
          autoCorrect={false}
          secureTextEntry
          value={accountPassword}
          onChangeText={setAccountPassword}
        />
        <Button label="Add email to account" onPress={addEmailToAccount} />
        <Text style={styles.hint}>
          Email delivery is opt-in. Add an email above first, then opt in — the backend routes
          campaigns to the `email` rail (POST /message-rail/register), which resolves your
          address server-side at send time.
        </Text>
        <Button label="Opt in to email" onPress={() => setRailOptIn('email', true)} />
        <Button label="Opt out of email" onPress={() => setRailOptIn('email', false)} />
      </Section>

      {/* 2 · Permission & remote registration (native SDK) */}
      <Section title={`2 · Permission & remote registration (native ${platformLabel})`}>
        {nativeSupported ? (
          <>
            <Text style={styles.hint}>
              The unified `@beamable/notifications-react-native` SDK (
              {isAndroidHost ? 'Android AAR' : 'iOS xcframework'} via autolinking).
              Request permission, then register for remote push — the device token
              arrives on the tokenReceived event and is auto-registered with the backend
              `push` message rail below. Remote push needs a physical device
              + {remoteProvider} credentials on your realm.
            </Text>
            {push.permission && (
              <Text style={styles.hint}>
                Permission status: {push.permission.status}
                {push.permission.granted ? ' · granted' : ''}
              </Text>
            )}
            <Button label="Request permission" onPress={beamAskPermission} />
            <Button label={`Register for remote (${remoteProvider})`} onPress={beamRegisterRemote} />
            <Button label="Fire local now → Details #777" onPress={beamFireLocal} />
            <Button label="Fire local in 10s (background & tap) → #888" onPress={beamFireDelayed} />
          </>
        ) : (
          <Text style={styles.hint}>Native module not available on this platform.</Text>
        )}
      </Section>

      {/* 3 · Device registration via the backend `push` message rail */}
      <Section title="3 · Device registration (push rail)">
        {nativeSupported ? (
          <>
            <Text style={styles.hint}>
              Opt in registers this device's {remoteProvider} token with the backend `push`
              message rail (POST /message-rail/register) so the realm can target it; opt out
              unregisters the player (POST /message-rail/unregister). Steps: Connect to
              Beamable → Register for remote (section 2) → Opt in to push. Delivery is driven
              from the Portal Campaign Builder.
            </Text>
            <Text style={styles.hint}>
              {remoteProvider} token: {pushToken ? `${pushToken.slice(0, 12)}…` : 'none yet (Register for remote in section 2)'}
            </Text>
            <Button label="Opt in to push (register this device)" onPress={registerThisDevice} />
            <Button label="Opt out of push (unregister this device)" onPress={optOutOfPush} />
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

      {/* In-game messages — the player's Beamable mailbox (InGameRailService rail) */}
      <Section title="In-game messages">
        <Text style={styles.hint}>
          Reads this player's Beamable mailbox. Campaigns that target the in-game rail are
          delivered here by the InGameRailService (POST /basic/mail/bulk). Connect first, then
          refresh — send a message from the Portal Campaign Builder to see it appear.
        </Text>
        <Text style={styles.hint}>
          In-game delivery is opt-in — opt in so campaigns targeting the `ingame` rail reach
          your mailbox (POST /message-rail/register).
        </Text>
        <Button label="Opt in to in-game delivery" onPress={() => setRailOptIn('ingame', true)} />
        <Button label="Opt out of in-game delivery" onPress={() => setRailOptIn('ingame', false)} />
        <Button label="Refresh inbox" onPress={refreshInbox} />
        {messages.length === 0 ? (
          <Text style={styles.hint}>No in-game messages.</Text>
        ) : (
          messages.map((m) => (
            <View key={String(m.id)} style={styles.messageCard}>
              <Text style={styles.messageSubject}>
                {m.subject || '(no subject)'}
              </Text>
              {!!m.body && <Text style={styles.messageBody}>{m.body}</Text>}
              <Text style={styles.messageMeta}>
                {m.state}
                {m.category ? ` · ${m.category}` : ''}
              </Text>
            </View>
          ))
        )}
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

    </ScrollView>

      {/* Activity console — pinned to the bottom by flex order (below the flex:1 ScrollView),
          collapsible. Because it's a flow sibling, the scroll area shrinks when it opens
          instead of the panel overlapping content. */}
      <View style={styles.console}>
        {logOpen && (
          <View style={styles.consolePanel}>
            <ScrollView contentContainerStyle={styles.consolePanelContent}>
              {log.length === 0 ? (
                <Text style={styles.consoleEmpty}>No activity yet.</Text>
              ) : (
                log.map((entry) => (
                  <Text key={entry.id} style={styles.consoleLine}>
                    {entry.text}
                  </Text>
                ))
              )}
            </ScrollView>
          </View>
        )}
        <View style={styles.consoleBar}>
          <Pressable
            style={styles.consoleBarToggle}
            onPress={() => setLogOpen((v) => !v)}
          >
            <Text style={styles.consoleBarText}>
              {logOpen ? '▾' : '▸'}  Activity log ({log.length})
            </Text>
          </Pressable>
          {log.length > 0 && (
            <Pressable onPress={() => setLog([])} hitSlop={8}>
              <Text style={styles.consoleClear}>Clear</Text>
            </Pressable>
          )}
        </View>
      </View>
    </View>
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
  root: { flex: 1 },
  // The scroll view fills all space above the console; the console takes its natural height
  // below it. Opening the log panel shrinks this viewport rather than covering content.
  scroll: { flex: 1 },
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
  messageCard: {
    backgroundColor: 'white',
    borderRadius: 10,
    padding: 12,
    gap: 4,
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  messageSubject: { fontSize: 14, fontWeight: '600', color: '#111827' },
  messageBody: { fontSize: 13, color: '#374151' },
  messageMeta: { fontSize: 11, color: '#9ca3af', fontFamily: 'Courier' },
  // Console pinned to the bottom by flex order (a flow sibling after the flex:1 ScrollView),
  // so it never overlaps the scroll content.
  console: {},
  consolePanel: {
    maxHeight: 260,
    backgroundColor: '#0b1021',
    borderTopWidth: 1,
    borderTopColor: '#1f2937',
  },
  consolePanelContent: { padding: 12, gap: 4 },
  consoleEmpty: { color: '#6b7280', fontSize: 12, fontFamily: 'Courier' },
  consoleLine: { color: '#e5e7eb', fontSize: 12, fontFamily: 'Courier' },
  consoleBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#111827',
    paddingHorizontal: 14,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: '#374151',
  },
  consoleBarToggle: { flex: 1 },
  consoleBarText: { color: '#e5e7eb', fontSize: 13, fontWeight: '700' },
  consoleClear: { color: '#f87171', fontWeight: '700', fontSize: 13 },
});
