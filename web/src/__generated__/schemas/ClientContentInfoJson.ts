import { ContentType } from './enums/ContentType';

export type ClientContentInfoJson = { 
  contentId: string; 
  tags: string[]; 
  type: ContentType; 
  uri: string; 
  version: string; 
  checksum?: string; 
};
