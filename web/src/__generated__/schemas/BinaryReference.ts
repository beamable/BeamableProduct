export type BinaryReference = { 
  id: string; 
  tags: string[]; 
  type: "binary"; 
  uploadMethod: string; 
  uploadUri: string; 
  uri: string; 
  version: string; 
  visibility: string; 
  checksum?: string; 
  created?: string; 
  lastChanged?: string; 
};
