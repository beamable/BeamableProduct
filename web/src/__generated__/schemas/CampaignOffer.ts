import type { CampaignOfferItem } from './CampaignOfferItem';
import type { CampaignOfferReason } from './CampaignOfferReason';
import type { CampaignOfferState } from './CampaignOfferState';

export type CampaignOffer = {
  /**
   * Whether the player can act on this right now — the store's rules AND the campaign's gate.
   * Distinct from `state`: a `Granted` offer whose condition is unmet reports `false` here and
   * unlocks on a later read, without a new send.
   */
  available?: boolean;
  expiresAtUnixSeconds?: bigint | string;
  grantId?: string;
  grantedAtUnixSeconds?: bigint | string;
  /** The offer in full, so one call renders a store screen. May be absent — write the fallback. */
  offer?: CampaignOfferItem;
  offerId?: string;
  state?: CampaignOfferState;
  /** Why `available` is false. May carry more than one reason. */
  unavailableReasons?: CampaignOfferReason[];
};
