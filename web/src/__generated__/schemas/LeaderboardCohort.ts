import { PlayerStatRequirement } from './PlayerStatRequirement';

export type LeaderboardCohort = { 
  id: string; 
  statRequirements: PlayerStatRequirement[]; 
  description?: string; 
};
