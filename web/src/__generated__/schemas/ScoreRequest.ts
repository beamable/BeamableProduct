export type ScoreRequest = { 
  playerId: bigint | string; 
  score: number; 
  tournamentId: string; 
  contentId?: string; 
  increment?: boolean; 
  stats?: Record<string, string>; 
};
