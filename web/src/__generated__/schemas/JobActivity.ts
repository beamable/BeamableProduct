import { JobState } from './enums/JobState';

export type JobActivity = { 
  executionId?: string | null; 
  id?: string; 
  jobId?: string; 
  jobName?: string; 
  message?: string | null; 
  owner?: string; 
  state?: JobState; 
  timestamp?: Date; 
};
