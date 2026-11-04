/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AuthorDto } from './AuthorDto';
import type { QueueConflictDto } from './QueueConflictDto';
import type { CampaignLifecycle } from './enums/CampaignLifecycle';

export type CampaignSummaryDto = { 
  acceptedRealms?: string[]; 
  archived?: boolean; 
  campaignId?: string; 
  createdAt?: Date | null; 
  createdBy?: AuthorDto; 
  endAt?: Date | null; 
  name?: string; 
  phase?: CampaignLifecycle; 
  publishedAt?: Date | null; 
  publishedBy?: AuthorDto; 
  queueCheck?: QueueConflictDto[] | null; 
  reviewedAt?: Date | null; 
  reviewedBy?: AuthorDto; 
  startAt?: Date | null; 
  submittedAt?: Date | null; 
  submittedBy?: AuthorDto; 
  version?: number; 
};
