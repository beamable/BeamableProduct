export type TextReference = { 
  id: string; 
  tags: string[]; 
  type: "text"; 
  uri: string; 
  version: string; 
  visibility: string; 
  checksum?: string; 
  created?: bigint | string; 
  lastChanged?: bigint | string; 
};
