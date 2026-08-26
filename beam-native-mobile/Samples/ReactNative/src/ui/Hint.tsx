import type { ReactNode } from 'react';
import { StyleSheet, Text } from 'react-native';

import { colors, mono } from './theme';

/** Explanatory copy under a section title: which SDK call runs, which endpoint it hits. */
export function Hint({ children }: { children: ReactNode }) {
  return <Text style={styles.hint}>{children}</Text>;
}

/** A misconfiguration the user has to fix before anything works (missing cid/pid). */
export function Warn({ children }: { children: ReactNode }) {
  return <Text style={styles.warn}>{children}</Text>;
}

/** A read-only `label: value` row, e.g. the current APNs/FCM token or account email. */
export function Value({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Text style={styles.value}>
      <Text style={styles.valueLabel}>{label}: </Text>
      {children}
    </Text>
  );
}

const styles = StyleSheet.create({
  hint: { color: colors.muted, fontSize: 12, fontFamily: mono },
  warn: { color: colors.warn, fontSize: 12 },
  value: { color: colors.inkSoft, fontSize: 13, fontFamily: mono },
  valueLabel: { color: colors.muted },
});
