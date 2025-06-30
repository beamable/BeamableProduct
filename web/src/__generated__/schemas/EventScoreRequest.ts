export type EventScoreRequest = { 
  eventId: string; 
  score: number; 
  increment?: boolean; 
  stats?: Record<string, string>; 
};
