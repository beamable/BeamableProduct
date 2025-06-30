import { ProjectView } from './ProjectView';

export type CustomerView = { 
  cid: bigint | string; 
  name: string; 
  projects: ProjectView[]; 
  alias?: string; 
};
