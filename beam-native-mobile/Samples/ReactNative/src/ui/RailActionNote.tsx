import { StyleSheet, Text } from 'react-native';

import { useBeam, type RailId } from '../state/beamContext';
import { colors, mono } from './theme';

/**
 * Reports the last opt-in/opt-out this app performed for a rail.
 *
 * Deliberately NOT presented as "you are opted in": `MessageRailService` exposes only
 * `optIn` / `optOut`, with no endpoint to read a player's registration back and no echo of it
 * in the response. Anything shown here is local to this session.
 */
export default function RailActionNote({ rail }: { rail: RailId }) {
  const { railStatus } = useBeam();
  const last = railStatus[rail];
  if (!last) return null;
  return (
    <Text style={styles.note}>
      Last action here: {last.optIn ? 'opt-in' : 'opt-out'} {last.ok ? '✓' : '✗'} · {last.at}
      {'\n'}(local to this session — the SDK cannot read rail status back)
    </Text>
  );
}

const styles = StyleSheet.create({
  note: { color: colors.mutedSoft, fontSize: 11, fontFamily: mono },
});
