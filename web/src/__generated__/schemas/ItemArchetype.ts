import { ClientPermission } from './ClientPermission';
import { FederationInfo } from './FederationInfo';

export type ItemArchetype = { 
  symbol: string; 
  clientPermission?: ClientPermission; 
  external?: FederationInfo; 
};
