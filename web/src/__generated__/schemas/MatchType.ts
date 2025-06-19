import { TeamContentProto } from './TeamContentProto';

export type MatchType = { 
  federatedGameServerNamespace?: string | null; 
  id?: string | null; 
  matchingIntervalSecs?: number; 
  maxWaitDurationSecs?: number; 
  teams?: TeamContentProto[] | null; 
  waitAfterMinReachedSecs?: number; 
};
