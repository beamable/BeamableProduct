import { ServiceReference } from './ServiceReference';
import { ServiceStorageReference } from './ServiceStorageReference';

export type ManifestView = { 
  checksum: string; 
  created: bigint | string; 
  id: string; 
  manifest: ServiceReference[]; 
  comments?: string; 
  createdByAccountId?: bigint | string; 
  storageReference?: ServiceStorageReference[]; 
};
