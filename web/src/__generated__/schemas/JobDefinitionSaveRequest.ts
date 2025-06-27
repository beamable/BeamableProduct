import { CronTrigger } from './CronTrigger';
import { ExactTrigger } from './ExactTrigger';
import { HttpCall } from './HttpCall';
import { JobRetryPolicy } from './JobRetryPolicy';
import { PublishMessage } from './PublishMessage';
import { ServiceCall } from './ServiceCall';

export type JobDefinitionSaveRequest = { 
  id?: string | null; 
  isUnique?: boolean | null; 
  jobAction?: HttpCall | PublishMessage | ServiceCall; 
  name?: string; 
  retryPolicy?: JobRetryPolicy; 
  source?: string | null; 
  triggers?: (CronTrigger | ExactTrigger)[]; 
};
