import { CronTrigger } from './CronTrigger';
import { ExactTrigger } from './ExactTrigger';
import { HttpCall } from './HttpCall';
import { JobAnalytics } from './JobAnalytics';
import { JobRetryPolicy } from './JobRetryPolicy';
import { PublishMessage } from './PublishMessage';
import { ServiceCall } from './ServiceCall';

export type JobDefinitionView = { 
  analytics?: JobAnalytics; 
  id?: string; 
  isUnique?: boolean; 
  jobAction?: HttpCall | PublishMessage | ServiceCall; 
  lastUpdate?: Date; 
  name?: string; 
  owner?: string; 
  retryPolicy?: JobRetryPolicy; 
  source?: string | null; 
  suspendedAt?: Date | null; 
  triggers?: (CronTrigger | ExactTrigger)[]; 
};
