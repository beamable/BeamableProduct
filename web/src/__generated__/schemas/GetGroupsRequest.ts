export type GetGroupsRequest = { 
  tournamentId: string; 
  contentId?: string; 
  cycle?: number; 
  focus?: bigint | string; 
  from?: number; 
  max?: number; 
};
