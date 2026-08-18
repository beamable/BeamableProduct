import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import type { BeamableEvent } from '@beamable/notifications-react-native';

import { useBeam } from '../state/beamContext';
import { useLogActions, useLogData, type EventEntry } from '../state/logContext';
import { exportLog, formatActivity, formatEvents, logFileName } from '../state/logExport';
import TabStrip from './TabStrip';
import { colors, mono, radius, space } from './theme';

/** A colour per native event so firings are easy to tell apart in the event stream. */
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
  liveActivityPushToStartToken: '#4f46e5',
  liveActivityUpdateToken: '#0369a1',
  liveActivityStarted: '#15803d',
  liveActivityCapability: '#9333ea',
};

type Stream = 'activity' | 'events';

/**
 * The quake-style console pinned to the bottom of every tab.
 *
 * Two streams behind a tab strip: **Activity** (human-readable feedback from button presses)
 * and **Native events** (the raw SDK event vocabulary with payloads). The event stream used to
 * live inside the main page's scroll, where a long JSON payload pushed everything else off
 * screen.
 *
 * It's a flow sibling of the navigator rather than an overlay, so opening it shrinks the page
 * viewport instead of covering content.
 */
export default function DebugConsole() {
  const { activity, events } = useLogData();
  const { append, clearActivity, clearEvents } = useLogActions();
  const { playerId } = useBeam();
  const [open, setOpen] = useState(false);
  const [stream, setStream] = useState<Stream>('activity');
  const [exporting, setExporting] = useState(false);
  // The console bar is the bottommost element, below the tab bar — so it, not the tab bar,
  // owns the gesture-bar inset. The tabs subtree is given zeroed top/bottom insets in
  // app/(tabs)/_layout.tsx so nothing pads twice.
  const insets = useSafeAreaInsets();

  const showingActivity = stream === 'activity';
  const count = showingActivity ? activity.length : events.length;
  const clear = showingActivity ? clearActivity : clearEvents;

  // Exports whichever stream is on screen — same scoping as Clear, so the button always acts
  // on what you're looking at.
  const exportCurrent = async () => {
    if (exporting) return;
    setExporting(true);
    try {
      const contents = showingActivity
        ? formatActivity(activity, playerId)
        : formatEvents(events, playerId);
      append(await exportLog(logFileName(showingActivity ? 'activity' : 'events'), contents));
    } catch (e) {
      append(`Log export failed: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setExporting(false);
    }
  };

  return (
    <View>
      {open && (
        <View style={styles.panel}>
          <TabStrip
            tabs={[
              { key: 'activity' as const, label: `Activity (${activity.length})` },
              { key: 'events' as const, label: `Native events (${events.length})` },
            ]}
            active={stream}
            onChange={setStream}
          />
          <ScrollView contentContainerStyle={styles.panelContent}>
            {showingActivity ? (
              activity.length === 0 ? (
                <Text style={styles.empty}>No activity yet.</Text>
              ) : (
                activity.map((entry) => (
                  <Text key={entry.id} style={styles.line}>
                    {entry.text}
                  </Text>
                ))
              )
            ) : events.length === 0 ? (
              <Text style={styles.empty}>
                No native events yet. Trigger an action — request permission, register for
                remote, or fire a local notification and tap it.
              </Text>
            ) : (
              events.map((e) => <EventCard key={e.key} entry={e} />)
            )}
          </ScrollView>
        </View>
      )}
      <View style={[styles.bar, { paddingBottom: insets.bottom + 12 }]}>
        <Pressable style={styles.toggle} onPress={() => setOpen((v) => !v)} hitSlop={8}>
          <Text style={styles.barText}>
            {open ? '▾' : '▸'}  {open && !showingActivity ? 'Native events' : 'Activity log'} (
            {open ? count : activity.length + events.length})
          </Text>
        </Pressable>
        {open && count > 0 && (
          <View style={styles.barActions}>
            <Pressable onPress={() => void exportCurrent()} hitSlop={8} disabled={exporting}>
              <Text style={[styles.export, exporting && styles.exportBusy]}>
                {exporting ? 'Exporting…' : 'Export .log'}
              </Text>
            </Pressable>
            <Pressable onPress={clear} hitSlop={8}>
              <Text style={styles.clear}>Clear</Text>
            </Pressable>
          </View>
        )}
      </View>
    </View>
  );
}

function EventCard({ entry }: { entry: EventEntry }) {
  return (
    <View style={styles.eventCard}>
      <View style={styles.eventTop}>
        <View style={[styles.badge, { backgroundColor: EVENT_COLOR[entry.event] }]}>
          <Text style={styles.badgeText}>{entry.event}</Text>
        </View>
        <Text style={styles.eventTime}>{entry.time}</Text>
      </View>
      <Text style={styles.json}>{pretty(entry.data)}</Text>
    </View>
  );
}

function pretty(data: unknown): string {
  try {
    return JSON.stringify(data, null, 2);
  } catch {
    return String(data);
  }
}

const styles = StyleSheet.create({
  panel: {
    maxHeight: 300,
    backgroundColor: colors.console,
    borderTopWidth: 1,
    borderTopColor: colors.consoleBorder,
  },
  panelContent: { padding: 12, gap: space.xs },
  empty: { color: colors.consoleMuted, fontSize: 12, fontFamily: mono },
  line: { color: colors.consoleInk, fontSize: 12, fontFamily: mono },
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: colors.consoleBar,
    paddingHorizontal: space.lg,
    // paddingBottom is applied inline from the safe-area inset.
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.consoleBarBorder,
  },
  toggle: { flex: 1 },
  barText: { color: colors.consoleInk, fontSize: 13, fontWeight: '700' },
  barActions: { flexDirection: 'row', alignItems: 'center', gap: space.lg },
  export: { color: colors.consoleInk, fontWeight: '700', fontSize: 13 },
  exportBusy: { color: colors.consoleMuted },
  clear: { color: colors.consoleClear, fontWeight: '700', fontSize: 13 },
  eventCard: {
    backgroundColor: colors.consoleBorder,
    borderRadius: radius.lg,
    padding: 12,
    gap: space.sm,
    marginBottom: space.xs,
  },
  eventTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  badge: { borderRadius: radius.sm, paddingHorizontal: space.sm, paddingVertical: 3 },
  badgeText: { color: 'white', fontWeight: '700', fontSize: 12 },
  eventTime: { color: colors.mutedSoft, fontSize: 12, fontFamily: mono },
  json: { color: colors.consoleInk, fontSize: 12, fontFamily: mono },
});
