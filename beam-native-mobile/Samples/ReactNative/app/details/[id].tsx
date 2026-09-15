import { Stack, useLocalSearchParams } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';

/**
 * Deep-link target screen.
 *
 * Reached via:
 *   - `beamrnsample://details/<id>` (external URL / push / `simctl openurl`)
 *   - tapping a local notification whose data.path = `/details/<id>`
 *   - in-app navigation
 */
export default function Details() {
  const { id } = useLocalSearchParams<{ id: string }>();

  return (
    <View style={styles.container}>
      <Stack.Screen options={{ title: `Details #${id ?? '?'}` }} />
      <Text style={styles.badge}>DEEP LINK TARGET</Text>
      <Text style={styles.h1}>Details Screen</Text>
      <Text style={styles.row}>
        id param = <Text style={styles.mono}>{String(id)}</Text>
      </Text>
      <Text style={styles.note}>
        You arrived here from a deep link, a tapped notification, or in-app
        navigation. The `id` above was parsed from the route.
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 24, gap: 12, justifyContent: 'center' },
  badge: {
    alignSelf: 'flex-start',
    backgroundColor: '#ede9fe',
    color: '#5A31F4',
    fontWeight: '700',
    fontSize: 11,
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 6,
    overflow: 'hidden',
  },
  h1: { fontSize: 26, fontWeight: '700' },
  row: { fontSize: 16, color: '#374151' },
  mono: { fontFamily: 'Courier', fontWeight: '700', color: '#111827' },
  note: { fontSize: 14, color: '#6b7280', marginTop: 8 },
});
