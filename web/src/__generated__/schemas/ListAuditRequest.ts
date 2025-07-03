export type ListAuditRequest = { 
  limit?: number; 
  player?: bigint | string; 
  provider?: string; 
  providerid?: string; 
  start?: number; 
  state?: string; 
  txid?: bigint | string; 
};
