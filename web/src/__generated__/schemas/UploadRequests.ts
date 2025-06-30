import { UploadRequest } from './UploadRequest';

export type UploadRequests = { 
  request: UploadRequest[]; 
  playerId?: bigint | string; 
};
