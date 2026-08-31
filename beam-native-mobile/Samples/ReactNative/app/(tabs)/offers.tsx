import { useCallback, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';

import {
  DEFAULT_STORE,
  OFFER_GRANT_KEY,
  claimGrant,
  describeState,
  formatExpiry,
  formatWhen,
  isClaimable,
  listEntitlements,
  type Entitlement,
} from '../../src/beam/campaignOffers';
import { useBeam } from '../../src/state/beamContext';
import { useLogActions } from '../../src/state/logContext';
import { useNotifications } from '../../src/state/notificationContext';
import AsyncButton from '../../src/ui/AsyncButton';
import Field from '../../src/ui/Field';
import { Hint, Value } from '../../src/ui/Hint';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';
import { colors, mono, radius, space } from '../../src/ui/theme';

/**
 * Offers tab: the player half of the **offer federation**.
 *
 * A campaign lane attaches an offer to the message it sends; the campaign runtime grants it to
 * each recipient as the send goes out; this screen is where the player claims it. Two calls,
 * both on `beam.campaignOffer`, and both parameterised by the federation id — because "which store"
 * is an extension point, not a Beamable feature. The Federation field is not a debugging
 * convenience: it is the whole point.
 *
 * Two ways in, deliberately shown side by side:
 *  - **Entitlements** — everything this store holds for you. Works with no message at all.
 *  - **From the last push** — the deep-link: a campaign writes the grant id into the push under
 *    the reserved `beam_offer_grant` key, so "you got something" and "here it is" are one press
 *    apart. This is the flow a shipping game writes.
 */
export default function OffersTab() {
  const { append } = useLogActions();
  const { isReady } = useBeam();
  const { lastOfferGrantId } = useNotifications();

  const [federationId, setFederationId] = useState<string>(DEFAULT_STORE);
  const [entitlements, setEntitlements] = useState<Entitlement[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const inFlight = useRef(false);

  const store = federationId.trim() || DEFAULT_STORE;
  const claimable = useMemo(() => entitlements.filter(isClaimable).length, [entitlements]);

  /**
   * `silent` suppresses the "not connected" line and the loading chatter, so the automatic
   * on-focus refresh doesn't spam the Activity log every time you switch tabs.
   */
  const refresh = useCallback(
    async (silent = false) => {
      if (!isReady) {
        if (!silent) append('Entitlements: Beamable is not connected yet');
        return;
      }
      if (inFlight.current) return;
      inFlight.current = true;
      setBusy(true);
      setError(null);
      try {
        const held = await listEntitlements(store);
        setEntitlements(held);
        append(`Entitlements (${store}): ${held.length}`);
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        // Shown in the section, not just the collapsed console — the on-focus refresh is
        // silent, so a failure would otherwise be indistinguishable from "you hold nothing".
        setError(msg);
        append(`Entitlements error: ${msg}`);
      } finally {
        inFlight.current = false;
        setBusy(false);
      }
    },
    [isReady, store, append],
  );

  // Auto-refresh on focus. `refresh` changes identity when `isReady` or the store flips, which
  // makes this re-run — so a tab opened before the connection landed fills in as soon as it does.
  useFocusEffect(
    useCallback(() => {
      void refresh(true);
    }, [refresh]),
  );

  /** Claim, then re-read so the row flips to its new state in place rather than going stale. */
  const claim = useCallback(
    (grantId: string) => async () => {
      if (!isReady) throw new Error('Beamable is not connected yet');
      try {
        return await claimGrant(store, grantId);
      } finally {
        // Even a refused claim can have moved the state server-side (an unclaimed expiry is
        // folded in on read), so re-read either way.
        void refresh(true);
      }
    },
    [isReady, store, refresh],
  );

  return (
    <Screen>
      <Section title="Store">
        <Hint>
          Which store answers is a federation — a microservice implementing IFederatedCampaignOffer
          under its own id. `beamable_store` is the default Beamable ships; a game selling through
          Steam or its own web shop deploys its own and is reached by these same two calls. Nothing
          on this screen branches on the value, which is why it is an input.
        </Hint>
        <Field
          value={federationId}
          onChangeText={setFederationId}
          placeholder={DEFAULT_STORE}
          accessibilityLabel="Store federation id"
        />
      </Section>

      <Section
        title={`Entitlements (${entitlements.length})`}
        right={
          busy ? (
            <ActivityIndicator size="small" />
          ) : (
            <Pressable
              onPress={() => void refresh()}
              hitSlop={10}
              style={({ pressed }) => [styles.refresh, pressed && styles.pressed]}
              accessibilityRole="button"
              accessibilityLabel="Refresh entitlements"
            >
              <Ionicons name="refresh" size={18} color={colors.primary} />
            </Pressable>
          )
        }
      >
        <Hint>
          Reads beam.campaignOffer.getEntitlements(federationId) → GET /api/campaign-offer/entitlements.
          Every grant in every state, so read the state rather than the presence of a row —
          claimed and revoked ones stay in the list. Expiry is evaluated on read, not swept, so a
          lapsed grant reports "expired" the first time you look.
        </Hint>
        {error && (
          <Text style={styles.error} selectable>
            ✕ {error}
          </Text>
        )}
        {entitlements.length === 0 ? (
          <Hint>
            {error
              ? 'Entitlements not loaded.'
              : `Nothing granted by "${store}" yet. Publish a campaign lane with an offer and enrol this player.`}
          </Hint>
        ) : (
          <>
            <Hint>
              {claimable} of {entitlements.length} ready to claim.
            </Hint>
            {entitlements.map((e) => (
              <View key={e.grantId} style={styles.card}>
                <Text style={styles.state}>{describeState(e.state)}</Text>
                {/* The offer id is opaque — only the store that minted it can interpret it. It
                    is shown as an identifier, never as a name. */}
                <Value label="offer">{e.offerId || '—'}</Value>
                <Value label="grant">{e.grantId}</Value>
                <Text style={styles.meta}>
                  granted {formatWhen(e.grantedAt)} · {formatExpiry(e.expiresAt)}
                </Text>
                {isClaimable(e) && <AsyncButton label="Claim" run={claim(e.grantId)} />}
              </View>
            ))}
          </>
        )}
      </Section>

      <Section title="From the last push">
        <Hint>
          A campaign that attaches an offer writes the grant id into the send's payload under the
          reserved `{OFFER_GRANT_KEY}` key, so the message can deep-link straight to what you were
          given. This reads it off the received notification — no entitlement list needed.
        </Hint>
        {lastOfferGrantId ? (
          <>
            <Value label="grant">{lastOfferGrantId}</Value>
            <AsyncButton label="Claim from push" run={claim(lastOfferGrantId)} />
          </>
        ) : (
          <Hint>
            No push received this session carried `{OFFER_GRANT_KEY}`. Most pushes do not — only a
            campaign send whose lane has an offer does. Note the in-game rail cannot carry it at
            all: Beamable mail has no field for it, so an in-game recipient claims from the list
            above.
          </Hint>
        )}
      </Section>
    </Screen>
  );
}

const styles = StyleSheet.create({
  refresh: { padding: space.xs },
  pressed: { opacity: 0.5 },
  card: {
    backgroundColor: colors.card,
    borderColor: colors.surfaceBorder,
    borderWidth: 1,
    borderRadius: radius.lg,
    padding: space.lg,
    gap: space.xs,
  },
  state: { color: colors.ink, fontSize: 14, fontWeight: '600' },
  meta: { color: colors.muted, fontSize: 12, fontFamily: mono },
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
