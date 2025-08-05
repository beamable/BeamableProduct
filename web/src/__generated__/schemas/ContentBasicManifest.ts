/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BinaryReference } from './BinaryReference';
import type { ContentReference } from './ContentReference';
import type { TextReference } from './TextReference';

export type ContentBasicManifest = { 
  checksum: string; 
  created: bigint | string; 
  id: string; 
  references: (ContentReference | TextReference | BinaryReference)[]; 
  archived?: boolean; 
  diffObjectKey?: string; 
  lastChanged?: bigint | string; 
  publisherAccountId?: bigint | string; 
  uid?: string; 
};
