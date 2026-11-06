/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignOfferItem } from './CampaignOfferItem';
import type { CampaignOfferReason } from './CampaignOfferReason';
import type { CampaignOfferState } from './enums/CampaignOfferState';

export type CampaignOffer = { 
  grantId: string; 
  offerId: string; 
  available?: boolean; 
  conditionToken?: string | null; 
  expiresAtUnixSeconds?: bigint | string; 
  grantedAtUnixSeconds?: bigint | string; 
  offer?: CampaignOfferItem; 
  state?: CampaignOfferState; 
  unavailableReasons?: CampaignOfferReason[]; 
};
