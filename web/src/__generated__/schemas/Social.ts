/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Friend } from './Friend';
import type { Invite } from './Invite';
import type { Player } from './Player';

export type Social = { 
  blocked: Player[]; 
  friends: Friend[]; 
  invites: Invite[]; 
  playerId: string; 
};
