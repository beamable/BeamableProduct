/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { OfferAmount } from './OfferAmount';

export type OfferItem = { 
  offerId: string; 
  cost?: OfferAmount[]; 
  description?: string | null; 
  imageUrl?: string | null; 
  priceLabel?: string | null; 
  properties?: Record<string, string>; 
  rewards?: OfferAmount[]; 
  tags?: string[]; 
  title?: string | null; 
};
