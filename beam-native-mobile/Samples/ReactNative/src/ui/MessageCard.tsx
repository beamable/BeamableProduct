import { StyleSheet, Text, View } from 'react-native';

import { colors, mono, radius, space } from './theme';

/**
 * A titled entry with an optional body and a monospaced meta line.
 *
 * Two callers: the In-game tab's mailbox (the `ingame` rail's last mile — fields from the SDK
 * `Message` schema via `listInGameMessages()`), and the Segments tab's membership and
 * transition rows.
 */
export default function MessageCard({
  subject,
  body,
  meta,
}: {
  subject?: string | null;
  body?: string | null;
  meta?: string | null;
}) {
  return (
    <View style={styles.card}>
      <Text style={styles.subject}>{subject || '(no subject)'}</Text>
      {!!body && <Text style={styles.body}>{body}</Text>}
      {!!meta && <Text style={styles.meta}>{meta}</Text>}
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
  subject: { fontSize: 14, fontWeight: '600', color: colors.ink },
  body: { fontSize: 13, color: colors.inkSoft },
  meta: { fontSize: 11, color: colors.mutedSoft, fontFamily: mono },
});
