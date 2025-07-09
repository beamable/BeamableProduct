import { ItemProperty } from './ItemProperty';

export type ItemCreateRequest = { 
  contentId: string; 
  properties: ItemProperty[]; 
};
