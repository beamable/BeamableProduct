/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TagList } from './TagList';

export type Party = { 
  created?: Date | null; 
  id?: string | null; 
  leader?: string | null; 
  maxSize?: number; 
  members?: string[] | null; 
  membersTags?: Record<string, TagList> | null; 
  pendingInvites?: string[] | null; 
  restriction?: string | null; 
};
