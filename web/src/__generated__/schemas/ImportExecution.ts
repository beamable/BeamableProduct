/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ImportExecutionRequest } from './ImportExecutionRequest';
import type { ImportExecutionStats } from './ImportExecutionStats';

export type ImportExecution = { 
  by: string; 
  kind: string; 
  purpose: string; 
  ref: string; 
  request: ImportExecutionRequest; 
  error?: string | null; 
  finishedAt?: Date | null; 
  state?: string; 
  stats?: ImportExecutionStats; 
  submittedAt?: Date; 
};
