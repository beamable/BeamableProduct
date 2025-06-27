import { CurrencyProperty } from './CurrencyProperty';
import { FederationInfo } from './FederationInfo';

export type Currency = { 
  amount: bigint | string; 
  id: string; 
  properties?: CurrencyProperty[]; 
  proxy?: FederationInfo; 
  updatedAt?: bigint | string; 
};
