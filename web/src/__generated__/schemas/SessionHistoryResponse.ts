/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { LocalDate } from './LocalDate';
import type { PaymentTotal } from './PaymentTotal';

export type SessionHistoryResponse = { 
  date: LocalDate; 
  daysPlayed: number; 
  payments: string[]; 
  sessions: string[]; 
  totalPaid: PaymentTotal[]; 
  installDate?: string; 
};
