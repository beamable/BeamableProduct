/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CurrencyChange } from './CurrencyChange';
import type { ItemCreateRequest } from './ItemCreateRequest';
import type { Price } from './Price';

export type PlayerOfferView = { 
  coupons: number; 
  descriptions: string[]; 
  images: string[]; 
  obtain: string[]; 
  obtainCurrency: CurrencyChange[]; 
  obtainItems: ItemCreateRequest[]; 
  price: Price; 
  symbol: string; 
  titles: string[]; 
  buttonText?: string; 
};
