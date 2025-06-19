import { TournamentEntry } from './TournamentEntry';

export type GetStandingsResponse = { 
  entries: TournamentEntry[]; 
  me?: TournamentEntry; 
};
