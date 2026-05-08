/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TicketDiagnostics } from './TicketDiagnostics';
import type { TicketJourney } from './TicketJourney';
import type { TicketPriority } from './enums/TicketPriority';
import type { TicketStatus } from './enums/TicketStatus';

export type CreateTicketRequest = { 
  assigneeEmail?: string | null; 
  assigneeId?: string | null; 
  description?: string; 
  diagnostics?: TicketDiagnostics; 
  journey?: TicketJourney; 
  metadata?: Record<string, any | null> | null; 
  playerId?: string; 
  playerName?: string | null; 
  priority?: TicketPriority; 
  status?: TicketStatus; 
  tags?: string[] | null; 
  title?: string; 
};
