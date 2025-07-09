import { LocalDate } from './LocalDate';
import { PaymentTotal } from './PaymentTotal';

export type SessionHistoryResponse = { 
  date: LocalDate; 
  daysPlayed: number; 
  payments: string[]; 
  sessions: string[]; 
  totalPaid: PaymentTotal[]; 
  installDate?: string; 
};
