import { useCallback, useMemo, useRef, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from 'expo-router';

import {
  DEFAULT_STORE,
  OFFER_GRANT_KEY,
  claimGrant,
  isClaimable,
  isPurchasable,
  listEntitlements,
  type Entitlement,
} from '../../src/beam/campaignOffers';
import {
  diffBalances,
  formatAmount,
  knownCurrencyIds,
  mergeKnownCurrencies,
  readBalances,
  type Balance,
  type BalanceDelta,
} from '../../src/beam/inventory';
import { purchaseIdFor, purchaseListing } from '../../src/beam/commerce';
import { useBeam } from '../../src/state/beamContext';
import { useLogActions } from '../../src/state/logContext';
import { useNotifications } from '../../src/state/notificationContext';
import AsyncButton from '../../src/ui/AsyncButton';
import BalanceRow from '../../src/ui/BalanceRow';
import Collapsible from '../../src/ui/Collapsible';
import Field from '../../src/ui/Field';
import { Hint, Value } from '../../src/ui/Hint';
import OfferCard from '../../src/ui/OfferCard';
import RefreshButton from '../../src/ui/RefreshButton';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';
import { colors, mono, radius, space } from '../../src/ui/theme';

/**
 * Offers tab: the player half of the **virtual offer federation**.
 *
 * A campaign lane attaches an offer to the message it sends; the campaign runtime grants it to
 * each recipient as the send goes out; this screen is where the player acts on it. Which store
 * answers is a federation — an extension point, not a Beamable feature — so the federation id is
 * an input and nothing here branches on its value.
 *
 * **What "acting on it" means, and why there are two calls.** An offer IS a storefront listing.
 * The price and the bundle both move through the platform's commerce flow, and the campaign-offer
 * claim only settles the grant afterwards — marking it redeemed and forfeiting the siblings it was
 * an alternative to. So acting on an offer is **buy, then claim**, and this screen does both in one
 * press because two presses for one act reads as a bug.
 *
 * The wallet sits above the offers deliberately: a purchase should read before → action → after
 * in one downward glance, and "what did I actually receive" is answered by diffing the wallet
 * around the purchase, not by the purchase response (which carries no deltas on this route).
 */
export default function OffersTab() {
  const { append } = useLogActions();
  const { isReady } = useBeam();
  const { lastOfferGrantId } = useNotifications();

  const [federationId, setFederationId] = useState<string>(DEFAULT_STORE);
  const [entitlements, setEntitlements] = useState<Entitlement[]>([]);
  const [balances, setBalances] = useState<Balance[]>([]);
  const [walletDeltas, setWalletDeltas] = useState<Record<string, bigint>>({});
  const [receipts, setReceipts] = useState<Record<string, BalanceDelta[]>>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Tracked separately from `error`: the wallet and the entitlement list fail for different
  // reasons and one must not hide the other. Reading currency needs the platform's `inventory`
  // service, which a trimmed local stack often does not run — an empty wallet with no explanation
  // looks like "you have nothing" rather than "this could not be read".
  const [walletError, setWalletError] = useState<string | null>(null);
  const inFlight = useRef(false);

  const store = federationId.trim() || DEFAULT_STORE;
  const claimable = useMemo(() => entitlements.filter(isClaimable).length, [entitlements]);

  /**
   * One read for the whole screen, under a single in-flight guard so a focus change cannot
   * interleave two of them. Entitlements are never cached across a session: expiry is evaluated
   * server-side on read, so a stale list is wrong by construction.
   *
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
      // A manual refresh is the user asking "what is true now", so the change chips from an
      // earlier purchase are cleared. The silent refresh that follows a purchase keeps them —
      // they are the whole point of that read.
      if (!silent) setWalletDeltas({});
      try {
        // The wallet read is best-effort: a realm with no currency content still has offers worth
        // showing, so a failure there must not take the entitlement list down with it — but it is
        // reported rather than swallowed.
        const [held, wallet, knownIds] = await Promise.all([
          listEntitlements(store),
          readBalances().then(
            (b) => {
              setWalletError(null);
              return b;
            },
            (e: unknown) => {
              setWalletError(e instanceof Error ? e.message : String(e));
              return [] as Balance[];
            },
          ),
          knownCurrencyIds(),
        ]);

        setEntitlements(held);
        setBalances(mergeKnownCurrencies(wallet, knownIds));
        if (!silent) append(`Entitlements (${store}): ${held.length}`);
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

  /** Settle a grant, then re-read so the row flips to its new state in place. */
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

  /**
   * Buy the listing behind a grant, then settle the grant — one press, one receipt.
   *
   * The order matters and the error handling more so: once the purchase returns, the player's
   * currency is already spent, so a failure to settle afterwards is reported as a warning line
   * **inside a successful receipt**, never as a thrown error. Losing the receipt after taking
   * someone's currency is the worst outcome available here.
   */
  const buy = useCallback(
    (entitlement: Entitlement) => async () => {
      if (!isReady) throw new Error('Beamable is not connected yet');

      const listing = entitlement.listings[0];
      if (!listing) throw new Error('This offer has no listing to buy.');

      const before = await readBalances().catch(() => [] as Balance[]);
      const purchaseId = purchaseIdFor(listing.listingSymbol, listing.storeSymbol);

      await purchaseListing(purchaseId);

      // The purchase response carries no inventory deltas on this route, so the wallet is the
      // source of truth for what moved. It also captures the price — which for a virtual offer is
      // itself a currency — so the debit and the payout show together in one receipt.
      const after = await readBalances().catch(() => before);
      const moved = diffBalances(before, after);

      setBalances((current) => mergeKnownCurrencies(after, current.map((b) => b.id)));
      setWalletDeltas(Object.fromEntries(moved.map((d) => [d.id, d.change])));
      setReceipts((current) => ({ ...current, [entitlement.grantId]: moved }));

      let settled = '';
      try {
        await claimGrant(store, entitlement.grantId);
        settled = ' · grant settled';
      } catch (e) {
        // Paid but not settled. Surface it without discarding the purchase.
        const msg = e instanceof Error ? e.message : String(e);
        settled = ` · ⚠ bought, but the grant did not settle: ${msg}`;
      }

      void refresh(true);

      const summary = moved.length
        ? moved.map((d) => `${d.change > 0n ? '+' : ''}${formatAmount(d.change)} ${d.label}`).join(', ')
        : 'no wallet change';
      return `Purchased ${purchaseId} — ${summary}${settled}`;
    },
    [isReady, store, refresh],
  );

  return (
    <Screen>
      <Section title="Store">
        <Hint>
          Which store answers is a federation — a microservice implementing
          IFederatedCampaignVirtualOffer under its own id. `beamable_virtual_store` is the default
          Beamable ships; a game with its own virtual economy deploys its own and is reached by
          these same two calls. Nothing on this screen branches on the value, which is why it is an
          input.
        </Hint>
        <Field
          value={federationId}
          onChangeText={setFederationId}
          placeholder={DEFAULT_STORE}
          accessibilityLabel="Store federation id"
        />
      </Section>

      <Section
        title="Wallet"
        right={<RefreshButton busy={busy} onPress={() => void refresh()} label="Refresh wallet" />}
      >
        <Hint>
          Reads GET /object/inventory/{'{playerId}'}/?scope=currency. Every currency the realm
          publishes is listed, including ones you hold none of — a purchase needs a row to move.
          Nothing here changes when you Claim: on `beamable_virtual_store` the claim only settles
          the grant. It changes when you Buy — which for a virtual offer moves this twice, once to
          pay and once to receive.
        </Hint>
        {walletError && (
          <Text style={styles.error} selectable>
            ✕ {walletError}
          </Text>
        )}
        {balances.length === 0 ? (
          <Hint>
            {walletError
              ? 'Could not read the wallet. The platform `inventory` service serves this route — a trimmed local stack may not be running it.'
              : 'No currencies read yet.'}
          </Hint>
        ) : (
          balances.map((balance) => {
            const delta = walletDeltas[balance.id];
            return (
              <BalanceRow
                key={balance.id}
                label={balance.label}
                amount={formatAmount(balance.amount)}
                delta={delta ? `${delta > 0n ? '+' : ''}${formatAmount(delta)}` : undefined}
                tone={delta && delta < 0n ? 'down' : 'up'}
              />
            );
          })
        )}
      </Section>

      <Section
        title={`Entitlements (${entitlements.length})`}
        right={
          <RefreshButton busy={busy} onPress={() => void refresh()} label="Refresh entitlements" />
        }
      >
        <Hint>
          Reads beam.campaignOffer.getEntitlements(federationId) → GET /api/campaign-offer/entitlements.
          Every grant in every state, so read the state rather than the presence of a row —
          claimed and revoked ones stay in the list. The store embeds the whole offer, so this one
          call renders the screen; a provider may send none, and those rows fall back to ids.
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
              {claimable} of {entitlements.length} ready to act on.
            </Hint>
            {entitlements.map((e) => (
              <OfferCard
                key={e.grantId}
                entitlement={e}
                receipt={receipts[e.grantId]}
                buy={isPurchasable(e) ? buy(e) : undefined}
                claim={isClaimable(e) ? claim(e.grantId) : undefined}
              />
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
            <Hint>
              Claim settles a grant you have already paid for. A push deep-link cannot buy on its
              own — the listing to charge for lives on the entitlement, so a paid offer is bought
              from the list above.
            </Hint>
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

      <Section title="How buying works">
        <Collapsible title="One call, one inventory transaction">
          <Hint>
            Buy posts to POST /object/commerce/{'{playerId}'}/purchase with the listing's
            `{'{listing}:{store}'}` id. Commerce resolves the listing, checks it is active and that
            you meet its requirements and purchase limits, then debits the price and credits the
            payout in a single inventory transaction. This is a real purchase — there is no test
            handler and no realm flag standing in for a store.
          </Hint>
          <Hint>
            This is the virtual federation, so a price is a currency amount. A real-money offer is a
            separate federation with its own contract: it needs platform product ids, a native
            purchase flow and receipt verification, none of which exist here. A SKU-priced listing
            is refused by the provider rather than shown.
          </Hint>
          <Hint>
            Buy then settles the grant, because nothing calls the federation's settlement callback
            on this path. The provider verifies the purchase against your commerce purchase history
            before settling, so a claim without a purchase is refused — which is what stops a claim
            forfeiting the alternatives it was offered against for free.
          </Hint>
        </Collapsible>
      </Section>
    </Screen>
  );
}

const styles = StyleSheet.create({
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
