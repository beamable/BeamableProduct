export type ReferenceSuperset = { 
  id: string; 
  type: string; 
  uri: string; 
  version: string; 
  checksum?: string; 
  tags?: string[]; 
  visibility?: string; 
};
