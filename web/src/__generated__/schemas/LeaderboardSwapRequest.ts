export type LeaderboardSwapRequest = { 
  delta: bigint | string; 
  swapBase: bigint | string; 
  loserId?: bigint | string; 
  winnerId?: bigint | string; 
};
