import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { useLogActions } from '../state/logContext';
import { colors, mono, radius, space } from './theme';

type Status =
  | { state: 'idle' }
  | { state: 'pending' }
  | { state: 'ok'; message: string }
  | { state: 'error'; message: string };

/**
 * One player stat, with the add button that bumps it.
 *
 * A card, not a row: the point of the Segments tab is that a stat is only meaningful next to
 * the segment rule it feeds, so the description and an example rule live here with the value
 * and the button that changes it.
 *
 * The button is a compact pill rather than an `AsyncButton` — a column of four full-width
 * primaries would bury the values — but it keeps `AsyncButton`'s contract: `add` resolves with
 * the message to show, or throws, and either way the outcome is mirrored into the Activity log.
 */
export default function StatCard({
  statKey,
  label,
  value,
  description,
  rule,
  step,
  add,
}: {
  statKey: string;
  label: string;
  /** Current value, or null while the first read is in flight. */
  value: string | null;
  description: string;
  /** Example Portal rule over this key. */
  rule: string;
  step: number;
  add: () => Promise<string>;
}) {
  const { append } = useLogActions();
  const [status, setStatus] = useState<Status>({ state: 'idle' });
  // Guards a state update after unmount when the tab is swapped mid-request.
  const mounted = useRef(true);
  const inFlight = useRef(false);

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  const onPress = useCallback(() => {
    if (inFlight.current) return;
    inFlight.current = true;
    setStatus({ state: 'pending' });

    void (async () => {
      try {
        const message = await add();
        append(message);
        if (mounted.current) setStatus({ state: 'ok', message });
      } catch (e) {
        const message = e instanceof Error ? e.message : String(e);
        append(`${statKey} +${step}: ${message}`);
        if (mounted.current) setStatus({ state: 'error', message });
      } finally {
        inFlight.current = false;
      }
    })();
  }, [add, append, statKey, step]);

  const pending = status.state === 'pending';

  return (
    <View style={styles.card}>
      <View style={styles.header}>
        <View style={styles.headerText}>
          <Text style={styles.label}>{label}</Text>
          <Text style={styles.key}>{statKey}</Text>
        </View>
        <Pressable
          style={({ pressed }) => [styles.add, pressed && styles.pressed, pending && styles.busy]}
          onPress={onPress}
          disabled={pending}
          hitSlop={6}
          accessibilityRole="button"
          accessibilityLabel={`Add ${step} to ${statKey}`}
          accessibilityState={{ disabled: pending, busy: pending }}
        >
          {pending ? (
            <ActivityIndicator size="small" color="white" />
          ) : (
            <Text style={styles.addText}>+{step}</Text>
          )}
        </Pressable>
      </View>

      <Text style={styles.value}>{value ?? '—'}</Text>
      <Text style={styles.description}>{description}</Text>
      <Text style={styles.rule}>{rule}</Text>

      {status.state === 'ok' && (
        <Text style={[styles.result, styles.ok]} selectable>
          ✓ {status.message}
        </Text>
      )}
      {status.state === 'error' && (
        <Text style={[styles.result, styles.error]} selectable>
          ✕ {status.message}
        </Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.card,
    borderRadius: radius.lg,
    padding: 12,
    gap: space.xs,
    borderWidth: 1,
    borderColor: colors.surfaceBorder,
  },
  header: { flexDirection: 'row', alignItems: 'flex-start', gap: space.sm },
  headerText: { flex: 1, gap: 2 },
  label: { fontSize: 14, fontWeight: '600', color: colors.ink },
  key: { fontSize: 11, color: colors.mutedSoft, fontFamily: mono },
  add: {
    backgroundColor: colors.primary,
    borderRadius: radius.md,
    paddingVertical: 6,
    paddingHorizontal: space.md,
    minWidth: 52,
    alignItems: 'center',
    justifyContent: 'center',
  },
  pressed: { opacity: 0.7 },
  busy: { opacity: 0.6 },
  addText: { color: 'white', fontWeight: '700', fontSize: 13 },
  // The value is the thing you came to read after pressing add — size it accordingly.
  value: { fontSize: 26, fontWeight: '700', color: colors.ink, fontFamily: mono },
  description: { fontSize: 12, color: colors.inkSoft },
  rule: { fontSize: 11, color: colors.muted, fontFamily: mono },
  result: {
    fontSize: 12,
    fontFamily: mono,
    paddingVertical: 6,
    paddingHorizontal: space.md,
    borderRadius: radius.md,
    borderWidth: 1,
    marginTop: space.xs,
  },
  ok: { color: colors.okInk, backgroundColor: colors.okBg, borderColor: colors.okBorder },
  error: {
    color: colors.errorInk,
    backgroundColor: colors.errorBg,
    borderColor: colors.errorBorder,
  },
});
