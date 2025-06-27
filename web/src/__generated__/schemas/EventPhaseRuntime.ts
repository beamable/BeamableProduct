import { EventRule } from './EventRule';

export type EventPhaseRuntime = { 
  endTime: bigint | string; 
  name: string; 
  rules: EventRule[]; 
  startTime: bigint | string; 
};
