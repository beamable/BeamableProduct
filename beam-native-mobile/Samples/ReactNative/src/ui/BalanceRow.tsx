import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';

import { colors, mono, radius, space } from './theme';

/**
 * One currency: its label, the amount, and an optional signed change.
 *
 * A row rather than a `StatCard` because the wallet is a list you scan, not a set of controls —
 * a column of cards would bury the offers below it. The change chip is the point: after a
 * purchase you want to see `-1,200` next to the currency you spent and `+500` next to the one you
 * gained, without doing the arithmetic yourself.
 *
 * `onAdd` puts one control back on the row — a ⊕ that tops this currency up. It is an accessory
 * rather than a `Button` for the same reason the section's ↻ is: a full-width button per currency
 * would turn the list back into a column of cards. Rows given no `onAdd` render exactly as before.
 */
export default function BalanceRow({
  label,
  amount,
  delta,
  tone,
  onAdd,
  adding = false,
}: {
  label: string;
  /** Pre-formatted — the caller owns grouping, because amounts are bigint. */
  amount: string;
  /** Signed, pre-formatted: `+500`, `-1,200`. Omitted when nothing changed. */
  delta?: string;
  tone?: 'up' | 'down';
  /** Grant more of this currency. Omit and the row carries no control at all. */
  onAdd?: () => void;
  /** True while this row's grant is in flight — swaps the ⊕ for a spinner. */
  adding?: boolean;
}) {
  return (
    <View style={styles.row}>
      <Text style={styles.label} numberOfLines={1}>
        {label}
      </Text>
      <View style={styles.amounts}>
        {delta ? (
          <Text style={[styles.delta, tone === 'down' ? styles.down : styles.up]}>{delta}</Text>
        ) : null}
        <Text style={styles.amount}>{amount}</Text>
        {onAdd &&
          (adding ? (
            <ActivityIndicator size="small" />
          ) : (
            <Pressable
              onPress={onAdd}
              hitSlop={10}
              style={({ pressed }) => [styles.add, pressed && styles.pressed]}
              accessibilityRole="button"
              accessibilityLabel={`Add ${label}`}
            >
              <Ionicons name="add-circle-outline" size={20} color={colors.primary} />
            </Pressable>
          ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: space.sm,
    paddingVertical: 4,
  },
  label: { color: colors.inkSoft, fontSize: 13, fontFamily: mono, flexShrink: 1 },
  amounts: { flexDirection: 'row', alignItems: 'center', gap: space.sm },
  amount: { color: colors.ink, fontSize: 18, fontWeight: '700', fontFamily: mono },
  delta: {
    fontSize: 12,
    fontFamily: mono,
    fontWeight: '700',
    paddingVertical: 2,
    paddingHorizontal: 6,
    borderRadius: radius.sm,
    overflow: 'hidden',
  },
  up: { color: colors.okInk, backgroundColor: colors.okBg },
  down: { color: colors.errorInk, backgroundColor: colors.errorBg },
  add: { padding: 2 },
  pressed: { opacity: 0.5 },
});
