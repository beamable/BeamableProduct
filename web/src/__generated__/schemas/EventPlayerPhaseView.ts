import { EventRule } from './EventRule';

export type EventPlayerPhaseView = { 
  durationSeconds: bigint | string; 
  name: string; 
  rules: EventRule[]; 
};
