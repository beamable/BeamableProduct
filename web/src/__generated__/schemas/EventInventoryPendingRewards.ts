import { ItemCreateRequest } from './ItemCreateRequest';

export type EventInventoryPendingRewards = { 
  empty: boolean; 
  currencies?: Record<string, string>; 
  items?: ItemCreateRequest[]; 
};
