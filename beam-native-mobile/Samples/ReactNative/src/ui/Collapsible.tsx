import { useState, type ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { colors, radius, space } from './theme';

/**
 * An expandable group inside a section. Used for the Push page's Debug controls (local
 * notifications + Live Activities), which are demo-only and shouldn't crowd the real
 * permission / registration / rail flow.
 */
export default function Collapsible({
  title,
  defaultOpen = false,
  children,
}: {
  title: string;
  defaultOpen?: boolean;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <View style={styles.wrap}>
      <Pressable
        style={({ pressed }) => [styles.header, pressed && styles.pressed]}
        onPress={() => setOpen((v) => !v)}
        accessibilityRole="button"
        accessibilityState={{ expanded: open }}
      >
        <Text style={styles.title}>
          {open ? '▾' : '▸'}  {title}
        </Text>
      </Pressable>
      {open && <View style={styles.body}>{children}</View>}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    borderWidth: 1,
    borderColor: colors.surfaceBorder,
    borderRadius: radius.lg,
    backgroundColor: colors.card,
    overflow: 'hidden',
  },
  header: { paddingVertical: space.md, paddingHorizontal: space.lg },
  pressed: { opacity: 0.6 },
  title: { fontSize: 14, fontWeight: '600', color: colors.ink },
  body: {
    padding: space.lg,
    paddingTop: 0,
    gap: space.md,
  },
});
