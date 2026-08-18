import { useState } from 'react';
import { StyleSheet, Text } from 'react-native';

import { BeamNotifications } from '@beamable/notifications-react-native';
import type {
  EventMap,
  NotificationIntentData,
  NotificationOffer,
} from '@beamable/notifications-react-native';

import { getBeam } from '../../src/beam/beamClient';
import { BEAM_CONFIG } from '../../src/beam/config';
import { buildAuthPayload, describeAuthPayload } from '../../src/beam/nativeAuth';
import { detailsUrl } from '../../src/linking/links';
import { useBeam } from '../../src/state/beamContext';
import { useNotifications } from '../../src/state/notificationContext';
import AsyncButton from '../../src/ui/AsyncButton';
import Field from '../../src/ui/Field';
import { Hint } from '../../src/ui/Hint';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';
import { colors, mono, radius, space } from '../../src/ui/theme';

const OFFER: NotificationOffer = {
  itemId: 'test_offer',
  value: 100,
  customData: { tier: 'gold' },
};

/** How long to wait for the native side to report the funnel send before giving up. */
const FUNNEL_TIMEOUT_MS = 10_000;

/**
 * `trackOfferClicked` / `trackOfferConverted` are fire-and-forget on the native side: the HTTP
 * result comes back later on the `funnelResult` event, not from the call. Subscribing BEFORE
 * firing and awaiting the next event turns that into a real per-press outcome.
 */
function nextFunnelResult(): Promise<EventMap['funnelResult']> {
  return new Promise((resolve, reject) => {
    let settled = false;
    const sub = BeamNotifications.addListener('funnelResult', (payload) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      sub.remove();
      resolve(payload);
    });
    const timer = setTimeout(() => {
      if (settled) return;
      settled = true;
      sub.remove();
      reject(
        new Error(
          `No funnelResult within ${FUNNEL_TIMEOUT_MS / 1000}s — the native call may not have been sent.`,
        ),
      );
    }, FUNNEL_TIMEOUT_MS);
  });
}

/**
 * Analytics tab: the native Clicked / Converted funnel events, and the native-side auth those
 * events use when the JS runtime isn't running.
 */
export default function AnalyticsTab() {
  const { isReady } = useBeam();
  const { campaignId, nodeId, setCampaignId, setNodeId, outreachId, trackId } = useNotifications();
  const [authView, setAuthView] = useState<string | null>(null);

  // Funnel coordinates live in the notification context, so a campaign push that arrives while
  // another tab is active still fills these in.
  const buildIntent = (): NotificationIntentData => ({
    campaignId: campaignId.trim(),
    nodeId: nodeId.trim(),
    gamerTag: String(getBeam()!.player.id),
    cidPid: `${BEAM_CONFIG.cid}.${BEAM_CONFIG.pid}`,
    deeplink: detailsUrl(777),
    // Echoed from the push that filled the coordinates. Null when the IDs were typed by hand — the
    // event is still sent, it just can't be attributed to a campaign send node.
    outreachId: outreachId ?? undefined,
    trackId: trackId ?? undefined,
  });

  const track = (kind: 'clicked' | 'converted') => async () => {
    if (!isReady) throw new Error('Beamable is not connected yet');
    if (!campaignId.trim() || !nodeId.trim())
      throw new Error('Enter a Campaign ID and a Node ID first');

    const intent = buildIntent();
    // Subscribe before firing — the native round trip can beat a later subscription.
    const pending = nextFunnelResult();
    if (kind === 'clicked') BeamNotifications.trackOfferClicked(intent, OFFER);
    else BeamNotifications.trackOfferConverted(intent, OFFER);

    const result = await pending;
    const detail = `${result.funnelType} · HTTP ${result.statusCode}${result.message ? ` — ${result.message}` : ''}`;
    if (!result.ok) throw new Error(detail);
    return detail;
  };

  const viewNativeAuth = async () => {
    const payload = await buildAuthPayload();
    setAuthView(describeAuthPayload(payload));
    return 'Auth payload read from token storage';
  };

  const clearNativeAuth = async () => {
    BeamNotifications.clearAuth();
    setAuthView(null);
    return 'Native funnel auth cleared — the next closed-app funnel call will fail to authenticate';
  };

  return (
    <Screen>
      <Section title="Funnel: clicked / converted">
        <Hint>
          Emits native Clicked / Converted funnel analytics for a test offer (iOS & Android). The
          native call is fire-and-forget, so each button waits for the matching funnelResult event
          and reports its HTTP status below.{'\n'}
          Type any Campaign / Node ID, or open the app from a campaign push and these fields
          auto-fill from its payload.
        </Hint>
        <Field
          placeholder="Campaign ID (e.g. test_campaign)"
          value={campaignId}
          onChangeText={setCampaignId}
        />
        <Field placeholder="Node ID (e.g. test_node)" value={nodeId} onChangeText={setNodeId} />
        <AsyncButton label="Track offer clicked" run={track('clicked')} />
        <AsyncButton label="Track offer converted" run={track('converted')} />
      </Section>

      <Section title="Native auth">
        <Hint>
          On connect the app hands the player's tokens to the native side
          (BeamNotifications.configureAuth) so the CLOSED-APP funnel can authenticate when the JS
          runtime is not running — that's how a Clicked event survives a push tapped from a killed
          app.
        </Hint>
        <Hint>
          Note: the native modules expose configureAuth and clearAuth only — there is no
          read-back. What's shown below is the payload this app SENDS to native, rebuilt from the
          SDK's token storage; it matches what native holds unless Clear has since run.
        </Hint>
        <AsyncButton label="View native auth payload" variant="secondary" run={viewNativeAuth} />
        {authView && (
          <Text style={styles.authBlock} selectable>
            {authView}
          </Text>
        )}
        <AsyncButton label="Clear native auth" variant="secondary" run={clearNativeAuth} />
      </Section>
    </Screen>
  );
}

const styles = StyleSheet.create({
  authBlock: {
    color: colors.consoleInk,
    backgroundColor: colors.console,
    borderRadius: radius.md,
    padding: space.md,
    fontSize: 11,
    fontFamily: mono,
  },
});
