/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ContentVisibility } from './enums/ContentVisibility';

export type ContentReference = { 
  id: string; 
  tag: string; 
  tags: string[]; 
  type: "content"; 
  uri: string; 
  version: string; 
  visibility: ContentVisibility; 
  checksum?: string; 
  created?: bigint | string; 
  lastChanged?: bigint | string; 
};
