import { EventPlayerGroupState } from './EventPlayerGroupState';
import { EventPlayerPhaseView } from './EventPlayerPhaseView';
import { EventRewardState } from './EventRewardState';

export type EventPlayerStateView = { 
  allPhases: EventPlayerPhaseView[]; 
  id: string; 
  leaderboardId: string; 
  name: string; 
  rank: bigint | string; 
  rankRewards: EventRewardState[]; 
  running: boolean; 
  score: number; 
  scoreRewards: EventRewardState[]; 
  secondsRemaining: bigint | string; 
  currentPhase?: EventPlayerPhaseView; 
  groupRewards?: EventPlayerGroupState; 
};
