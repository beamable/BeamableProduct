import { ContentType } from './enums/ContentType';

export type ClientContentInfo = { 
  contentId: string; 
  tags: string[]; 
  type: ContentType; 
  uri: string; 
  version: string; 
  checksum?: string; 
};
