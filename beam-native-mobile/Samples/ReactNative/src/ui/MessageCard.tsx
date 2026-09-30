import { Pressable, StyleSheet, Text, View } from 'react-native';

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
  onPress,
}: {
  subject?: string | null;
  body?: string | null;
  meta?: string | null;
  /** Present only for mail that can still be opened; tapping marks it read. */
  onPress?: () => void;
}) {
  if (onPress) {
    return (
      <Pressable
        onPress={onPress}
        accessibilityRole="button"
        accessibilityLabel={`Open message ${subject ?? ''}`}
        style={({ pressed }) => [styles.card, pressed && styles.pressed]}
      >
        <CardBody subject={subject} body={body} meta={meta} />
      </Pressable>
    );
  }

  return (
    <View style={styles.card}>
      <CardBody subject={subject} body={body} meta={meta} />
    </View>
  );
}

function CardBody({
  subject,
  body,
  meta,
}: {
  subject?: string | null;
  body?: string | null;
  meta?: string | null;
}) {
  return (
    <>
      <Text style={styles.subject}>{subject || '(no subject)'}</Text>
      {!!body && <Text style={styles.body}>{body}</Text>}
      {!!meta && <Text style={styles.meta}>{meta}</Text>}
    </>
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
  pressed: { opacity: 0.6 },
  subject: { fontSize: 14, fontWeight: '600', color: colors.ink },
  body: { fontSize: 13, color: colors.inkSoft },
  meta: { fontSize: 11, color: colors.mutedSoft, fontFamily: mono },
});
