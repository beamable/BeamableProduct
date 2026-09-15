import { Pressable, StyleSheet, Text, View } from 'react-native';

import type { ParamRow } from '../beam/objectiveEvents';
import Field from './Field';
import { colors, radius, space } from './theme';

/**
 * The key/value editor for analytics event params.
 *
 * Values are strings, matching how params arrive on the wire from a real client — the platform
 * compares numerically whenever both sides parse as numbers, so `"25"` still satisfies
 * `amount >= 10`. Anything that genuinely needs another JSON type goes through the raw-JSON mode.
 */
export default function ParamRows({
  rows,
  onChange,
}: {
  rows: ParamRow[];
  onChange: (rows: ParamRow[]) => void;
}) {
  const patch = (index: number, next: Partial<ParamRow>) =>
    onChange(rows.map((row, i) => (i === index ? { ...row, ...next } : row)));

  const remove = (index: number) => onChange(rows.filter((_, i) => i !== index));

  return (
    <View style={styles.rows}>
      {rows.map((row, index) => (
        <View key={index} style={styles.row}>
          <Field
            style={styles.key}
            placeholder="key"
            value={row.key}
            onChangeText={(key) => patch(index, { key })}
          />
          <Field
            style={styles.value}
            placeholder="value"
            value={row.value}
            onChangeText={(value) => patch(index, { value })}
          />
          <Pressable
            style={styles.remove}
            onPress={() => remove(index)}
            accessibilityRole="button"
            accessibilityLabel={`Remove param ${row.key || index + 1}`}
          >
            <Text style={styles.removeInk}>✕</Text>
          </Pressable>
        </View>
      ))}

      <Pressable
        style={styles.add}
        onPress={() => onChange([...rows, { key: '', value: '' }])}
        accessibilityRole="button"
      >
        <Text style={styles.addInk}>+ Add param</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  rows: { gap: space.sm },
  row: { flexDirection: 'row', alignItems: 'center', gap: space.sm },
  // The key carries dot-notation paths (`details.price`), so it gets the wider half.
  key: { flex: 1.2 },
  value: { flex: 1 },
  remove: {
    width: 32,
    height: 32,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radius.md,
  },
  removeInk: { color: colors.muted, fontSize: 15 },
  add: { paddingVertical: space.sm },
  addInk: { color: colors.primary, fontSize: 13, fontWeight: '600' },
});
