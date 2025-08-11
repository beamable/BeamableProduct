/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CurrencyChange } from './CurrencyChange';
import type { EntitlementGenerator } from './EntitlementGenerator';
import type { ItemCreateRequest } from './ItemCreateRequest';
import type { PaymentDetailsEntryViewModel } from './PaymentDetailsEntryViewModel';
import type { PaymentHistoryEntryViewModel } from './PaymentHistoryEntryViewModel';

export type PaymentAuditEntryViewModel = { 
  details: PaymentDetailsEntryViewModel; 
  entitlements: EntitlementGenerator[]; 
  gt: bigint | string; 
  history: PaymentHistoryEntryViewModel[]; 
  providerid: string; 
  providername: string; 
  txid: bigint | string; 
  txstate: string; 
  created?: bigint | string; 
  obtainCurrency?: CurrencyChange[]; 
  obtainItems?: ItemCreateRequest[]; 
  replayGuardValue?: string; 
  updated?: bigint | string; 
  version?: string; 
};
