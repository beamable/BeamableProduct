/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CronTrigger } from './CronTrigger';
import type { ExactTrigger } from './ExactTrigger';
import type { HttpCall } from './HttpCall';
import type { JobRetryPolicy } from './JobRetryPolicy';
import type { PublishMessage } from './PublishMessage';
import type { ServiceCall } from './ServiceCall';

export type JobDefinitionSaveRequest = { 
  id?: string | null; 
  isUnique?: boolean | null; 
  jobAction?: HttpCall | PublishMessage | ServiceCall; 
  name?: string; 
  retryPolicy?: JobRetryPolicy; 
  source?: string | null; 
  triggers?: (CronTrigger | ExactTrigger)[]; 
};
