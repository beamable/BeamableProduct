/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { LocalDate } from './LocalDate';
import type { SessionBasicPaymentTotal } from './SessionBasicPaymentTotal';

export type SessionBasicSessionHistoryResponse = { 
  date: LocalDate; 
  daysPlayed: number; 
  payments: string[]; 
  sessions: string[]; 
  totalPaid: SessionBasicPaymentTotal[]; 
  installDate?: string; 
};
