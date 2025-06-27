export type ProjectView = { 
  pid: string; 
  projectName: string; 
  archived?: boolean; 
  children?: string[]; 
  cid?: bigint | string; 
  parent?: string; 
  secret?: string; 
  sharded?: boolean; 
};
