import { LobbyPlayer } from './LobbyPlayer';
import { MatchType } from './MatchType';
import { LobbyRestriction } from './enums/LobbyRestriction';

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
