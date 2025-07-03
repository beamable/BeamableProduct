import { InvitationDirection } from './enums/InvitationDirection';

export type Invite = { 
  direction: InvitationDirection; 
  playerId: string; 
};
