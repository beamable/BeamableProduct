/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { MatchType } from './MatchType';
import type { Team } from './Team';
import type { Ticket } from './Ticket';

export type Match = { 
  created?: Date | null; 
  matchId?: string | null; 
  matchType?: MatchType; 
  status?: string | null; 
  teams?: Team[] | null; 
  tickets?: Ticket[] | null; 
};
