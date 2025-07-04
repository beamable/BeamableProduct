import { CurrencyChange } from './CurrencyChange';
import { EntitlementGenerator } from './EntitlementGenerator';
import { ItemCreateRequest } from './ItemCreateRequest';
import { PaymentDetailsEntryViewModel } from './PaymentDetailsEntryViewModel';
import { PaymentHistoryEntryViewModel } from './PaymentHistoryEntryViewModel';

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
