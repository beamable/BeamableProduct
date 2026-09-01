/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignOfferItem } from './CampaignOfferItem';
import type { CampaignOfferListingRef } from './CampaignOfferListingRef';
import type { CampaignOfferReason } from './CampaignOfferReason';

export type CampaignOfferEntitlement = { 
  available?: boolean; 
  expiresAtUnixSeconds?: bigint | string; 
  grantId?: string; 
  grantedAtUnixSeconds?: bigint | string; 
  listings?: CampaignOfferListingRef[]; 
  offer?: CampaignOfferItem; 
  offerId?: string; 
  state?: string; 
  unavailableReasons?: CampaignOfferReason[]; 
};