import { PromotionScope } from './PromotionScope';

export type PromoteRealmResponse = { 
  scopes: PromotionScope[]; 
  sourcePid: string; 
};
