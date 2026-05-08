/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TicketDiagnostics } from './TicketDiagnostics';
import type { TicketJourney } from './TicketJourney';
import type { TicketPriority } from './enums/TicketPriority';
import type { TicketStatus } from './enums/TicketStatus';

export type PlayerSupportTicketView = { 
  createdAt?: Date; 
  description?: string; 
  diagnostics?: TicketDiagnostics; 
  id?: string; 
  journey?: TicketJourney; 
  playerId?: string; 
  playerName?: string | null; 
  priority?: TicketPriority; 
  realmId?: string; 
  status?: TicketStatus; 
  title?: string; 
  updatedAt?: Date; 
};
