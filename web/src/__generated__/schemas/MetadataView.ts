import { ClientPermission } from './ClientPermission';
import { LeaderboardCohortSettings } from './LeaderboardCohortSettings';

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
