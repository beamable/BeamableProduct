import { OfferConstraint } from './OfferConstraint';

export type OfferRequirement = { 
  offerSymbol: string; 
  purchases: OfferConstraint; 
};
