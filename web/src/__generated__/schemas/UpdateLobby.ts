import { UpdateData } from './UpdateData';
import { LobbyRestriction } from './enums/LobbyRestriction';

export type UpdateLobby = { 
  data?: UpdateData; 
  description?: string | null; 
  matchType?: string | null; 
  maxPlayers?: number | null; 
  name?: string | null; 
  newHost?: string | null; 
  restriction?: LobbyRestriction; 
};
