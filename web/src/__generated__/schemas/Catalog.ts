import { OfferDefinition } from './OfferDefinition';
import { Store } from './Store';

export type Catalog = { 
  created: bigint | string; 
  offerDefinitions: OfferDefinition[]; 
  stores: Store[]; 
  version: bigint | string; 
};
