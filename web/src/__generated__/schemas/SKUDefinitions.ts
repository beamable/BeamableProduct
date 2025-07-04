import { SKU } from './SKU';

export type SKUDefinitions = { 
  created: bigint | string; 
  definitions: SKU[]; 
  version: bigint | string; 
};
