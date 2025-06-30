import { CurrencyProperty } from './CurrencyProperty';
import { FederationInfo } from './FederationInfo';

export type CurrencyView = { 
  amount: bigint | string; 
  id: string; 
  properties: CurrencyProperty[]; 
  proxy?: FederationInfo; 
};
