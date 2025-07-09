export type LeaderboardApiViewRequest = { 
  focus?: bigint | string; 
  friends?: boolean; 
  from?: number; 
  guild?: boolean; 
  max?: number; 
  outlier?: bigint | string; 
};
