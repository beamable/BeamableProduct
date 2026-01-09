/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ClientPermission } from './ClientPermission';
import type { LeaderboardCohortSettings } from './LeaderboardCohortSettings';

export type MetadataView = { 
  cohorted: boolean; 
  frozen: boolean; 
  parentLeaderboard: string; 
  partitioned: boolean; 
  cohortSettings?: LeaderboardCohortSettings; 
  derivatives?: string[]; 
  expiration?: bigint | string; 
  freezeTime?: bigint | string; 
  maxEntries?: number; 
  permissions?: ClientPermission; 
};
