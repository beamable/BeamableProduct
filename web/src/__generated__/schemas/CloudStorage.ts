import { CloudDataStatus } from './CloudDataStatus';

export type CloudStorage = { 
  ejected: boolean; 
  sent: boolean; 
  sid: bigint | string; 
  status: CloudDataStatus; 
  stype: number; 
  added?: bigint | string; 
  data?: string; 
  expiration?: bigint | string; 
  gt?: bigint | string; 
  jobId?: string; 
  reference?: string; 
  retrieved?: bigint | string; 
  uniqueIdentifier?: string; 
  updated?: bigint | string; 
  version?: bigint | string; 
};
