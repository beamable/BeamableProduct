import { Tag } from './Tag';
import { LobbyRestriction } from './enums/LobbyRestriction';

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
