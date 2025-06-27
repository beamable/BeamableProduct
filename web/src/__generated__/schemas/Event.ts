import { ClientPermission } from './ClientPermission';
import { EventGroupRewards } from './EventGroupRewards';
import { EventPhase } from './EventPhase';
import { EventRewardContent } from './EventRewardContent';
import { LeaderboardCohortSettings } from './LeaderboardCohortSettings';
import { Schedule } from './Schedule';

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
