/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CompletedStatus } from './CompletedStatus';

export type GroupStatus = { 
  contentId: string; 
  groupId: bigint | string; 
  lastUpdateCycle: number; 
  stage: number; 
  tier: number; 
  tournamentId: string; 
  completed?: CompletedStatus[]; 
};
