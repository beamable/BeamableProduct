import { TournamentCurrencyReward } from './TournamentCurrencyReward';

export type AdminPlayerStatus = { 
  contentId: string; 
  nextCycleStartMs: bigint | string; 
  playerId: bigint | string; 
  rank: number; 
  score: number; 
  stage: number; 
  tier: number; 
  tournamentId: string; 
  unclaimedRewards: TournamentCurrencyReward[]; 
};
