export type GetStandingsRequest = { 
  tournamentId: string; 
  contentId?: string; 
  cycle?: number; 
  focus?: bigint | string; 
  from?: number; 
  max?: number; 
};
