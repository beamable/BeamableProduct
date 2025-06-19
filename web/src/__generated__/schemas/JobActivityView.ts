import { JobState } from './enums/JobState';

export type JobActivityView = { 
  id?: string; 
  jobId?: string; 
  jobName?: string; 
  message?: string | null; 
  state?: JobState; 
  timestamp?: Date; 
};
