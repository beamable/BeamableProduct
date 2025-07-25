/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { JobDefinition } from './JobDefinition';
import type { JobRetryPolicy } from './JobRetryPolicy';

export type JobExecutionEvent = { 
  executionId?: string | null; 
  executionKey?: string; 
  executionTime?: Date; 
  jobDefinition?: JobDefinition; 
  jobId?: string; 
  retryCount?: number; 
  retryPolicy?: JobRetryPolicy; 
};
