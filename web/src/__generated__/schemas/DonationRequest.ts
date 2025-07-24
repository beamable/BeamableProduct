/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Currency } from './Currency';
import type { DonationEntry } from './DonationEntry';

export type DonationRequest = { 
  currency: Currency; 
  playerId: bigint | string; 
  progress: DonationEntry[]; 
  satisfied: boolean; 
  timeRequested: bigint | string; 
};
