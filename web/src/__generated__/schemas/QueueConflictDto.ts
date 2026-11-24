/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CampaignLifecycle } from './enums/CampaignLifecycle';

export type QueueConflictDto = { 
  blocking?: boolean; 
  campaignId?: string; 
  experiment?: boolean; 
  from?: Date; 
  name?: string; 
  phase?: CampaignLifecycle; 
  to?: Date; 
  version?: number; 
};
