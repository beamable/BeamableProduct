/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CurrencyProperty } from './CurrencyProperty';
import type { ItemCreateRequest } from './ItemCreateRequest';
import type { ItemDeleteRequest } from './ItemDeleteRequest';
import type { ItemUpdateRequest } from './ItemUpdateRequest';

export type InventoryUpdateRequest = { 
  applyVipBonus?: boolean; 
  currencies?: Record<string, bigint | string>; 
  currencyProperties?: Record<string, CurrencyProperty[]>; 
  deleteItems?: ItemDeleteRequest[]; 
  newItems?: ItemCreateRequest[]; 
  transaction?: string; 
  updateItems?: ItemUpdateRequest[]; 
};
