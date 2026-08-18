/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignChangeDto } from './CampaignChangeDto';
import type { CampaignChangeErrorDto } from './CampaignChangeErrorDto';

export type PublishResponseDto = { 
  changes?: CampaignChangeDto[]; 
  errors?: CampaignChangeErrorDto[]; 
  isNewVersion?: boolean; 
  ok?: boolean; 
  version?: number | null; 
};
