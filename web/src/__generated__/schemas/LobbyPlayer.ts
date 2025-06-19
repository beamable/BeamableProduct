import { Tag } from './Tag';

export type LobbyPlayer = { 
  joined?: Date | null; 
  playerId?: string | null; 
  tags?: Tag[] | null; 
};
