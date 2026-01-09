/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { GroupType } from './enums/GroupType';

export type GroupSearchRequest = { 
  type: GroupType; 
  enrollmentTypes?: string; 
  hasSlots?: boolean; 
  limit?: number; 
  name?: string; 
  offset?: number; 
  scoreMax?: bigint | string; 
  scoreMin?: bigint | string; 
  sortField?: string; 
  sortValue?: number; 
  subGroup?: boolean; 
  userScore?: bigint | string; 
};
