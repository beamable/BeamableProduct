export type Project = { 
  archived: boolean; 
  customCharts: Record<string, string>; 
  name: string; 
  plan: string; 
  root: boolean; 
  secret: string; 
  children?: string[]; 
  config?: Record<string, string>; 
  created?: bigint | string; 
  displayName?: string; 
  parent?: string; 
  sharded?: boolean; 
  sigval?: boolean; 
  status?: string; 
};
