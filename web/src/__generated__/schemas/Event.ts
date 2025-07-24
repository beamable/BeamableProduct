/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ClientPermission } from './ClientPermission';
import type { EventGroupRewards } from './EventGroupRewards';
import type { EventPhase } from './EventPhase';
import type { EventRewardContent } from './EventRewardContent';
import type { LeaderboardCohortSettings } from './LeaderboardCohortSettings';
import type { Schedule } from './Schedule';

export type Event = { 
  name: string; 
  phases: EventPhase[]; 
  start_date: string; 
  symbol: string; 
  cohortSettings?: LeaderboardCohortSettings; 
  group_rewards?: EventGroupRewards; 
  partition_size?: number; 
  permissions?: ClientPermission; 
  rank_rewards?: EventRewardContent[]; 
  schedule?: Schedule; 
  score_rewards?: EventRewardContent[]; 
  stores?: string[]; 
};
