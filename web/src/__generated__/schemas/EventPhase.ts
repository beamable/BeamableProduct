import { EventRule } from './EventRule';

export type EventPhase = { 
  durationMillis: bigint | string; 
  durationSeconds: bigint | string; 
  duration_minutes: number; 
  name: string; 
  rules?: EventRule[]; 
};
