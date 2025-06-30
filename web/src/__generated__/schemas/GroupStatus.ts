import { CompletedStatus } from './CompletedStatus';

export type GroupStatus = { 
  contentId: string; 
  groupId: bigint | string; 
  lastUpdateCycle: number; 
  stage: number; 
  tier: number; 
  tournamentId: string; 
  completed?: CompletedStatus[]; 
};
