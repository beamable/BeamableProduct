export type AdminGetPlayerStatusRequest = { 
  playerId: bigint | string; 
  contentId?: string; 
  hasUnclaimedRewards?: boolean; 
  tournamentId?: string; 
};
