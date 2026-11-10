/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AuthV2StatsSearchCriteria } from './AuthV2StatsSearchCriteria';
import type { AuthV2StatsVisibility } from './enums/AuthV2StatsVisibility';

export type AuthV2StatsSearchRequest = { 
  domain: string; 
  itemType: string; 
  visibility: AuthV2StatsVisibility; 
  criteria?: AuthV2StatsSearchCriteria[]; 
  limit?: number; 
  offset?: number; 
};
