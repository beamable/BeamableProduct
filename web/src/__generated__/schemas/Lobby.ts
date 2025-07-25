/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { LobbyPlayer } from './LobbyPlayer';
import type { MatchType } from './MatchType';
import type { LobbyRestriction } from './enums/LobbyRestriction';

export type Lobby = { 
  created?: Date | null; 
  data?: Record<string, string | null> | null; 
  description?: string | null; 
  host?: string | null; 
  lobbyId?: string | null; 
  matchType?: MatchType; 
  maxPlayers?: number; 
  name?: string | null; 
  passcode?: string | null; 
  players?: LobbyPlayer[] | null; 
  restriction?: LobbyRestriction; 
};
