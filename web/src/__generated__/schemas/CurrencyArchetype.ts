import { ClientPermission } from './ClientPermission';
import { FederationInfo } from './FederationInfo';

export type CurrencyArchetype = { 
  symbol: string; 
  clientPermission?: ClientPermission; 
  external?: FederationInfo; 
  startingAmount?: bigint | string; 
};
