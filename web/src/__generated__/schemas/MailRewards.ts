import { CurrencyChange } from './CurrencyChange';
import { ItemCreateRequest } from './ItemCreateRequest';

export type MailRewards = { 
  currencies: CurrencyChange[]; 
  items: ItemCreateRequest[]; 
  applyVipBonus?: boolean; 
};
