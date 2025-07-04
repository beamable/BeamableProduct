import { Account } from './Account';
import { ListAuditResponse } from './ListAuditResponse';
import { StatsResponse } from './StatsResponse';

export type AccountPersonallyIdentifiableInformationResponse = { 
  account: Account; 
  paymentAudits: ListAuditResponse; 
  stats: StatsResponse[]; 
};
