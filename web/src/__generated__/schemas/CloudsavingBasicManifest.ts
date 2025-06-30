import { CloudsavingBasicReference } from './CloudsavingBasicReference';

export type CloudsavingBasicManifest = { 
  created: bigint | string; 
  id: string; 
  manifest: CloudsavingBasicReference[]; 
  replacement: boolean; 
};
