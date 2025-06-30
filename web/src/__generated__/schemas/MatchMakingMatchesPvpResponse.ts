import { MatchMakingRanking } from './MatchMakingRanking';
import { MatchMakingWindowResp } from './MatchMakingWindowResp';

export type MatchMakingMatchesPvpResponse = { 
  playerRank: MatchMakingRanking; 
  result: string; 
  totalEntries: number; 
  windows: MatchMakingWindowResp[]; 
};
