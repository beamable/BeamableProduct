import { Pressable, StyleSheet, Text, View } from 'react-native';

import { colors, space } from './theme';

export type Tab<K extends string> = { key: K; label: string };

/**
 * A segmented control for switching between views inside a panel. Currently drives the
 * debug console's Activity / Native events streams.
 *
 * Styled for the dark console (`tone="dark"`); `tone="light"` is available for card use.
 */
export default function TabStrip<K extends string>({
  tabs,
  active,
  onChange,
  tone = 'dark',
}: {
  tabs: readonly Tab<K>[];
  active: K;
  onChange: (key: K) => void;
  tone?: 'dark' | 'light';
}) {
  const dark = tone === 'dark';
  return (
    <View style={[styles.strip, dark ? styles.stripDark : styles.stripLight]}>
      {tabs.map((tab) => {
        const isActive = tab.key === active;
        return (
          <Pressable
            key={tab.key}
            style={({ pressed }) => [
              styles.tab,
              isActive && (dark ? styles.tabActiveDark : styles.tabActiveLight),
              pressed && styles.pressed,
            ]}
            onPress={() => onChange(tab.key)}
            accessibilityRole="tab"
            accessibilityState={{ selected: isActive }}
          >
            <Text
              style={[
                styles.label,
                dark ? styles.labelDark : styles.labelLight,
                isActive && (dark ? styles.labelActiveDark : styles.labelActiveLight),
              ]}
              numberOfLines={1}
            >
              {tab.label}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  strip: { flexDirection: 'row', borderBottomWidth: 1 },
  stripDark: { backgroundColor: colors.console, borderBottomColor: colors.consoleBorder },
  stripLight: { backgroundColor: colors.surface, borderBottomColor: colors.surfaceBorder },
  tab: {
    flex: 1,
    paddingVertical: space.md,
    paddingHorizontal: space.md,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabActiveDark: { borderBottomColor: colors.consoleInk },
  tabActiveLight: { borderBottomColor: colors.primary },
  pressed: { opacity: 0.6 },
  label: { fontSize: 12, fontWeight: '600', textAlign: 'center' },
  labelDark: { color: colors.consoleMuted },
  labelLight: { color: colors.muted },
  labelActiveDark: { color: colors.consoleInk },
  labelActiveLight: { color: colors.primary },
});
