export type LeaderboardAddRequest = { 
  id: bigint | string; 
  score: number; 
  increment?: boolean; 
  maxScore?: number; 
  minScore?: number; 
  stats?: Record<string, string>; 
};
