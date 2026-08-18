import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { BEAM_CONFIG, isConfigured } from '../beam/config';
import type { BeamStatus } from '../beam/beamClient';
import { useBeam } from '../state/beamContext';
import { colors, radius, space } from './theme';

const DOT: Record<BeamStatus['state'], string> = {
  idle: colors.statusIdle,
  connecting: colors.statusConnecting,
  ready: colors.statusReady,
  error: colors.statusError,
};

/**
 * The always-visible connection strip above the tabs. Replaces the old "1 · Beamable Web SDK"
 * section and its Connect button: init is automatic, so the only action left is retrying a
 * failed connection.
 */
export default function ConnectionBar() {
  const { status, retry } = useBeam();
  // This bar is the topmost element in the app — it sits above the navigator, so no header
  // applies the status-bar inset for us. Pad by it here (rather than on a wrapper) so the
  // bar's own background fills the notch/status-bar strip instead of leaving a bare band.
  const insets = useSafeAreaInsets();

  return (
    <View style={[styles.bar, { paddingTop: insets.top + space.md }]}>
      <View style={styles.row}>
        {status.state === 'connecting' ? (
          <ActivityIndicator size="small" />
        ) : (
          <View style={[styles.dot, { backgroundColor: DOT[status.state] }]} />
        )}
        <Text style={styles.text} numberOfLines={2}>
          {label(status)}
        </Text>
        {status.state === 'error' && (
          <Pressable
            style={({ pressed }) => [styles.retry, pressed && styles.pressed]}
            onPress={retry}
            accessibilityRole="button"
          >
            <Text style={styles.retryText}>Retry connection</Text>
          </Pressable>
        )}
      </View>
      {!isConfigured() && (
        <Text style={styles.warn}>
          cid/pid not set — edit .beamable/config.beam.json. (host: {String(BEAM_CONFIG.host)})
        </Text>
      )}
    </View>
  );
}

function label(status: BeamStatus): string {
  switch (status.state) {
    case 'idle':
      return 'Not connected';
    case 'connecting':
      return 'Connecting to Beamable…';
    case 'ready':
      return `Ready · player ${status.playerId}`;
    case 'error':
      return `Error · ${status.message}`;
  }
}

const styles = StyleSheet.create({
  bar: {
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.surfaceBorder,
    paddingHorizontal: space.lg,
    // paddingTop is applied inline from the safe-area inset.
    paddingBottom: space.md,
    gap: space.xs,
  },
  row: { flexDirection: 'row', alignItems: 'center', gap: space.sm },
  dot: { width: 10, height: 10, borderRadius: 5 },
  text: { flex: 1, fontSize: 13, color: colors.inkSoft },
  retry: {
    backgroundColor: colors.primary,
    borderRadius: radius.md,
    paddingHorizontal: space.md,
    paddingVertical: 6,
  },
  pressed: { opacity: 0.7 },
  retryText: { color: 'white', fontWeight: '700', fontSize: 12 },
  warn: { color: colors.warn, fontSize: 11 },
});
