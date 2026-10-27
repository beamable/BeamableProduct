/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { OfferAmount } from './OfferAmount';
import type { OfferReason } from './OfferReason';
import type { OfferText } from './OfferText';

export type OfferItem = { 
  offerId: string; 
  available?: boolean; 
  cost?: OfferAmount[]; 
  description?: string | null; 
  imageUrl?: string | null; 
  localizations?: Record<string, OfferText>; 
  priceLabel?: string | null; 
  properties?: Record<string, string>; 
  rewards?: OfferAmount[]; 
  tags?: string[]; 
  title?: string | null; 
  unavailableReasons?: OfferReason[]; 
};
