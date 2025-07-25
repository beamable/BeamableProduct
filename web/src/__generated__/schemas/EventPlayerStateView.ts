/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { EventPlayerGroupState } from './EventPlayerGroupState';
import type { EventPlayerPhaseView } from './EventPlayerPhaseView';
import type { EventRewardState } from './EventRewardState';

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
