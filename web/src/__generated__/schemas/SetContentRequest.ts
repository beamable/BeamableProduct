import { Event } from './Event';

export type SetContentRequest = { 
  event: Event; 
  origin: string; 
  originType?: string; 
  rootEventId?: string; 
};
