/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignOfferDto } from './CampaignOfferDto';
import type { CampaignOfferGroupDto } from './CampaignOfferGroupDto';
import type { GoalDto } from './GoalDto';

export type SendBodyDto = { 
  campaignOfferGroups?: CampaignOfferGroupDto[] | null; 
  campaignOffers?: CampaignOfferDto[] | null; 
  deliveryMaxWaitMs?: bigint | string | null; 
  goals?: GoalDto[]; 
  maxWaitMs?: bigint | string; 
  message?: string; 
  railFederationId?: string; 
  targetPid?: string; 
};
