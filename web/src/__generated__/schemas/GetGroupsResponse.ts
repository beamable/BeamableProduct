import { TournamentGroupEntry } from './TournamentGroupEntry';

export type GetGroupsResponse = { 
  entries: TournamentGroupEntry[]; 
  focus?: TournamentGroupEntry; 
};
