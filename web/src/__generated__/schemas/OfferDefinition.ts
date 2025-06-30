import { CommerceLootRoll } from './CommerceLootRoll';
import { CurrencyChange } from './CurrencyChange';
import { ItemCreateRequest } from './ItemCreateRequest';

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
