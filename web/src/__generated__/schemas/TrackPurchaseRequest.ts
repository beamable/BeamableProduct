/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CurrencyChange } from './CurrencyChange';
import type { ItemCreateRequest } from './ItemCreateRequest';

export type TrackPurchaseRequest = { 
  isoCurrencySymbol: string; 
  obtainCurrency: CurrencyChange[]; 
  obtainItems: ItemCreateRequest[]; 
  priceInLocalCurrency: number; 
  purchaseId: string; 
  skuName: string; 
  skuProductId: string; 
  store: string; 
};
