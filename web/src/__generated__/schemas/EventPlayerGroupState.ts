import { EventRewardState } from './EventRewardState';

export type EventPlayerGroupState = { 
  groupRank: bigint | string; 
  groupScore: number; 
  rankRewards: EventRewardState[]; 
  scoreRewards: EventRewardState[]; 
  groupId?: string; 
};
