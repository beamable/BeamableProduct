/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AuthorDto } from './AuthorDto';

export type CampaignAuditDto = { 
  createdAt?: Date; 
  createdBy?: AuthorDto; 
  createdBySystem?: string; 
  lastUpdatedAt?: Date; 
  lastUpdatedBy?: AuthorDto; 
  overrodeConflicts?: boolean; 
  publishedAt?: Date | null; 
  publishedBy?: AuthorDto; 
  reviewNote?: string | null; 
  reviewedAt?: Date | null; 
  reviewedBy?: AuthorDto; 
  submittedAt?: Date | null; 
  submittedBy?: AuthorDto; 
};
