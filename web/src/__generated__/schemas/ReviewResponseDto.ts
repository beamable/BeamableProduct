/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignChangeErrorDto } from './CampaignChangeErrorDto';
import type { QueueConflictDto } from './QueueConflictDto';

export type ReviewResponseDto = { 
  conflicts?: QueueConflictDto[]; 
  errors?: CampaignChangeErrorDto[]; 
  ok?: boolean; 
  version?: number | null; 
};
