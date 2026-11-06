/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignOfferGroupMode } from './enums/CampaignOfferGroupMode';
import type { CampaignOfferInvalidation } from './enums/CampaignOfferInvalidation';

export type CampaignOfferGroupDto = { 
  id?: string; 
  invalidation?: CampaignOfferInvalidation; 
  label?: string | null; 
  memberEntryIds?: string[]; 
  mode?: CampaignOfferGroupMode; 
};
