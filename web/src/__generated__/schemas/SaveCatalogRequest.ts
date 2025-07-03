import { OfferDefinition } from './OfferDefinition';
import { Store } from './Store';

export type SaveCatalogRequest = { 
  offerDefinitions: OfferDefinition[]; 
  stores: Store[]; 
};
