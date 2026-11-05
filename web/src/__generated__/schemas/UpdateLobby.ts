/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { UpdateData } from './UpdateData';
import type { LobbyRestriction } from './enums/LobbyRestriction';

export type UpdateLobby = { 
  data?: UpdateData; 
  description?: string | null; 
  matchType?: string | null; 
  maxPlayers?: number | null; 
  name?: string | null; 
  newHost?: string | null; 
  restriction?: LobbyRestriction; 
};
