/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AccountsBasicAccount } from './AccountsBasicAccount';
import type { ListAuditResponse } from './ListAuditResponse';
import type { StatsResponse } from './StatsResponse';

export type AccountPersonallyIdentifiableInformationResponse = { 
  account: AccountsBasicAccount; 
  paymentAudits: ListAuditResponse; 
  stats: StatsResponse[]; 
};
