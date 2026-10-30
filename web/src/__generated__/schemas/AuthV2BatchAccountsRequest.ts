/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AuthV2BatchAccountsStats } from './AuthV2BatchAccountsStats';
import type { AuthV2StatsSearchRequest } from './AuthV2StatsSearchRequest';

export type AuthV2BatchAccountsRequest = { 
  playerIds: (bigint | string)[]; 
  filter?: AuthV2StatsSearchRequest; 
  includeAccount?: boolean; 
  stats?: AuthV2BatchAccountsStats[]; 
};
