import { StyleSheet, TextInput, type TextInputProps } from 'react-native';

import { colors, radius } from './theme';

/** A text input styled to match the sample's cards. Passes every TextInput prop through. */
export default function Field(props: TextInputProps) {
  return (
    <TextInput
      autoCapitalize="none"
      autoCorrect={false}
      placeholderTextColor={colors.mutedSoft}
      {...props}
      style={[styles.input, props.style]}
    />
  );
}

const styles = StyleSheet.create({
  input: {
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: radius.md,
    paddingVertical: 10,
    paddingHorizontal: 12,
    fontSize: 14,
    color: colors.ink,
    backgroundColor: colors.card,
  },
});
