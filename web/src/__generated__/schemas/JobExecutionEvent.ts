import { JobDefinition } from './JobDefinition';
import { JobRetryPolicy } from './JobRetryPolicy';

export type JobExecutionEvent = { 
  executionId?: string | null; 
  executionKey?: string; 
  executionTime?: Date; 
  jobDefinition?: JobDefinition; 
  jobId?: string; 
  retryCount?: number; 
  retryPolicy?: JobRetryPolicy; 
};
