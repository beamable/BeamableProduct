import { ItemProperty } from './ItemProperty';

export type ItemUpdateRequest = { 
  contentId: string; 
  id: bigint | string; 
  properties: ItemProperty[]; 
};
