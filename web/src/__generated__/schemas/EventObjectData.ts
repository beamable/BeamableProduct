import { ClientPermission } from './ClientPermission';
import { Event } from './Event';
import { EventPhaseRuntime } from './EventPhaseRuntime';
import { EventPhaseTime } from './EventPhaseTime';
import { InFlightMessage } from './InFlightMessage';
import { EventState } from './enums/EventState';

export type EventObjectData = { 
  content: Event; 
  done: boolean; 
  id: string; 
  leaderboardId: string; 
  running: boolean; 
  state: EventState; 
  createdAt?: bigint | string; 
  endTime?: bigint | string; 
  inFlight?: InFlightMessage[]; 
  lastChildEventId?: string; 
  origin?: string; 
  originType?: string; 
  permissions?: ClientPermission; 
  phase?: EventPhaseRuntime; 
  phaseTimes?: EventPhaseTime[]; 
  rootEventId?: string; 
  startTime?: bigint | string; 
};
