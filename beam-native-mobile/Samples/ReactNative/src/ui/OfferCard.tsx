import { StyleSheet, Text, View } from 'react-native';

import type { CampaignOffer } from '../beam/campaignOffers';
import { describeState, formatExpiry, formatWhen } from '../beam/campaignOffers';
import type { BalanceDelta } from '../beam/inventory';
import { formatDelta } from '../beam/inventory';
import AsyncButton from './AsyncButton';
import { Hint, Value } from './Hint';
import { colors, mono, radius, space } from './theme';

/**
 * One campaign offer, as a player should see it.
 *
 * The card exists because the row grew past what a tab file should hold inline — the same reason
 * `StatCard` was lifted out of the Segments tab. It owns no network state: both actions are
 * `AsyncButton`s, which already own their in-flight and result rendering.
 *
 * **Everything the store sends is optional.** A provider may omit the offer, the cost, or the
 * rewards, and a third-party store legitimately will. So each block degrades on its own rather
 * than being guarded as a whole: with no offer the card is the opaque grant id it always was,
 * which is still useful, rather than an empty box.
 */
export default function OfferCard({
  campaignOffer,
  receipt,
  buy,
  claim,
}: {
  /**
   * The grant. Named in full rather than `offer` because the store's offer hangs off it as
   * `campaignOffer.offer`, and one word for both would make this file unreadable.
   */
  campaignOffer: CampaignOffer;
  /** What the last purchase on this row moved, if there was one this session. */
  receipt?: BalanceDelta[];
  /** Present only when this sample can actually complete the purchase. */
  buy?: () => Promise<string>;
  /** Present only when the grant is still claimable. */
  claim?: () => Promise<string>;
}) {
  const { offer } = campaignOffer;
  const isClaimableState = campaignOffer.state === 'Granted';
  // One source now, not a fallback chain: the price lives on the offer, beside what it pays out.
  const priceLabel = offer?.priceLabel || '';

  return (
    <View style={styles.card}>
      <Text style={styles.state}>{describeState(campaignOffer.state)}</Text>

      {offer ? (
        <>
          <Text style={styles.title}>{offer.title || campaignOffer.offerId}</Text>
          {!!offer.description && <Text style={styles.description}>{offer.description}</Text>}
        </>
      ) : (
        // The contract allows a null offer. Show the identifier as an identifier — never dress an
        // opaque id up as a name.
        <Text style={styles.titleFallback}>Offer {campaignOffer.offerId || '—'}</Text>
      )}

      {!!priceLabel && <Text style={styles.price}>{priceLabel}</Text>}

      {/* What the bundle contains. Absent for a store that cannot enumerate its payout — which is
          not the same as an offer that gives nothing, so the empty case says nothing at all
          rather than "no rewards". */}
      {!!offer?.rewards.length && (
        <View style={styles.rewards}>
          <Text style={styles.rewardsLabel}>You get</Text>
          {offer.rewards.map((reward, i) => (
            <Text key={`${reward.type}:${reward.symbol}:${i}`} style={styles.reward}>
              {/* 0 means "not known until fulfilment" (a loot roll), so it must not read as "0 of". */}
              {reward.amount > 0 ? `${reward.amount} × ` : ''}
              {reward.label}
              {reward.amount === 0 ? ' (contents vary)' : ''}
              {Object.keys(reward.properties).length > 0
                ? `  ${describeProperties(reward.properties)}`
                : ''}
            </Text>
          ))}
        </View>
      )}

      {/* Distinct from state: a granted grant can still be unactionable. */}
      {!campaignOffer.available && campaignOffer.reasons.length > 0 && (
        <View style={styles.reasons}>
          {campaignOffer.reasons.map((reason, i) => (
            <Text key={`${reason.code}:${i}`} style={styles.reason}>
              {reason.message || reason.code}
              {reason.detail ? ` (${reason.detail})` : ''}
            </Text>
          ))}
        </View>
      )}

      <Value label="offer">{campaignOffer.offerId || '—'}</Value>
      <Value label="grant">{campaignOffer.grantId}</Value>
      <Text style={styles.meta}>
        granted {formatWhen(campaignOffer.grantedAt)} · {formatExpiry(campaignOffer.expiresAt)}
      </Text>

      {/* The store's own fields, rendered generically. This is the only thing that makes the
          contract's escape hatch testable: a provider can add data and see it without any
          change here. */}
      {!!offer && Object.keys(offer.properties).length > 0 && (
        <Text style={styles.properties}>{describeProperties(offer.properties)}</Text>
      )}

      {buy && (
        <AsyncButton label={priceLabel ? `Buy — ${priceLabel}` : 'Buy'} run={buy} />
      )}

      {/* Held but not yet actionable — a store rule or the campaign's own gate. The reasons above
          say which, and the row stays: the gate is re-checked on every read, so this unlocks by
          itself once the player qualifies. Saying that is the difference between "not yet" and
          "not for you". */}
      {!buy && isClaimableState && !campaignOffer.available && (
        <Hint>
          You cannot claim this yet. It unlocks on its own once you meet the requirements above —
          no need to wait for another message.
        </Hint>
      )}

      {claim && <AsyncButton label="Claim" run={claim} variant="secondary" />}

      {!!receipt?.length && (
        <View style={styles.receipt}>
          <Text style={styles.receiptLabel}>Received</Text>
          {receipt.map((delta) => (
            <Text
              key={delta.id}
              style={[styles.receiptLine, delta.change < 0n ? styles.spent : styles.gained]}
            >
              {formatDelta(delta.change)} {delta.label}
            </Text>
          ))}
        </View>
      )}
    </View>
  );
}

/** `{ rarity: 'epic' }` → `rarity=epic`. Generic on purpose — the keys are the store's, not ours. */
function describeProperties(properties: Record<string, string>): string {
  return Object.entries(properties)
    .map(([key, value]) => `${key}=${value}`)
    .join(' · ');
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.card,
    borderColor: colors.surfaceBorder,
    borderWidth: 1,
    borderRadius: radius.lg,
    padding: space.lg,
    gap: space.xs,
  },
  state: { color: colors.muted, fontSize: 11, fontFamily: mono, textTransform: 'uppercase' },
  title: { color: colors.ink, fontSize: 16, fontWeight: '700' },
  titleFallback: { color: colors.ink, fontSize: 14, fontWeight: '600', fontFamily: mono },
  description: { color: colors.inkSoft, fontSize: 13 },
  price: { color: colors.ink, fontSize: 15, fontWeight: '700' },
  rewards: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    padding: space.md,
    gap: 2,
    marginTop: space.xs,
  },
  rewardsLabel: { color: colors.muted, fontSize: 11, fontFamily: mono, textTransform: 'uppercase' },
  reward: { color: colors.ink, fontSize: 13 },
  reasons: { gap: 2 },
  reason: { color: colors.warn, fontSize: 12 },
  meta: { color: colors.muted, fontSize: 12, fontFamily: mono },
  properties: { color: colors.mutedSoft, fontSize: 11, fontFamily: mono },
  receipt: {
    backgroundColor: colors.okBg,
    borderColor: colors.okBorder,
    borderWidth: 1,
    borderRadius: radius.md,
    padding: space.md,
    gap: 2,
    marginTop: space.xs,
  },
  receiptLabel: { color: colors.muted, fontSize: 11, fontFamily: mono, textTransform: 'uppercase' },
  receiptLine: { fontSize: 13, fontFamily: mono, fontWeight: '700' },
  gained: { color: colors.okInk },
  spent: { color: colors.errorInk },
});
