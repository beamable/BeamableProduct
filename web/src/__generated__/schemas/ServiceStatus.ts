import { ServiceDependencyReference } from './ServiceDependencyReference';

export type ServiceStatus = { 
  imageId: string; 
  isCurrent: boolean; 
  running: boolean; 
  serviceName: string; 
  serviceDependencyReferences?: ServiceDependencyReference[]; 
};
