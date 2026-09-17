import type { ReactNode } from 'react';
import { ScrollView, StyleSheet } from 'react-native';

import { space } from './theme';

/**
 * The standard scrolling page body. Every tab wraps its sections in this so padding and
 * inter-section spacing are identical across pages.
 */
export default function Screen({ children }: { children: ReactNode }) {
  return (
    <ScrollView style={styles.scroll} contentContainerStyle={styles.content}>
      {children}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scroll: { flex: 1 },
  content: { padding: space.xl, paddingTop: space.lg, gap: space.lg },
});
