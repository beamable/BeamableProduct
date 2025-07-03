import { PresenceStatus } from './enums/PresenceStatus';

export type SetPresenceStatusRequest = { 
  description?: string | null; 
  status?: PresenceStatus; 
};
