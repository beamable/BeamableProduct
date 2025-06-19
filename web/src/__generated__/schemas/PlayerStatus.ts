import { TournamentCurrencyReward } from './TournamentCurrencyReward';

export type PlayerStatus = { 
  contentId: string; 
  lastUpdateCycle: number; 
  playerId: bigint | string; 
  stage: number; 
  tier: number; 
  tournamentId: string; 
  unclaimedRewards: TournamentCurrencyReward[]; 
  groupId?: bigint | string; 
};
