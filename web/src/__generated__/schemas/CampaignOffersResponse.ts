import type { CampaignOffer } from './CampaignOffer';

export type CampaignOffersResponse = {
  /** The contract version this was produced against, so skew is detectable rather than a parse bug. */
  contractVersion?: number;
  offers?: CampaignOffer[];
  playerId?: string;
};
