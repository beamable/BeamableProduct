import { ServiceReference } from './ServiceReference';
import { ServiceStorageReference } from './ServiceStorageReference';

export type PostManifestRequest = { 
  manifest: ServiceReference[]; 
  autoDeploy?: boolean; 
  comments?: string; 
  storageReferences?: ServiceStorageReference[]; 
};
