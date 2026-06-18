import { useEffect, useRef, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';

import {
  BEAMABLE_EVENTS,
  type BeamableEvent,
  addBeamableListener,
  addWebhookListener,
  cancelAllLocal,
  cancelLocal,
  clearDelivered,
  getDeliveryReceipts,
  getPending,
  getPermissionStatus,
  initBeamableNotifications,
  isBeamableNotificationsSupported,
  registerForRemote,
  requestBeamablePermission,
  scheduleLocal,
  setBadge,
  unregisterForRemote,
} from '../src/notifications/beamableNotifications';

// A synthetic log entry for the app-side webhook POST (not a native SDK event).
const WEBHOOK_EVENT = 'webhookPost';
type LogEvent = BeamableEvent | typeof WEBHOOK_EVENT;

/** One captured callback firing (or an app-side webhook POST result). */
type EventEntry = {
  key: number;
  time: string;
  event: LogEvent;
  data: unknown;
};

// Loose signature for subscribing in a loop (the typed `addBeamableListener`
// correlates payload to event name, which a generic loop can't express).
const subscribe = addBeamableListener as unknown as (
  event: BeamableEvent,
  handler: (data: unknown) => void,
) => { remove: () => void };

// A colour per event so firings are easy to tell apart in the log.
const EVENT_COLOR: Record<LogEvent, string> = {
  permissionResult: '#2563eb',
  tokenReceived: '#16a34a',
  tokenError: '#dc2626',
  notificationPresented: '#7c3aed',
  notificationReceived: '#0891b2',
  notificationTapped: '#ea580c',
  pendingNotifications: '#ca8a04',
  deliveryReceipts: '#0d9488',
  [WEBHOOK_EVENT]: '#db2777',
};

export default function Callbacks() {
  const [events, setEvents] = useState<EventEntry[]>([]);
  const counter = useRef(0);

  // Subscribe to every SDK event and capture each firing with its full payload.
  useEffect(() => {
    if (!isBeamableNotificationsSupported) return;

    initBeamableNotifications();

    const push = (event: LogEvent, data: unknown) =>
      setEvents((prev) =>
        [
          { key: (counter.current += 1), time: now(), event, data },
          ...prev,
        ].slice(0, 100),
      );

    const subs = BEAMABLE_EVENTS.map((event) =>
      subscribe(event, (data) => push(event, data)),
    );

    // App-side webhook POST outcomes (fired for local notifications etc.).
    const webhookSub = addWebhookListener((r) => push(WEBHOOK_EVENT, r));

    return () => {
      subs.forEach((s) => s.remove());
      webhookSub.remove();
    };
  }, []);

  if (!isBeamableNotificationsSupported) {
    return (
      <View style={styles.center}>
        <StatusBar style="auto" />
        <Text style={styles.hint}>
          Beamable Notifications is iOS only — the native module is not available
          on this platform.
        </Text>
      </View>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <StatusBar style="auto" />

      <Text style={styles.h1}>Notification callbacks</Text>
      <Text style={styles.subtitle}>
        Trigger an action; every event the SDK emits is captured below with its
        payload. (Token/receipts need a physical device + APNs.)
      </Text>

      {/* Actions that cause callbacks */}
      <Section title="Permission → permissionResult">
        <Row>
          <Button label="Request permission" onPress={requestBeamablePermission} />
          <Button label="Get status" onPress={getPermissionStatus} />
        </Row>
      </Section>

      <Section title="Local → presented / received / tapped">
        <Text style={styles.hint}>
          Keep the app in the FOREGROUND to see notificationPresented; tap the
          banner to see notificationTapped.
        </Text>
        <Row>
          <Button
            label="Fire now (#101)"
            onPress={() =>
              scheduleLocal({
                id: 'cb-101',
                title: 'Callback test #101',
                body: 'Fired immediately — watch the events below',
                trigger: { type: 'immediate' },
                userInfo: { source: 'callbacks-screen', n: 101 },
              })
            }
          />
          <Button
            label="Fire in 3s (#102)"
            onPress={() =>
              scheduleLocal({
                id: 'cb-102',
                title: 'Callback test #102',
                body: 'Fires in 3s — background then tap to test tapped',
                trigger: { type: 'timeInterval', seconds: 3 },
                userInfo: { source: 'callbacks-screen', n: 102 },
              })
            }
          />
        </Row>
        <Row>
          <Button label="Cancel #102" onPress={() => cancelLocal('cb-102')} />
          <Button label="Cancel all" onPress={cancelAllLocal} />
        </Row>
      </Section>

      <Section title="Pending → pendingNotifications">
        <Button label="Get pending" onPress={getPending} />
      </Section>

      <Section title="Remote → tokenReceived / tokenError">
        <Row>
          <Button label="Register for remote" onPress={registerForRemote} />
          <Button label="Unregister" onPress={unregisterForRemote} />
        </Row>
      </Section>

      <Section title="Analytics → deliveryReceipts">
        <Button label="Get delivery receipts" onPress={getDeliveryReceipts} />
      </Section>

      <Section title="Badge (no callback)">
        <Row>
          <Button label="Set badge 5" onPress={() => setBadge(5)} />
          <Button label="Clear badge" onPress={() => setBadge(0)} />
          <Button label="Clear delivered" onPress={clearDelivered} />
        </Row>
      </Section>

      {/* The live event log */}
      <View style={styles.logHeader}>
        <Text style={styles.sectionTitle}>
          Events captured ({events.length})
        </Text>
        {events.length > 0 && (
          <Pressable onPress={() => setEvents([])}>
            <Text style={styles.clear}>Clear</Text>
          </Pressable>
        )}
      </View>

      {events.length === 0 ? (
        <Text style={styles.hint}>
          No events yet. Trigger an action above.
        </Text>
      ) : (
        events.map((e) => (
          <View key={e.key} style={styles.eventCard}>
            <View style={styles.eventTop}>
              <View
                style={[styles.badge, { backgroundColor: EVENT_COLOR[e.event] }]}
              >
                <Text style={styles.badgeText}>{e.event}</Text>
              </View>
              <Text style={styles.eventTime}>{e.time}</Text>
            </View>
            <Text style={styles.json}>{pretty(e.data)}</Text>
          </View>
        ))
      )}
    </ScrollView>
  );
}

function now(): string {
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

function Row(props: { children: React.ReactNode }) {
  return <View style={styles.row}>{props.children}</View>;
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

const styles = StyleSheet.create({
  container: { padding: 20, paddingTop: 24, gap: 14 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  h1: { fontSize: 24, fontWeight: '700' },
  subtitle: { fontSize: 13, color: '#6b7280', marginTop: -6 },
  section: {
    backgroundColor: '#f9fafb',
    borderRadius: 12,
    padding: 14,
    gap: 10,
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  sectionTitle: { fontSize: 15, fontWeight: '600', color: '#111827' },
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  button: {
    backgroundColor: '#5A31F4',
    borderRadius: 10,
    paddingVertical: 10,
    paddingHorizontal: 12,
    flexGrow: 1,
  },
  buttonPressed: { opacity: 0.7 },
  buttonText: { color: 'white', fontWeight: '600', textAlign: 'center', fontSize: 13 },
  logHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: 6,
  },
  clear: { color: '#dc2626', fontWeight: '600', fontSize: 13 },
  eventCard: {
    backgroundColor: '#0b1021',
    borderRadius: 10,
    padding: 12,
    gap: 8,
  },
  eventTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  badgeText: { color: 'white', fontWeight: '700', fontSize: 12 },
  eventTime: { color: '#9ca3af', fontSize: 12, fontFamily: 'Courier' },
  json: { color: '#e5e7eb', fontSize: 12, fontFamily: 'Courier' },
  hint: { color: '#6b7280', fontSize: 12 },
});
