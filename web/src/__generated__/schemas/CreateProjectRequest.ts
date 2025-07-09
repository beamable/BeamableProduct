export type CreateProjectRequest = { 
  name: string; 
  parent?: string; 
  plan?: string; 
  sharded?: boolean; 
};
