import { RewardCalendarDay } from './RewardCalendarDay';

export type CalendarView = { 
  days: RewardCalendarDay[]; 
  id: string; 
  nextClaimSeconds: bigint | string; 
  nextIndex: number; 
  remainingSeconds: bigint | string; 
};
