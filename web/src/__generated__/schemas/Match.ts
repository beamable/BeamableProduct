import { MatchType } from './MatchType';
import { Team } from './Team';
import { Ticket } from './Ticket';

export type Match = { 
  created?: Date | null; 
  matchId?: string | null; 
  matchType?: MatchType; 
  status?: string | null; 
  teams?: Team[] | null; 
  tickets?: Ticket[] | null; 
};
