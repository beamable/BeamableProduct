import { useCallback, useEffect, useRef, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import { useLogActions } from '../state/logContext';
import Button, { type ButtonVariant } from './Button';
import { colors, mono, radius, space } from './theme';

type Outcome =
  | { status: 'idle' }
  | { status: 'pending' }
  | { status: 'ok'; message: string }
  | { status: 'error'; message: string };

/**
 * A button for anything that awaits the backend: it owns the in-flight spinner and shows the
 * result inline, directly under itself.
 *
 * The sample used to report every outcome only through `append()` into the debug console —
 * which is collapsed by default, so pressing a button appeared to do nothing. Results are still
 * mirrored into the Activity log (that's the scrollback), but the outcome of the press you just
 * made is now visible where you pressed it.
 *
 * `run` returns the success message, or throws — a rejection renders red. Guard conditions
 * ("not connected yet") should throw for the same reason: they're why the action didn't happen.
 */
export default function AsyncButton({
  label,
  run,
  variant = 'primary',
  disabled = false,
}: {
  label: string;
  run: () => Promise<string>;
  variant?: ButtonVariant;
  disabled?: boolean;
}) {
  const { append } = useLogActions();
  const [outcome, setOutcome] = useState<Outcome>({ status: 'idle' });
  // Guards against a state update after unmount when a page is swapped mid-request.
  const mounted = useRef(true);
  const inFlight = useRef(false);

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  const onPress = useCallback(() => {
    if (inFlight.current) return;
    inFlight.current = true;
    setOutcome({ status: 'pending' });

    void (async () => {
      try {
        const message = await run();
        append(message);
        if (mounted.current) setOutcome({ status: 'ok', message });
      } catch (e) {
        const message = e instanceof Error ? e.message : String(e);
        append(`${label}: ${message}`);
        if (mounted.current) setOutcome({ status: 'error', message });
      } finally {
        inFlight.current = false;
      }
    })();
  }, [append, label, run]);

  return (
    <View style={styles.wrap}>
      <Button
        label={label}
        onPress={onPress}
        variant={variant}
        disabled={disabled}
        loading={outcome.status === 'pending'}
      />
      {outcome.status === 'pending' && <Text style={styles.pending}>Working…</Text>}
      {outcome.status === 'ok' && (
        <Text style={[styles.result, styles.ok]} selectable>
          ✓ {outcome.message}
        </Text>
      )}
      {outcome.status === 'error' && (
        <Text style={[styles.result, styles.error]} selectable>
          ✕ {outcome.message}
        </Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { gap: space.xs },
  pending: { color: colors.muted, fontSize: 12, fontFamily: mono, paddingHorizontal: space.xs },
  // Results are selectable so a backend error can be copied out of the app.
  result: {
    fontSize: 12,
    fontFamily: mono,
    paddingVertical: 6,
    paddingHorizontal: space.md,
    borderRadius: radius.md,
    borderWidth: 1,
  },
  ok: {
    color: colors.okInk,
    backgroundColor: colors.okBg,
    borderColor: colors.okBorder,
  },
  error: {
    color: colors.errorInk,
    backgroundColor: colors.errorBg,
    borderColor: colors.errorBorder,
  },
});
