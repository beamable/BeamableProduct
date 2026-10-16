import type { CampaignOfferAmount } from './CampaignOfferAmount';
import type { CampaignOfferReason } from './CampaignOfferReason';
import type { CampaignOfferText } from './CampaignOfferText';

export type CampaignOfferItem = {
  available?: boolean;
  /**
   * What the player pays. Several entries are an AND. Empty means free — and is how "no cost" is
   * said, rather than by the absence of some other collection.
   */
  cost?: CampaignOfferAmount[];
  description?: string | null;
  imageUrl?: string | null;
  localizations?: Record<string, CampaignOfferText>;
  offerId: string;
  /** Already formatted for display. Never the only representation of a price — see `cost`. */
  priceLabel?: string | null;
  properties?: Record<string, string>;
  /** What the player gets. Disclosure, not a receipt: never reconcile it against what landed. */
  rewards?: CampaignOfferAmount[];
  tags?: string[];
  title?: string;
  unavailableReasons?: CampaignOfferReason[];
};
