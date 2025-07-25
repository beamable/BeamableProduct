/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ClientPermission } from './ClientPermission';
import type { Event } from './Event';
import type { EventPhaseRuntime } from './EventPhaseRuntime';
import type { EventPhaseTime } from './EventPhaseTime';
import type { InFlightMessage } from './InFlightMessage';
import type { EventState } from './enums/EventState';

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
