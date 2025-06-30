import { ClientPermission } from './ClientPermission';
import { LeaderboardCohortSettings } from './LeaderboardCohortSettings';

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
