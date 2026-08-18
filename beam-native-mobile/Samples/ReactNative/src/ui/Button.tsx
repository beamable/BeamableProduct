import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { colors, radius, space } from './theme';

export type ButtonVariant = 'primary' | 'secondary';

/**
 * The sample's only button. `secondary` is an outlined variant for destructive-ish or
 * lower-traffic actions (opt-out, clear native auth) so a section of six primaries doesn't
 * read as six equally-weighted choices.
 *
 * `loading` shows an inline spinner and blocks presses — see `AsyncButton`, which drives it
 * automatically for anything that awaits the backend.
 */
export default function Button({
  label,
  onPress,
  variant = 'primary',
  disabled = false,
  loading = false,
}: {
  label: string;
  onPress: () => void;
  variant?: ButtonVariant;
  disabled?: boolean;
  loading?: boolean;
}) {
  const secondary = variant === 'secondary';
  const blocked = disabled || loading;
  return (
    <Pressable
      style={({ pressed }) => [
        styles.button,
        secondary && styles.secondary,
        pressed && styles.pressed,
        blocked && styles.disabled,
      ]}
      onPress={onPress}
      disabled={blocked}
      accessibilityRole="button"
      accessibilityState={{ disabled: blocked, busy: loading }}
    >
      <View style={styles.content}>
        {loading && (
          <ActivityIndicator size="small" color={secondary ? colors.primary : 'white'} />
        )}
        <Text style={[styles.text, secondary && styles.secondaryText]}>{label}</Text>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    backgroundColor: colors.primary,
    borderRadius: radius.lg,
    paddingVertical: 12,
    paddingHorizontal: space.lg,
    borderWidth: 1,
    borderColor: colors.primary,
  },
  secondary: { backgroundColor: 'transparent' },
  pressed: { opacity: 0.7 },
  disabled: { opacity: 0.5 },
  content: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: space.sm,
  },
  text: { color: 'white', fontWeight: '600', textAlign: 'center', flexShrink: 1 },
  secondaryText: { color: colors.primary },
});
