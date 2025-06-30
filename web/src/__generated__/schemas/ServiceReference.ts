import { ServiceComponent } from './ServiceComponent';
import { ServiceDependencyReference } from './ServiceDependencyReference';

export type ServiceReference = { 
  archived: boolean; 
  arm: boolean; 
  checksum: string; 
  enabled: boolean; 
  imageId: string; 
  serviceName: string; 
  templateId: string; 
  comments?: string; 
  components?: ServiceComponent[]; 
  containerHealthCheckPort?: bigint | string; 
  dependencies?: ServiceDependencyReference[]; 
  imageCpuArch?: string; 
};
