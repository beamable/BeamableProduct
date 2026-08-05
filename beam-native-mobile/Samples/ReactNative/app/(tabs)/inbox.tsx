import { useCallback, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text } from 'react-native';
import { useFocusEffect } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';

import { listInGameMessages } from '../../src/beam/ingameMessages';
import { useBeam } from '../../src/state/beamContext';
import { useLogActions } from '../../src/state/logContext';
import AsyncButton from '../../src/ui/AsyncButton';
import { Hint } from '../../src/ui/Hint';
import MessageCard from '../../src/ui/MessageCard';
import RailActionNote from '../../src/ui/RailActionNote';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';
import { colors, mono, radius, space } from '../../src/ui/theme';

type Messages = Awaited<ReturnType<typeof listInGameMessages>>;

/**
 * In-game tab: the `ingame` message rail plus the player's mailbox, refreshed whenever the tab
 * gains focus.
 */
export default function InboxTab() {
  const { append } = useLogActions();
  const { isReady, setRailOptIn } = useBeam();
  const [messages, setMessages] = useState<Messages>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const inFlight = useRef(false);

  /**
   * `silent` suppresses the "not connected" line and the loading chatter, so the automatic
   * on-focus refresh doesn't spam the Activity log every time you switch tabs.
   */
  const refresh = useCallback(
    async (silent = false) => {
      if (!isReady) {
        if (!silent) append('In-game messages: Beamable is not connected yet');
        return;
      }
      if (inFlight.current) return;
      inFlight.current = true;
      setBusy(true);
      setError(null);
      try {
        const msgs = await listInGameMessages();
        setMessages(msgs);
        append(`In-game messages: ${msgs.length}`);
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        // Surfaced in the section itself, not just the collapsed console — the on-focus
        // refresh is silent, so a failure would otherwise look like an empty mailbox.
        setError(msg);
        append(`Inbox error: ${msg}`);
      } finally {
        inFlight.current = false;
        setBusy(false);
      }
    },
    [isReady, append],
  );

  // Auto-refresh on focus. `refresh` changes identity when `isReady` flips, which makes this
  // re-run — so a mailbox opened before the connection landed fills in as soon as it does.
  useFocusEffect(
    useCallback(() => {
      void refresh(true);
    }, [refresh]),
  );

  return (
    <Screen>
      <Section title="In-game rail">
        <Hint>
          In-game delivery is opt-in — opt in so campaigns targeting the `ingame` rail reach your
          mailbox (POST /api/message-rail/register). The InGameRailService writes one Beamable
          mail per recipient (POST /basic/mail/bulk).
        </Hint>
        <AsyncButton label="Opt in to in-game delivery" run={() => setRailOptIn('ingame', true)} />
        <AsyncButton
          label="Opt out of in-game delivery"
          variant="secondary"
          run={() => setRailOptIn('ingame', false)}
        />
        <RailActionNote rail="ingame" />
      </Section>

      <Section
        title={`Inbox (${messages.length})`}
        right={
          busy ? (
            <ActivityIndicator size="small" />
          ) : (
            <Pressable
              onPress={() => void refresh()}
              hitSlop={10}
              style={({ pressed }) => [styles.refresh, pressed && styles.pressed]}
              accessibilityRole="button"
              accessibilityLabel="Refresh inbox"
            >
              <Ionicons name="refresh" size={18} color={colors.primary} />
            </Pressable>
          )
        }
      >
        <Hint>
          Reads this player's Beamable mailbox, newest first. Refreshes automatically when this
          tab opens — send a message from the Portal Campaign Builder to see it appear.
        </Hint>
        {error && <Text style={styles.error} selectable>✕ {error}</Text>}
        {messages.length === 0 ? (
          <Hint>{error ? 'Mailbox not loaded.' : 'No in-game messages.'}</Hint>
        ) : (
          messages.map((m) => (
            <MessageCard
              key={String(m.id)}
              subject={m.subject}
              body={m.body}
              meta={`${m.state}${m.category ? ` · ${m.category}` : ''}`}
            />
          ))
        )}
      </Section>
    </Screen>
  );
}

const styles = StyleSheet.create({
  refresh: { padding: space.xs },
  pressed: { opacity: 0.5 },
  error: {
    color: colors.errorInk,
    backgroundColor: colors.errorBg,
    borderColor: colors.errorBorder,
    borderWidth: 1,
    borderRadius: radius.md,
    paddingVertical: 6,
    paddingHorizontal: space.md,
    fontSize: 12,
    fontFamily: mono,
  },
});
