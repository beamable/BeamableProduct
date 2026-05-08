/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TicketEventType } from './enums/TicketEventType';

export type TicketEvent = { 
  actorId?: string | null; 
  actorLabel?: string; 
  details?: Record<string, any | null>; 
  id?: string; 
  ts?: Date; 
  type?: TicketEventType; 
};
