export type LeaderboardPartitionInfo = { 
  isEmpty: boolean; 
  leaderboardId: string; 
  playerId: bigint | string; 
  partition?: number; 
};
