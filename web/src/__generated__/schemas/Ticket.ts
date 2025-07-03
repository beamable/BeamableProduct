import { Tag } from './Tag';

export type Ticket = { 
  created?: Date | null; 
  expires?: Date | null; 
  lobbyId?: string | null; 
  matchId?: string | null; 
  matchType?: string | null; 
  numberProperties?: Record<string, number> | null; 
  partyId?: string | null; 
  players?: string[] | null; 
  priority?: number; 
  status?: string | null; 
  stringProperties?: Record<string, string | null> | null; 
  tags?: Tag[] | null; 
  team?: string | null; 
  ticketId?: string | null; 
  watchOnlineStatus?: boolean; 
};
