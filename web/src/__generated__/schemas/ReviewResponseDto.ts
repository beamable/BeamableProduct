/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignChangeErrorDto } from './CampaignChangeErrorDto';
import type { QueueConflictDto } from './QueueConflictDto';
import type { UnboundedParamDto } from './UnboundedParamDto';

export type ReviewResponseDto = { 
  conflicts?: QueueConflictDto[]; 
  errors?: CampaignChangeErrorDto[]; 
  ok?: boolean; 
  unboundedParams?: UnboundedParamDto[]; 
  version?: number | null; 
};
