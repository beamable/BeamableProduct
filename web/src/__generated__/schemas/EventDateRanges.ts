import { DateRange } from './DateRange';

export type EventDateRanges = { 
  dates: DateRange[]; 
  id: string; 
  name: string; 
  state: string; 
  createdAt?: bigint | string; 
};
