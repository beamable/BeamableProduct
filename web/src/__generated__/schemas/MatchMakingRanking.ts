export type MatchMakingRanking = { 
  gt: bigint | string; 
  isUnranked: boolean; 
  rank: number; 
  variables: Record<string, string>; 
};
