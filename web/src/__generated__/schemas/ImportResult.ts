/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ImportEventSummary } from './ImportEventSummary';
import type { ImportFileResult } from './ImportFileResult';
import type { ImportInvalidEvent } from './ImportInvalidEvent';

export type ImportResult = { 
  actTimeMax?: Date | null; 
  actTimeMin?: Date | null; 
  events?: ImportEventSummary[]; 
  eventsSkipped?: bigint | string | null; 
  eventsTruncated?: boolean; 
  eventsWritten?: bigint | string | null; 
  files?: ImportFileResult[]; 
  invalidEventsByScope?: ImportInvalidEvent[]; 
  params?: string[]; 
  rowsInvalidEvent?: bigint | string | null; 
  rowsUnmapped?: bigint | string | null; 
  rowsUnmatched?: bigint | string | null; 
  rowsWritten?: bigint | string | null; 
  sourceRows?: bigint | string | null; 
  status?: string | null; 
};
