/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { OfferReason } from './OfferReason';
import type { OfferGrantState } from './enums/OfferGrantState';

export type RedeemOfferResult = { 
  grantId: string; 
  message?: string | null; 
  reason?: OfferReason; 
  state?: OfferGrantState; 
  status?: string | null; 
  success?: boolean; 
};
