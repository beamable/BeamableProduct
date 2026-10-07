import { ActivityIndicator, Pressable, StyleSheet } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';

import { colors, space } from './theme';

/**
 * The ↻ accessory for a `Section`'s `right` slot, swapped for a spinner while a read is in
 * flight.
 *
 * Lifted here because it had been written twice — inline in the Offers tab and as a
 * component-local closure in Segments — and the Offers tab now needs a third.
 */
export default function RefreshButton({
  busy,
  onPress,
  label,
}: {
  busy: boolean;
  onPress: () => void;
  /** Accessibility label, e.g. "Refresh campaign offers". */
  label: string;
}) {
  if (busy) return <ActivityIndicator size="small" />;

  return (
    <Pressable
      onPress={onPress}
      hitSlop={10}
      style={({ pressed }) => [styles.refresh, pressed && styles.pressed]}
      accessibilityRole="button"
      accessibilityLabel={label}
    >
      <Ionicons name="refresh" size={18} color={colors.primary} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  refresh: { padding: space.xs },
  pressed: { opacity: 0.5 },
});
