import { MatchMakingRanking } from './MatchMakingRanking';

export type MatchMakingWindowResp = { 
  difficulty: number; 
  matches: MatchMakingRanking[]; 
};
