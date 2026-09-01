/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignOfferListingRef } from './CampaignOfferListingRef';
import type { CampaignOfferReward } from './CampaignOfferReward';
import type { CampaignOfferText } from './CampaignOfferText';

export type CampaignOfferItem = { 
  offerId: string; 
  description?: string | null; 
  imageUrl?: string | null; 
  listings?: CampaignOfferListingRef[]; 
  localizations?: Record<string, CampaignOfferText>; 
  priceLabel?: string | null; 
  properties?: Record<string, string>; 
  rewards?: CampaignOfferReward[]; 
  tags?: string[]; 
  title?: string; 
};