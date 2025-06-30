import { CurrencyChange } from './CurrencyChange';
import { ItemCreateRequest } from './ItemCreateRequest';
import { Price } from './Price';

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
