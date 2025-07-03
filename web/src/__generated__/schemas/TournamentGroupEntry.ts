import { TournamentCurrencyReward } from './TournamentCurrencyReward';

export type TournamentGroupEntry = { 
  currencyRewards: TournamentCurrencyReward[]; 
  groupId: bigint | string; 
  rank: bigint | string; 
  score: number; 
  stageChange: number; 
};
