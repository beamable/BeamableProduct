import { EventPlayerStateView } from './EventPlayerStateView';

export type EventPlayerView = { 
  done: EventPlayerStateView[]; 
  running: EventPlayerStateView[]; 
};
