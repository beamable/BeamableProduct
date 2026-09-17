/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { GoalDto } from './GoalDto';

export type SendBodyDto = { 
  deliveryMaxWaitMs?: bigint | string | null; 
  goals?: GoalDto[]; 
  maxWaitMs?: bigint | string; 
  message?: string; 
  offer?: string | null; 
  railFederationId?: string; 
  targetPid?: string; 
};
