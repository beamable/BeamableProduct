import { UploadRequestFromPortal } from './UploadRequestFromPortal';

export type UploadRequestsFromPortal = { 
  request: UploadRequestFromPortal[]; 
  playerId?: bigint | string; 
};
