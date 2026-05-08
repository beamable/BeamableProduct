/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TicketComment } from './TicketComment';
import type { TicketDiagnostics } from './TicketDiagnostics';
import type { TicketEvent } from './TicketEvent';
import type { TicketJourney } from './TicketJourney';
import type { TicketPriority } from './enums/TicketPriority';
import type { TicketStatus } from './enums/TicketStatus';

export type SupportTicket = { 
  assigneeEmail?: string | null; 
  assigneeId?: string | null; 
  comments?: TicketComment[]; 
  createdAt?: Date; 
  description?: string; 
  diagnostics?: TicketDiagnostics; 
  history?: TicketEvent[]; 
  id?: string; 
  journey?: TicketJourney; 
  metadata?: Record<string, any | null>; 
  playerId?: string; 
  playerName?: string | null; 
  priority?: TicketPriority; 
  realmId?: string; 
  status?: TicketStatus; 
  tags?: string[]; 
  title?: string; 
  updatedAt?: Date; 
};
