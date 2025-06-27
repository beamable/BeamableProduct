import { CurrencyProperty } from './CurrencyProperty';
import { ItemCreateRequest } from './ItemCreateRequest';
import { ItemDeleteRequest } from './ItemDeleteRequest';
import { ItemUpdateRequest } from './ItemUpdateRequest';

export type InventoryUpdateRequest = { 
  applyVipBonus?: boolean; 
  currencies?: Record<string, bigint | string>; 
  currencyProperties?: Record<string, CurrencyProperty[]>; 
  deleteItems?: ItemDeleteRequest[]; 
  newItems?: ItemCreateRequest[]; 
  transaction?: string; 
  updateItems?: ItemUpdateRequest[]; 
};
