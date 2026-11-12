/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

export type GrantOfferRequest = { 
  federationId: string; 
  idempotencyKey: string; 
  offerId: string; 
  playerId: bigint | string; 
  expiresAtUnixSeconds?: bigint | string | null; 
  metadata?: Record<string, string> | null; 
  originId?: string | null; 
};
