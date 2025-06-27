import { Tag } from './Tag';

export type TicketReservationRequest = { 
  matchTypes?: string[] | null; 
  maxWaitDurationSecs?: number; 
  players?: string[] | null; 
  tags?: Tag[] | null; 
  team?: string | null; 
  watchOnlineStatus?: boolean; 
};
