import { FederationInfo } from './FederationInfo';
import { ItemProperty } from './ItemProperty';

export type Item = { 
  id: bigint | string; 
  properties: ItemProperty[]; 
  createdAt?: bigint | string; 
  proxy?: FederationInfo; 
  proxyId?: string; 
  updatedAt?: bigint | string; 
};
