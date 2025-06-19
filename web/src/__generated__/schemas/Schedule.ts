import { ScheduleDefinition } from './ScheduleDefinition';

export type Schedule = { 
  activeFrom: string; 
  activeTo?: string; 
  crons?: string[]; 
  definitions?: ScheduleDefinition[]; 
  description?: string; 
};
