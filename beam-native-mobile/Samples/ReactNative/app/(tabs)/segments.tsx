import { useCallback, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text } from 'react-native';
import { useFocusEffect } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';

import {
  DEMO_STATS,
  MAX_BULK_PLAYERS,
  STAT_OBJECT,
  addToStat,
  createPlayersWithStat,
  deleteStat,
  describeSegmentError,
  formatWhen,
  listMySegments,
  listMyTransitions,
  readStats,
  setStat,
  statNumber,
  type PlayerSegment,
  type SegmentTransition,
} from '../../src/beam/segments';
import { useBeam } from '../../src/state/beamContext';
import { useLogActions } from '../../src/state/logContext';
import AsyncButton from '../../src/ui/AsyncButton';
import Field from '../../src/ui/Field';
import { Hint } from '../../src/ui/Hint';
import MessageCard from '../../src/ui/MessageCard';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';
import StatCard from '../../src/ui/StatCard';
import { colors, mono, radius, space } from '../../src/ui/theme';

/** How many transition rows to render — the endpoint pages, and this is a demo. */
const MAX_TRANSITIONS = 10;

/**
 * Segments tab: the stats → segment loop.
 *
 * Segments are evaluated from a player's GAME-PRIVATE stats, which a client token cannot read or
 * write — so both halves of this page go through the `PlayerStatsService` microservice for the
 * stats, and through the realms segments API for the membership. A press on a card's add button
 * and the resulting Enter/Exit row are visible at once.
 *
 * Nothing here provisions a segment — the rule has to exist in the Portal for a stat bump to
 * move the player. Until then the stat cards still work and "My segments" stays empty, which is
 * itself the honest result.
 */
export default function SegmentsTab() {
  const { isReady } = useBeam();
  const { append } = useLogActions();

  const [stats, setStats] = useState<Record<string, string>>({});
  const [statsLoaded, setStatsLoaded] = useState(false);
  const [statsBusy, setStatsBusy] = useState(false);
  const [statsError, setStatsError] = useState<string | null>(null);

  const [segments, setSegments] = useState<PlayerSegment[]>([]);
  const [segmentsError, setSegmentsError] = useState<string | null>(null);
  const [transitions, setTransitions] = useState<SegmentTransition[]>([]);
  const [transitionsError, setTransitionsError] = useState<string | null>(null);
  const [segmentsBusy, setSegmentsBusy] = useState(false);

  const [customKey, setCustomKey] = useState('');
  const [customValue, setCustomValue] = useState('');

  // Bulk create. Prefilled with the card's key so the default press does something meaningful.
  const [bulkCount, setBulkCount] = useState('5');
  const [bulkKey, setBulkKey] = useState(DEMO_STATS[0].key);
  const [bulkValue, setBulkValue] = useState('10');
  const [bulkPlayers, setBulkPlayers] = useState<string[]>([]);

  const statsInFlight = useRef(false);
  const segmentsInFlight = useRef(false);

  /** `silent` keeps the on-focus refresh out of the Activity log — see the Inbox tab. */
  const refreshStats = useCallback(
    async (silent = false) => {
      if (!isReady) {
        if (!silent) append('Stats: Beamable is not connected yet');
        return;
      }
      if (statsInFlight.current) return;
      statsInFlight.current = true;
      setStatsBusy(true);
      setStatsError(null);
      try {
        const next = await readStats();
        setStats(next);
        setStatsLoaded(true);
        if (!silent) append(`Game-private stats: ${Object.keys(next).length} key(s)`);
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        setStatsError(msg);
        append(`Stats error: ${msg}`);
      } finally {
        statsInFlight.current = false;
        setStatsBusy(false);
      }
    },
    [isReady, append],
  );

  /**
   * Membership and history are read together but reported apart: they are two endpoints, and
   * one being closed off shouldn't blank out the other.
   */
  const refreshSegments = useCallback(
    async (silent = false) => {
      if (!isReady) {
        if (!silent) append('Segments: Beamable is not connected yet');
        return;
      }
      if (segmentsInFlight.current) return;
      segmentsInFlight.current = true;
      setSegmentsBusy(true);
      try {
        const [mine, history] = await Promise.allSettled([listMySegments(), listMyTransitions()]);

        if (mine.status === 'fulfilled') {
          setSegments(mine.value);
          setSegmentsError(null);
          if (!silent) append(`My segments: ${mine.value.length}`);
        } else {
          const msg = describeSegmentError(mine.reason);
          setSegmentsError(msg);
          append(`Segments error: ${msg}`);
        }

        if (history.status === 'fulfilled') {
          setTransitions(history.value);
          setTransitionsError(null);
        } else {
          const msg = describeSegmentError(history.reason);
          setTransitionsError(msg);
          append(`Segment transitions error: ${msg}`);
        }
      } finally {
        segmentsInFlight.current = false;
        setSegmentsBusy(false);
      }
    },
    [isReady, append],
  );

  // Both reads run on focus, and re-run when the connection lands (`isReady` flips the
  // callbacks' identity), so a tab opened before init completes fills itself in.
  useFocusEffect(
    useCallback(() => {
      void refreshStats(true);
      void refreshSegments(true);
    }, [refreshStats, refreshSegments]),
  );

  /** The add button on a stat card: bump the stat, then look for the resulting move. */
  const add = (key: string, step: number) => async () => {
    if (!isReady) throw new Error('Beamable is not connected yet');
    const next = await addToStat(key, step);
    setStats((prev) => ({ ...prev, [key]: String(next) }));
    // Re-evaluation happens server-side and is not instant; this is a best-effort peek, and the
    // ↻ in the Segments header is there for when the move lands a moment later.
    void refreshSegments(true);
    return `${key} = ${next} — rules watching this key re-evaluate server-side`;
  };

  const setCustomStat = async () => {
    const key = requireCustomKey();
    const value = customValue.trim();
    if (!value) throw new Error('Enter a value first');
    const message = await setStat(key, value);
    setStats((prev) => ({ ...prev, [key]: value }));
    void refreshSegments(true);
    return message;
  };

  /** Deleting is how you drop a player back out of a segment without guessing a lower value. */
  const deleteCustomStat = async () => {
    const key = requireCustomKey();
    const message = await deleteStat(key);
    setStats((prev) => {
      const next = { ...prev };
      delete next[key];
      return next;
    });
    void refreshSegments(true);
    return message;
  };

  /**
   * Create N players carrying one stat.
   *
   * Nothing else on this screen changes — these are OTHER players. What moves is the segment's
   * member count in the Portal, which is the thing a rule is really for.
   */
  const bulkCreate = async () => {
    if (!isReady) throw new Error('Beamable is not connected yet');
    const count = Number(bulkCount.trim());
    if (!Number.isInteger(count) || count < 1) throw new Error('Enter a whole number of players (1 or more)');
    const key = bulkKey.trim();
    if (!key) throw new Error('Enter a stat key first');
    const value = bulkValue.trim();
    if (!value) throw new Error('Enter a value first');

    const { created, message } = await createPlayersWithStat(count, key, value);
    setBulkPlayers(created.map((p) => String(p.playerId)));
    return message;
  };

  function requireCustomKey(): string {
    if (!isReady) throw new Error('Beamable is not connected yet');
    const key = customKey.trim();
    if (!key) throw new Error('Enter a stat key first');
    return key;
  }

  const refreshButton = (busy: boolean, onPress: () => void, label: string) =>
    busy ? (
      <ActivityIndicator size="small" />
    ) : (
      <Pressable
        onPress={onPress}
        hitSlop={10}
        style={({ pressed }) => [styles.refresh, pressed && styles.pressed]}
        accessibilityRole="button"
        accessibilityLabel={label}
      >
        <Ionicons name="refresh" size={18} color={colors.primary} />
      </Pressable>
    );

  // Keys the player has that aren't one of the four cards — usually stats this realm's own
  // segments watch, written by a game server or another service.
  const otherKeys = Object.keys(stats)
    .filter((k) => !DEMO_STATS.some((s) => s.key === k))
    .sort();

  return (
    <Screen>
      <Section title="How segments work">
        <Hint>
          A segment is a rule over player STATS, authored in the Portal (Segments) — e.g.
          `PLAYER_LEVEL {'>'}= 10`. The backend watches the stat keys a rule mentions; when one
          changes for a player it re-evaluates the rule and moves the player in or out. Campaigns,
          announcements and listings then target the segment, never a player list.
          {'\n\n'}
          The stats it reads are GAME PRIVATE — `{STAT_OBJECT}` — and a client token cannot create
          or change those. So the cards below do not call `beam.stats`: they call the
          `PlayerStatsService` MICROSERVICE, which runs with a privileged identity and only ever
          writes the caller's own stats.
          {'\n\n'}
          1. write a stat — PlayerStatsService.AddToMyStat / SetMyStat (the cards below){'\n'}
          2. read the membership back — GET
          /api/realms/{'{'}realmId{'}'}/players/{'{'}playerId{'}'}/segments
          {'\n\n'}
          Author a segment in the Portal over one of the keys below, then press its add button
          until the rule matches.
        </Hint>
      </Section>

      <Section
        title="Player stats · game.private"
        right={refreshButton(statsBusy, () => void refreshStats(), 'Refresh stats')}
      >
        <Hint>
          Game-private stats for this player — object id `{STAT_OBJECT}`. Read with
          PlayerStatsService.GetMyStats, because the client SDK cannot read this domain either.
          {'\n'}
          The add button calls AddToMyStat: the service re-reads the key, adds the step and writes
          the sum back in one round trip — the stats API has no atomic increment.
          {'\n'}
          `PLAYER_LEVEL` is a key of your own, so nothing recomputes it and the value sticks. The
          platform's own aggregates (`SPEND_*` / `PURCHASES_*` / `SESSIONS_*`) are rebuilt from real
          session and purchase records at session start, so writing those by hand does not last —
          use "Any stat key" below if a rule in your realm watches one.
        </Hint>
        {statsError && (
          <Text style={styles.error} selectable>
            ✕ {statsError}
          </Text>
        )}
        {DEMO_STATS.map((s) => (
          <StatCard
            key={s.key}
            statKey={s.key}
            label={s.label}
            value={statsLoaded ? String(statNumber(stats, s.key)) : null}
            description={s.description}
            rule={s.rule}
            step={s.step}
            add={add(s.key, s.step)}
          />
        ))}
        {otherKeys.length > 0 && (
          <Hint>
            Other game-private stats on this player:{'\n'}
            {otherKeys.map((k) => `${k} = ${stats[k]}`).join('\n')}
          </Hint>
        )}
      </Section>

      <Section title="Any stat key">
        <Hint>
          Your realm's segments probably watch keys of their own. Set one directly here —
          PlayerStatsService.SetMyStat, an absolute value instead of an increment. Delete removes
          the stat outright (DeleteMyStat), which is the cleanest way to make a player EXIT a
          segment again.
        </Hint>
        <Field
          placeholder="Stat key (e.g. VIP_TIER)"
          value={customKey}
          onChangeText={setCustomKey}
        />
        <Field placeholder="Value (e.g. 3)" value={customValue} onChangeText={setCustomValue} />
        <AsyncButton label="Set stat" run={setCustomStat} />
        <AsyncButton label="Delete stat" variant="secondary" run={deleteCustomStat} />
      </Section>

      <Section title="Populate a segment">
        <Hint>
          Creates N brand-new players and sets one game-private stat on each
          (PlayerStatsService.CreatePlayersWithStat). A rule tested against one player proves the
          plumbing; this is how you get a segment with MEMBERS — watch its count in Portal →
          Segments.
          {'\n'}
          Each player is a fresh anonymous account whose only distinguishing mark is the stat. They
          are REAL and permanent: the service caps one call at {MAX_BULK_PLAYERS} and there is no
          delete, so it is a QA fixture — not something to ship in a client.
        </Hint>
        <Field
          placeholder={`How many players (1–${MAX_BULK_PLAYERS})`}
          value={bulkCount}
          onChangeText={setBulkCount}
          keyboardType="number-pad"
        />
        <Field placeholder="Stat key" value={bulkKey} onChangeText={setBulkKey} />
        <Field placeholder="Value for every player" value={bulkValue} onChangeText={setBulkValue} />
        <AsyncButton label="Create players" run={bulkCreate} />
        {bulkPlayers.length > 0 && (
          <Hint>
            Last created ({bulkPlayers.length}):{'\n'}
            {bulkPlayers.join('\n')}
          </Hint>
        )}
      </Section>

      <Section
        title={`My segments (${segments.length})`}
        right={refreshButton(segmentsBusy, () => void refreshSegments(), 'Refresh segments')}
      >
        <Hint>
          The segments this player is in right now, with how they got in (`Rule` — matched the
          segment's rule; `IncludeList` — added by hand in the Portal). Evaluation is
          asynchronous, so after a stat bump give it a moment and press ↻.
        </Hint>
        {segmentsError && (
          <Text style={styles.error} selectable>
            ✕ {segmentsError}
          </Text>
        )}
        {segments.length === 0 ? (
          <Hint>
            {segmentsError
              ? 'Membership not loaded.'
              : 'In no segments. Author a segment in the Portal over one of the stat keys above, then bump it past the threshold.'}
          </Hint>
        ) : (
          segments.map((s) => (
            <MessageCard
              key={s.segmentId}
              subject={s.segmentId}
              body={`Entered ${formatWhen(s.enteredAt)}`}
              meta={`source: ${s.sources.join(', ') || 'unknown'}`}
            />
          ))
        )}
      </Section>

      <Section title="Recent transitions">
        <Hint>
          Enter/exit history for this player — the proof a stat write did something. A row with
          `cause: Rule` is the segment engine reacting to a stat change; `StateChange` is a
          segment being enabled or archived in the Portal.
        </Hint>
        {transitionsError && (
          <Text style={styles.error} selectable>
            ✕ {transitionsError}
          </Text>
        )}
        {transitions.length === 0 ? (
          <Hint>{transitionsError ? 'History not loaded.' : 'No transitions yet.'}</Hint>
        ) : (
          transitions.slice(0, MAX_TRANSITIONS).map((t, i) => (
            <MessageCard
              key={`${t.segmentId}-${String(t.timestamp)}-${i}`}
              subject={`${t.kind === 'Exit' ? '←' : '→'} ${t.kind}  ${t.segmentId}`}
              body={`cause: ${t.cause} · rule v${t.ruleVersion}`}
              meta={formatWhen(t.timestamp)}
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
