export type PlayerOnlineStatusResponse = { 
  lastSeen: bigint | string; 
  online: boolean; 
  playerId: bigint | string; 
};
