import { ContentMeta } from './ContentMeta';

export type ContentDefinition = { 
  checksum: string; 
  id: string; 
  properties: Record<string, ContentMeta>; 
  tags?: string[]; 
  variants?: Record<string, ContentMeta>[]; 
};
