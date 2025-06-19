import { TournamentCurrencyReward } from './TournamentCurrencyReward';

export type TournamentEntry = { 
  currencyRewards: TournamentCurrencyReward[]; 
  playerId: bigint | string; 
  rank: bigint | string; 
  score: number; 
  stage: number; 
  stageChange: number; 
  tier: number; 
  nextStageChange?: number; 
  previousStageChange?: number; 
};
