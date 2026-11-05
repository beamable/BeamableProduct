/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { OfferItem } from './OfferItem';
import type { OfferReason } from './OfferReason';
import type { OfferGrantState } from './enums/OfferGrantState';

export type PlayerOffer = { 
  federationId: string; 
  grantId: string; 
  offerId: string; 
  state: OfferGrantState; 
  available?: boolean; 
  expiresAtUnixSeconds?: bigint | string; 
  grantedAtUnixSeconds?: bigint | string; 
  offer?: OfferItem; 
  unavailableReasons?: OfferReason[]; 
};
