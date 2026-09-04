import { useCallback, useMemo, useRef, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from 'expo-router';

import {
  DEFAULT_STORE,
  OFFER_GRANT_KEY,
  claimGrant,
  isClaimable,
  isPurchasable,
  listCampaignOffers,
  type CampaignOffer,
} from '../../src/beam/campaignOffers';
import {
  currencyLabel,
  diffBalances,
  formatAmount,
  knownCurrencyIds,
  grantCurrency,
  mergeKnownCurrencies,
  parseAmount,
  readBalances,
  type Balance,
  type BalanceDelta,
} from '../../src/beam/inventory';
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
 *
 * The wallet can also grant. Every offer below is priced in soft currency, so a fresh player holds
 * nothing to pay with and every claim is refused for insufficient funds — which reads as a broken
 * screen rather than as an empty wallet. The ⊕ on each row tops that currency up, so the whole
 * federation can be exercised from the app instead of from the Portal. That grant is a
 * microservice call (`DebugWalletService`) rather than a client one, because currency content
 * decides who may credit a wallet and it is never the client.
 */
export default function OffersTab() {
  const { append } = useLogActions();
  const { isReady } = useBeam();
  const { lastOfferGrantId } = useNotifications();

  const [federationId, setFederationId] = useState<string>(DEFAULT_STORE);
  const [campaignOffers, setCampaignOffers] = useState<CampaignOffer[]>([]);
  const [balances, setBalances] = useState<Balance[]>([]);
  const [walletDeltas, setWalletDeltas] = useState<Record<string, bigint>>({});
  const [receipts, setReceipts] = useState<Record<string, BalanceDelta[]>>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Tracked separately from `error`: the wallet and the campaign-offer list fail for different
  // reasons and one must not hide the other. Reading currency needs the platform's `inventory`
  // service, which a trimmed local stack often does not run — an empty wallet with no explanation
  // looks like "you have nothing" rather than "this could not be read".
  const [walletError, setWalletError] = useState<string | null>(null);
  // How much one ⊕ grants. A string because it is a text field: it is parsed on press, so a typo
  // is reported rather than silently becoming zero.
  const [grantAmount, setGrantAmount] = useState('100');
  const [addingId, setAddingId] = useState<string | null>(null);
  // A third error line, for the same reason `walletError` is a second one: "could not read the
  // wallet" and "the platform refused this grant" are different sentences with different fixes.
  const [grantError, setGrantError] = useState<string | null>(null);
  const inFlight = useRef(false);

  const store = federationId.trim() || DEFAULT_STORE;
  const claimable = useMemo(() => campaignOffers.filter(isClaimable).length, [campaignOffers]);

  /**
   * One read for the whole screen, under a single in-flight guard so a focus change cannot
   * interleave two of them. Campaign offers are never cached across a session: expiry is evaluated
   * server-side on read, so a stale list is wrong by construction.
   *
   * `silent` suppresses the "not connected" line and the loading chatter, so the automatic
   * on-focus refresh doesn't spam the Activity log every time you switch tabs.
   */
  const refresh = useCallback(
    async (silent = false) => {
      if (!isReady) {
        if (!silent) append('Campaign offers: Beamable is not connected yet');
        return;
      }
      if (inFlight.current) return;
      inFlight.current = true;
      setBusy(true);
      setError(null);
      // A manual refresh is the user asking "what is true now", so the change chips from an
      // earlier purchase are cleared. The silent refresh that follows a purchase keeps them —
      // they are the whole point of that read.
      if (!silent) {
        setWalletDeltas({});
        setGrantError(null);
      }
      try {
        // The wallet read is best-effort: a realm with no currency content still has offers worth
        // showing, so a failure there must not take the campaign-offer list down with it — but it
        // is reported rather than swallowed.
        const [held, wallet, knownIds] = await Promise.all([
          listCampaignOffers(store),
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

        setCampaignOffers(held);
        setBalances(mergeKnownCurrencies(wallet, knownIds));
        if (!silent) append(`Campaign offers (${store}): ${held.length}`);
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        // Shown in the section, not just the collapsed console — the on-focus refresh is
        // silent, so a failure would otherwise be indistinguishable from "you hold nothing".
        setError(msg);
        append(`Campaign offers error: ${msg}`);
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
   * Top this currency up by the amount in the field, through `DebugWalletService`.
   *
   * The grant cannot be a client call: `CurrencyContent.clientPermission.write_self` gates
   * inventory writes and is off for any currency worth having, so a microservice does it — see
   * `inventory.ts`. Reading the wallet, on the other hand, is client-side, which is why only this
   * half of the section needs a service at all.
   *
   * Not an `AsyncButton`: the control is a ⊕ on a list row, so the in-flight state and the failure
   * live on the screen rather than under the button — `addingId` swaps that one row's ⊕ for a
   * spinner, and `grantError` renders once above the list however many rows there are.
   *
   * The wallet is read either side and diffed, exactly as `buy` does below. The service does
   * return the new balance, but the whole list has to be re-read anyway to be sure nothing else
   * moved — so one mechanism answers "what moved" for both actions, and the ⊕ lights up the same
   * green chip a purchase does.
   */
  const addFunds = useCallback(
    (id: string) => () => {
      if (addingId) return;

      void (async () => {
        setGrantError(null);
        setAddingId(id);
        try {
          if (!isReady) throw new Error('Beamable is not connected yet');
          // Parsed before anything is sent, so a typo costs no round trip.
          const amount = parseAmount(grantAmount);

          const before = await readBalances();
          const balance = await grantCurrency(id, amount);
          const after = await readBalances();
          const moved = diffBalances(before, after);

          // `mergeKnownCurrencies` against the ids already on screen keeps the `0` rows for
          // currencies the player still holds none of — they are what the next ⊕ is pressed on.
          setBalances((current) => mergeKnownCurrencies(after, current.map((b) => b.id)));
          setWalletDeltas(Object.fromEntries(moved.map((d) => [d.id, d.change])));
          // The service's own figure, not the diff's: if the two disagree, something else moved
          // this currency at the same time, and the log is where that is worth seeing.
          append(
            `Wallet: +${formatAmount(amount)} ${currencyLabel(id)} → ${formatAmount(balance)}`,
          );
        } catch (e) {
          const msg = e instanceof Error ? e.message : String(e);
          setGrantError(msg);
          append(`Add currency error: ${msg}`);
        } finally {
          setAddingId(null);
        }
      })();
    },
    [addingId, isReady, grantAmount, append],
  );

  /**
   * Claim a grant — one press, one call, one receipt.
   *
   * **The claim IS the purchase.** The provider spends on the player's behalf: commerce debits the
   * price and credits the payout in one inventory transaction. So this screen must NOT buy the
   * listing itself first — doing so charges the player twice, or gets refused by a purchase limit
   * and then reports the refusal as "bought, but the grant did not settle", which points at exactly
   * the wrong cause.
   *
   * The wallet is read either side because `InventoryUpdateResponse.deltas` is not populated on
   * this path, so the diff is the only truthful account of what moved — and it captures the price
   * as well as the payout, since for a virtual offer both are currency.
   */
  const buy = useCallback(
    (campaignOffer: CampaignOffer) => async () => {
      if (!isReady) throw new Error('Beamable is not connected yet');

      const before = await readBalances().catch(() => [] as Balance[]);

      const settled = await claimGrant(store, campaignOffer.grantId);

      const after = await readBalances().catch(() => before);
      const moved = diffBalances(before, after);

      setBalances((current) => mergeKnownCurrencies(after, current.map((b) => b.id)));
      setWalletDeltas(Object.fromEntries(moved.map((d) => [d.id, d.change])));
      setReceipts((current) => ({ ...current, [campaignOffer.grantId]: moved }));

      void refresh(true);

      const summary = moved.length
        ? moved.map((d) => `${d.change > 0n ? '+' : ''}${formatAmount(d.change)} ${d.label}`).join(', ')
        : 'no wallet change';
      return `${settled} — ${summary}`;
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
        <Hint>
          ⊕ grants this much of that currency — and it goes through the `DebugWalletService`
          microservice, not through this client. Crediting a wallet is the currency content's call,
          not the token's: `clientPermission.write_self` is off for anything worth holding, so the
          platform refuses a client write however the request is shaped. A service runs with the
          privileged identity that check does not apply to. Reading the wallet needs none of that,
          which is why only half of this section is server-side. Top up an offer's price currency
          and the Buy below stops failing for insufficient funds.
        </Hint>
        <View style={styles.grantRow}>
          <Text style={styles.grantLabel}>grant</Text>
          <Field
            value={grantAmount}
            onChangeText={setGrantAmount}
            placeholder="100"
            keyboardType="number-pad"
            accessibilityLabel="Grant amount"
            style={styles.grantField}
          />
        </View>
        {grantError && (
          <Text style={styles.error} selectable>
            ✕ {grantError}
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
                onAdd={addFunds(balance.id)}
                adding={addingId === balance.id}
              />
            );
          })
        )}
      </Section>

      <Section
        title={`Campaign Offers (${campaignOffers.length})`}
        right={
          <RefreshButton
            busy={busy}
            onPress={() => void refresh()}
            label="Refresh campaign offers"
          />
        }
      >
        <Hint>
          Reads beam.campaignOffer.getCampaignOffers(federationId) → GET /api/campaign-offer/campaign-offers.
          Every grant in every state, so read the state rather than the presence of a row —
          claimed and revoked ones stay in the list. The store embeds the whole offer, so this one
          call renders the screen; a provider may send none, and those rows fall back to ids.
        </Hint>
        {error && (
          <Text style={styles.error} selectable>
            ✕ {error}
          </Text>
        )}
        {campaignOffers.length === 0 ? (
          <Hint>
            {error
              ? 'Campaign offers not loaded.'
              : `Nothing granted by "${store}" yet. Publish a campaign lane with an offer and enrol this player.`}
          </Hint>
        ) : (
          <>
            <Hint>
              {claimable} of {campaignOffers.length} ready to act on.
            </Hint>
            {campaignOffers.map((e) => (
              <OfferCard
                key={e.grantId}
                campaignOffer={e}
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
          given. This reads it off the received notification — no campaign-offer list needed.
        </Hint>
        {lastOfferGrantId ? (
          <>
            <Value label="grant">{lastOfferGrantId}</Value>
            <AsyncButton label="Claim from push" run={claim(lastOfferGrantId)} />
            <Hint>
              Claim settles a grant you have already paid for. A push deep-link cannot buy on its
              own — the listing to charge for lives on the grant, so a paid offer is bought
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
            Claiming IS the purchase. This screen makes one call — redeem — and the provider spends
            on your behalf: commerce debits the price and credits the payout in a single inventory
            transaction, then forfeits any offers this one was an alternative to. This sample never
            posts to commerce itself; doing so would charge you twice.
          </Hint>
          <Hint>
            The provider buys rather than checking that a client already did, because redeem is
            client-callable: taking the caller's word would let anyone settle a grant for free and
            destroy the siblings they were meant to choose between. Performing the purchase removes
            the question instead of answering it.
          </Hint>
          <Hint>
            This is the virtual federation, so a price is a currency amount. A real-money offer is a
            separate federation with its own contract: it needs platform product ids, a native
            purchase flow and receipt verification, none of which exist here. A SKU-priced listing
            is refused by the provider rather than shown.
          </Hint>
          <Hint>
            An offer can arrive locked. A campaign can gate one on a requirement, and that gate is
            re-checked every time this list loads — so an offer you cannot claim today unlocks on
            its own once you qualify, with no new message.
          </Hint>
        </Collapsible>
      </Section>
    </Screen>
  );
}

const styles = StyleSheet.create({
  grantRow: { flexDirection: 'row', alignItems: 'center', gap: space.sm },
  grantLabel: { color: colors.muted, fontSize: 12, fontFamily: mono },
  grantField: { flex: 1 },
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
