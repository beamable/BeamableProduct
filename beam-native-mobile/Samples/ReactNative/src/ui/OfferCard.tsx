import { useState } from 'react';
import { Image, StyleSheet, Text, View } from 'react-native';

import type { CampaignOffer, Reward } from '../beam/campaignOffers';
import {
  BADGE_REWARD_TYPE,
  describeState,
  formatExpiry,
  formatWhen,
  isFree,
} from '../beam/campaignOffers';
import type { BalanceDelta } from '../beam/inventory';
import { formatDelta } from '../beam/inventory';
import AsyncButton from './AsyncButton';
import { Hint, Value } from './Hint';
import { colors, mono, radius, space } from './theme';

/**
 * One campaign offer, as a player should see it.
 *
 * The card exists because the row grew past what a tab file should hold inline — the same reason
 * `StatCard` was lifted out of the Segments tab. It owns no network state: its action is an
 * `AsyncButton`, which already owns its in-flight and result rendering.
 *
 * **Everything the store sends is optional.** A provider may omit the offer, the cost, or the
 * rewards, and a third-party store legitimately will. So each block degrades on its own rather
 * than being guarded as a whole: with no offer the card is the opaque grant id it always was,
 * which is still useful, rather than an empty box.
 *
 * **Rendered by what the offer is, not by who sent it.** A cost means Buy; an empty cost means a
 * free Claim; a `badge` reward draws as a badge. The federation id never enters this file, so a
 * third-party provider that sends the same shapes renders the same way.
 */
export default function OfferCard({
  campaignOffer,
  receipt,
  act,
}: {
  /**
   * The grant. Named in full rather than `offer` because the store's offer hangs off it as
   * `campaignOffer.offer`, and one word for both would make this file unreadable.
   */
  campaignOffer: CampaignOffer;
  /** What the last claim on this row moved, if there was one this session. */
  receipt?: OfferReceipt;
  /**
   * The one action on this row — Buy or Claim, never both. Present only when actionable.
   *
   * There is deliberately no second button: for a priced offer the claim IS the purchase — both
   * would post the same redeem call — so two buttons for one act read as two different acts, and
   * the player has to guess which is which.
   */
  act?: () => Promise<string>;
}) {
  const { offer } = campaignOffer;
  const isClaimableState = campaignOffer.state === 'Granted';
  const free = isFree(campaignOffer);
  // One source now, not a fallback chain: the price lives on the offer, beside what it pays out.
  const priceLabel = offer?.priceLabel || (free ? 'Free' : '');
  // No embedded offer means an unknown price, so neither "Buy" nor "free" would be honest.
  const actLabel = free
    ? 'Claim (free)'
    : offer?.cost.length
      ? `Buy${priceLabel ? ` — ${priceLabel}` : ''}`
      : 'Claim';

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
          <Text style={styles.rewardsLabel}>{free ? "What you'll get" : 'You get'}</Text>
          {offer.rewards.map((reward, i) =>
            // Known types get a shape; anything else falls back to "N × label" — the type string is
            // open, so this must never be an exhaustive switch.
            reward.type === BADGE_REWARD_TYPE ? (
              <BadgeReward key={`${reward.type}:${reward.symbol}:${i}`} reward={reward} />
            ) : (
              <Text key={`${reward.type}:${reward.symbol}:${i}`} style={styles.reward}>
                {reward.amount} × {reward.label}
                {Object.keys(reward.properties).length > 0
                  ? `  ${describeProperties(reward.properties)}`
                  : ''}
              </Text>
            ),
          )}
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

      {act && <AsyncButton label={actLabel} run={act} />}

      {/* Held but not yet actionable — a store rule or the campaign's own gate. The reasons above
          say which, and the row stays: the gate is re-checked on every read, so this unlocks by
          itself once the player qualifies. Saying that is the difference between "not yet" and
          "not for you". */}
      {!act && isClaimableState && !campaignOffer.available && (
        <Hint>
          You cannot claim this yet. It unlocks on its own once you meet the requirements above —
          no need to wait for another message.
        </Hint>
      )}

      {!!receipt?.moved.length && (
        <View style={styles.receipt}>
          <Text style={styles.receiptLabel}>Received</Text>
          {receipt.moved.map((delta) => (
            <Text
              key={delta.id}
              style={[styles.receiptLine, delta.change < 0n ? styles.spent : styles.gained]}
            >
              {formatDelta(delta.change)} {delta.label}
            </Text>
          ))}
        </View>
      )}

      {/* Read back from stats, not taken from the claim response — the same "what actually landed"
          rule the wallet diff follows. A failed read is shown here, not as a failed claim: the
          claim did succeed. */}
      {receipt?.badges.map((badge) => (
        <View key={badge.key} style={badge.error ? styles.badgeReadError : styles.receipt}>
          <Text style={styles.receiptLabel}>Badge earned</Text>
          <Text style={styles.reward}>{badge.title}</Text>
          <Text style={styles.meta} selectable>
            {badge.error
              ? `stat ${badge.key} could not be read: ${badge.error}`
              : `stat ${badge.key} = ${badge.value ?? '(not set yet)'}`}
          </Text>
        </View>
      ))}
    </View>
  );
}

/** What one claim moved: the wallet diff, and each badge's stat as read back afterwards. */
export type OfferReceipt = {
  moved: BalanceDelta[];
  badges: { key: string; title: string; value?: string; error?: string }[];
};

/**
 * A `badge` reward: its image (or a lettered disc when there is none, or it fails to load), its
 * title, and the stat it is delivered as — not "1 × badge_…", which reads as a quantity of an id.
 */
function BadgeReward({ reward }: { reward: Reward }) {
  const [imageFailed, setImageFailed] = useState(false);
  const { statDomain, statAccess, statValue } = reward.properties;
  const where = statDomain && statAccess ? `${statDomain}.${statAccess} · ` : '';
  return (
    <View style={styles.badge}>
      {reward.imageUrl && !imageFailed ? (
        <Image
          source={{ uri: reward.imageUrl }}
          style={styles.badgeImage}
          onError={() => setImageFailed(true)}
          accessibilityLabel={reward.label}
        />
      ) : (
        <View style={[styles.badgeImage, styles.badgeFallback]}>
          <Text style={styles.badgeInitial}>{(reward.label[0] ?? '?').toUpperCase()}</Text>
        </View>
      )}
      <View style={styles.badgeText}>
        <Text style={styles.reward}>Badge · {reward.label}</Text>
        <Text style={styles.meta} selectable>
          {where}
          {reward.symbol}
          {statValue ? ` = ${statValue}` : ''}
        </Text>
      </View>
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
  badge: { flexDirection: 'row', alignItems: 'center', gap: space.sm },
  badgeImage: { width: 36, height: 36, borderRadius: 18 },
  badgeFallback: {
    backgroundColor: colors.card,
    borderColor: colors.primary,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badgeInitial: { color: colors.primary, fontSize: 15, fontWeight: '700' },
  badgeText: { flex: 1, gap: 2 },
  badgeReadError: {
    backgroundColor: colors.errorBg,
    borderColor: colors.errorBorder,
    borderWidth: 1,
    borderRadius: radius.md,
    padding: space.md,
    gap: 2,
    marginTop: space.xs,
  },
  gained: { color: colors.okInk },
  spent: { color: colors.errorInk },
});
