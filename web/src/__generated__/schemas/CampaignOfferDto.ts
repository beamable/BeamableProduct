/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignConditionDto } from './CampaignConditionDto';

export type CampaignOfferDto = { 
  condition?: CampaignConditionDto; 
  durationMs?: bigint | string | null; 
  entryId?: string; 
  extraData?: Record<string, string> | null; 
  federationId?: string; 
  invalidatesEntryIds?: string[] | null; 
  label?: string | null; 
  offerId?: string; 
  required?: boolean; 
};
