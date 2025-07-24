/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CronTrigger } from './CronTrigger';
import type { ExactTrigger } from './ExactTrigger';
import type { HttpCall } from './HttpCall';
import type { JobAnalytics } from './JobAnalytics';
import type { JobRetryPolicy } from './JobRetryPolicy';
import type { PublishMessage } from './PublishMessage';
import type { ServiceCall } from './ServiceCall';

export type JobDefinition = { 
  analytics?: JobAnalytics; 
  id?: string; 
  isUnique?: boolean; 
  jobAction?: HttpCall | PublishMessage | ServiceCall; 
  lastUpdate?: Date; 
  name?: string; 
  nonce?: string | null; 
  owner?: string; 
  retryPolicy?: JobRetryPolicy; 
  source?: string | null; 
  suspendedAt?: Date | null; 
  triggers?: (CronTrigger | ExactTrigger)[]; 
};
