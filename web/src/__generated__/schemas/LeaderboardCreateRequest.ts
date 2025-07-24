/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ClientPermission } from './ClientPermission';
import type { LeaderboardCohortSettings } from './LeaderboardCohortSettings';

export type LeaderboardCreateRequest = { 
  sharded: boolean; 
  cohortSettings?: LeaderboardCohortSettings; 
  derivatives?: string[]; 
  freezeTime?: bigint | string; 
  maxEntries?: number; 
  partitioned?: boolean; 
  permissions?: ClientPermission; 
  scoreName?: string; 
  ttl?: bigint | string; 
};
