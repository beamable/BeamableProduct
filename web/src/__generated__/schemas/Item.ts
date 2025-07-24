/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { FederationInfo } from './FederationInfo';
import type { ItemProperty } from './ItemProperty';

export type Item = { 
  id: bigint | string; 
  properties: ItemProperty[]; 
  createdAt?: bigint | string; 
  proxy?: FederationInfo; 
  proxyId?: string; 
  updatedAt?: bigint | string; 
};
