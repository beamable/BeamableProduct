import { ServiceStatus } from './ServiceStatus';
import { ServiceStorageStatus } from './ServiceStorageStatus';

export type GetStatusResponse = { 
  isCurrent: boolean; 
  services: ServiceStatus[]; 
  storageStatuses?: ServiceStorageStatus[]; 
};
