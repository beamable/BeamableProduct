/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignOfferAmount } from './CampaignOfferAmount';
import type { CampaignOfferReason } from './CampaignOfferReason';
import type { CampaignOfferText } from './CampaignOfferText';

export type CampaignOfferItem = { 
  offerId: string; 
  available?: boolean; 
  cost?: CampaignOfferAmount[]; 
  description?: string | null; 
  imageUrl?: string | null; 
  localizations?: Record<string, CampaignOfferText>; 
  priceLabel?: string | null; 
  properties?: Record<string, string>; 
  rewards?: CampaignOfferAmount[]; 
  tags?: string[]; 
  title?: string | null; 
  unavailableReasons?: CampaignOfferReason[]; 
};
