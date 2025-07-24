/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CommerceLootRoll } from './CommerceLootRoll';
import type { CurrencyChange } from './CurrencyChange';
import type { ItemCreateRequest } from './ItemCreateRequest';

export type OfferDefinition = { 
  descriptions: string[]; 
  images: string[]; 
  obtain: string[]; 
  symbol: string; 
  titles: string[]; 
  lootRoll?: CommerceLootRoll; 
  metadata?: string; 
  obtainCurrency?: CurrencyChange[]; 
  obtainItems?: ItemCreateRequest[]; 
};
