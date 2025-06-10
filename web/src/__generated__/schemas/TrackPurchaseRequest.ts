import { CurrencyChange } from './CurrencyChange';
import { ItemCreateRequest } from './ItemCreateRequest';

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
