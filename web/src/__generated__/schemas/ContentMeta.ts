import { ContentVisibility } from './enums/ContentVisibility';

export type ContentMeta = { 
  visibility: ContentVisibility; 
  $link?: string; 
  $links?: string[]; 
  data?: string; 
  text?: string; 
};
