import { ReferenceSuperset } from './ReferenceSuperset';

export type SaveManifestRequest = { 
  id: string; 
  references: ReferenceSuperset[]; 
};
