/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Account } from './Account';
import type { ListAuditResponse } from './ListAuditResponse';
import type { StatsResponse } from './StatsResponse';

export type AccountPersonallyIdentifiableInformationResponse = { 
  account: Account; 
  paymentAudits: ListAuditResponse; 
  stats: StatsResponse[]; 
};
