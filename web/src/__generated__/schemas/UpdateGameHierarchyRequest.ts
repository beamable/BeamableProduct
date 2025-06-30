import { ProjectView } from './ProjectView';

export type UpdateGameHierarchyRequest = { 
  projects: ProjectView[]; 
  rootPID: string; 
};
