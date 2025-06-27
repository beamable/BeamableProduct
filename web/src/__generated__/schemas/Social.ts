import { Friend } from './Friend';
import { Invite } from './Invite';
import { Player } from './Player';

export type Social = { 
  blocked: Player[]; 
  friends: Friend[]; 
  invites: Invite[]; 
  playerId: string; 
};
