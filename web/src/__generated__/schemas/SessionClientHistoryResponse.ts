import { LocalDate } from './LocalDate';

export type SessionClientHistoryResponse = { 
  date: LocalDate; 
  daysPlayed: number; 
  sessions: string[]; 
  installDate?: string; 
};
