import { PresenceStatus } from './enums/PresenceStatus';

export type OnlineStatus = { 
  description?: string | null; 
  lastOnline?: Date | null; 
  online?: boolean; 
  playerId?: string | null; 
  status?: PresenceStatus; 
};
