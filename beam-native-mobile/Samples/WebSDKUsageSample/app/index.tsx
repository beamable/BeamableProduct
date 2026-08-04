import { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { getBeam, initBeam } from '../src/beam/beamClient';
import {
  DEFAULT_INPUTS,
  SDK_ACTION_COUNT,
  SDK_GROUPS,
  type SdkAction,
  type SdkInputs,
} from '../src/beam/sdkCatalog';

type RunState = { status: 'running' | 'ok' | 'error'; text: string };

function pretty(value: unknown): string {
  try {
    const json = JSON.stringify(
      value,
      (_k, v) => (typeof v === 'bigint' ? `${v}n` : v),
      2,
    );
    return json ?? String(value);
  } catch {
    return String(value);
  }
}

export default function SdkExplorer() {
  const [ready, setReady] = useState(false);
  const [connError, setConnError] = useState<string | null>(null);
  const [inputs, setInputs] = useState<SdkInputs>(DEFAULT_INPUTS);
  const [results, setResults] = useState<Record<string, RunState>>({});

  useEffect(() => {
    let active = true;
    initBeam()
      .then(() => active && setReady(true))
      .catch((e) => active && setConnError(e instanceof Error ? e.message : String(e)));
    return () => {
      active = false;
    };
  }, []);

  const setInput = (key: keyof SdkInputs, value: string) =>
    setInputs((prev) => ({ ...prev, [key]: value }));

  const runAction = async (id: string, action: SdkAction) => {
    const beam = getBeam();
    if (!beam) return;
    setResults((r) => ({ ...r, [id]: { status: 'running', text: '…' } }));
    try {
      const result = await action.run(beam, inputs);
      setResults((r) => ({ ...r, [id]: { status: 'ok', text: pretty(result) } }));
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      setResults((r) => ({ ...r, [id]: { status: 'error', text: msg } }));
    }
  };

  const writeCount = useMemo(
    () => SDK_GROUPS.reduce((n, g) => n + g.actions.filter((a) => a.kind === 'write').length, 0),
    [],
  );
  const highCount = useMemo(() => SDK_GROUPS.filter((g) => g.layer !== 'low').length, []);
  const lowCount = useMemo(() => SDK_GROUPS.filter((g) => g.layer === 'low').length, []);
  const firstLowKey = useMemo(() => SDK_GROUPS.find((g) => g.layer === 'low')?.key, []);

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.h1}>Beamable SDK Explorer</Text>
      <Text style={styles.summary}>
        {highCount} high-level services + {lowCount} low-level API areas ·{' '}
        {SDK_ACTION_COUNT} actions ({SDK_ACTION_COUNT - writeCount} read ○ ·{' '}
        {writeCount} write ✎). Each button calls a real SDK method on the live{' '}
        <Text style={styles.mono}>beam</Text> instance.
      </Text>

      {/* Connection status */}
      <View style={styles.statusBox}>
        {!ready && !connError ? (
          <View style={styles.row}>
            <ActivityIndicator />
            <Text style={styles.statusText}>Connecting to Beamable…</Text>
          </View>
        ) : connError ? (
          <Text style={styles.err}>Not connected: {connError}</Text>
        ) : (
          <Text style={styles.ok}>Connected · player {getBeam()?.player.id}</Text>
        )}
      </View>

      {/* Inputs */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Inputs</Text>
        <Text style={styles.hint}>
          Used by parameterized actions. Realm-specific values (leaderboard id,
          content id) come from your Beamable realm.
        </Text>
        <Field label="email" value={inputs.email} onChange={(v) => setInput('email', v)} />
        <Field label="password" value={inputs.password} onChange={(v) => setInput('password', v)} />
        <Field label="statKey" value={inputs.statKey} onChange={(v) => setInput('statKey', v)} />
        <Field label="statValue" value={inputs.statValue} onChange={(v) => setInput('statValue', v)} />
        <Field label="contentId" value={inputs.contentId} onChange={(v) => setInput('contentId', v)} placeholder="e.g. announcements.welcome" />
        <Field label="contentType" value={inputs.contentType} onChange={(v) => setInput('contentType', v)} placeholder="e.g. announcements" />
        <Field label="leaderboardId" value={inputs.leaderboardId} onChange={(v) => setInput('leaderboardId', v)} placeholder="from your realm" />
        <Field label="score" value={inputs.score} onChange={(v) => setInput('score', v)} keyboardType="numeric" />
        <Field label="objectId" value={inputs.objectId} onChange={(v) => setInput('objectId', v)} placeholder="group / party / calendar id" />
      </View>

      {/* Service groups */}
      {SDK_GROUPS.map((group) => (
        <View key={group.key}>
          {group.key === firstLowKey ? (
            <View style={styles.divider}>
              <Text style={styles.dividerTitle}>Low-level API · @beamable/sdk/api</Text>
              <Text style={styles.dividerSub}>
                Raw REST bindings via beam.requester. Player-facing modules only;
                server/admin modules (Payments, Beamo, Realms, Customer, Billing)
                are omitted — they reject a client token.
              </Text>
            </View>
          ) : null}
          <View style={styles.section}>
          <Text style={styles.sectionTitle}>{group.title}</Text>
          <Text style={styles.hint}>{group.blurb}</Text>
          {group.actions.map((action) => {
            const id = `${group.key}:${action.label}`;
            const state = results[id];
            return (
              <View key={id} style={styles.action}>
                <Pressable
                  disabled={!ready}
                  onPress={() => runAction(id, action)}
                  style={({ pressed }) => [
                    styles.button,
                    action.kind === 'write' && styles.buttonWrite,
                    (!ready || pressed) && styles.buttonDim,
                  ]}
                >
                  <Text style={styles.buttonText}>
                    {action.kind === 'write' ? '✎ ' : '○ '}
                    {action.label}
                  </Text>
                </Pressable>
                {action.note ? <Text style={styles.note}>{action.note}</Text> : null}
                {state ? (
                  <View
                    style={[
                      styles.result,
                      state.status === 'error' && styles.resultErr,
                      state.status === 'ok' && styles.resultOk,
                    ]}
                  >
                    {state.status === 'running' ? (
                      <ActivityIndicator />
                    ) : (
                      <Text style={styles.resultText}>{state.text}</Text>
                    )}
                  </View>
                ) : null}
              </View>
            );
          })}
          </View>
        </View>
      ))}

      <Text style={styles.footer}>
        Methods needing external input (third-party tokens, email verification
        codes, friend ids) are documented in the README feature matrix.
      </Text>
    </ScrollView>
  );
}

function Field(props: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  keyboardType?: 'default' | 'numeric';
}) {
  return (
    <View style={styles.field}>
      <Text style={styles.fieldLabel}>{props.label}</Text>
      <TextInput
        style={styles.input}
        value={props.value}
        onChangeText={props.onChange}
        placeholder={props.placeholder}
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType={props.keyboardType ?? 'default'}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { padding: 16, gap: 14, paddingBottom: 48 },
  h1: { fontSize: 22, fontWeight: '700' },
  summary: { fontSize: 13, color: '#4b5563', marginTop: -6 },
  mono: { fontFamily: 'Courier' },
  statusBox: {
    padding: 10,
    borderRadius: 10,
    backgroundColor: '#f3f4f6',
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  row: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  statusText: { color: '#374151' },
  ok: { color: '#15803d', fontWeight: '600' },
  err: { color: '#b91c1c' },
  section: {
    backgroundColor: '#fafafa',
    borderRadius: 12,
    padding: 12,
    gap: 8,
    borderWidth: 1,
    borderColor: '#eee',
  },
  sectionTitle: { fontSize: 15, fontWeight: '700', color: '#111827' },
  hint: { fontSize: 12, color: '#6b7280' },
  action: { gap: 4, marginTop: 4 },
  button: {
    backgroundColor: '#5A31F4',
    borderRadius: 8,
    paddingVertical: 9,
    paddingHorizontal: 12,
  },
  buttonWrite: { backgroundColor: '#b45309' },
  buttonDim: { opacity: 0.5 },
  buttonText: { color: 'white', fontWeight: '600', fontSize: 13 },
  note: { fontSize: 11, color: '#9ca3af' },
  result: {
    backgroundColor: '#0b1021',
    borderRadius: 8,
    padding: 8,
    maxHeight: 220,
  },
  resultOk: { backgroundColor: '#0b1021' },
  resultErr: { backgroundColor: '#3f1d1d' },
  resultText: { color: '#d1fae5', fontFamily: 'Courier', fontSize: 11 },
  field: { gap: 3 },
  fieldLabel: { fontSize: 11, color: '#6b7280', fontFamily: 'Courier' },
  input: {
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 8,
    paddingHorizontal: 10,
    paddingVertical: 8,
    fontSize: 13,
    backgroundColor: 'white',
  },
  footer: { fontSize: 11, color: '#9ca3af', textAlign: 'center', marginTop: 4 },
  divider: { marginTop: 8, marginBottom: 6, gap: 4 },
  dividerTitle: { fontSize: 14, fontWeight: '800', color: '#5A31F4' },
  dividerSub: { fontSize: 11, color: '#6b7280' },
});
