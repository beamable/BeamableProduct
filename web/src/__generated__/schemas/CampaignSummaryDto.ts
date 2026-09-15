/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AuthorDto } from './AuthorDto';
import type { CampaignLifecycle } from './enums/CampaignLifecycle';

export type CampaignSummaryDto = { 
  campaignId?: string; 
  createdAt?: Date | null; 
  createdBy?: AuthorDto; 
  name?: string; 
  phase?: CampaignLifecycle; 
  publishedAt?: Date | null; 
  publishedBy?: AuthorDto; 
  version?: number; 
};
