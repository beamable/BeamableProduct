/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Tag } from './Tag';
import type { LobbyRestriction } from './enums/LobbyRestriction';

export type CreateLobby = { 
  data?: Record<string, string | null> | null; 
  description?: string | null; 
  matchType?: string | null; 
  maxPlayers?: number; 
  name?: string | null; 
  passcodeLength?: number; 
  playerTags?: Tag[] | null; 
  restriction?: LobbyRestriction; 
};
