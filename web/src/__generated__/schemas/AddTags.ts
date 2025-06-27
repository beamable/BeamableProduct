import { Tag } from './Tag';

export type AddTags = { 
  playerId?: string | null; 
  replace?: boolean; 
  tags?: Tag[] | null; 
};
