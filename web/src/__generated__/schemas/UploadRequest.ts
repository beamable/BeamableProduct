/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { MetadataPair } from './MetadataPair';

export type UploadRequest = { 
  objectKey: string; 
  sizeInBytes: bigint | string; 
  checksum?: string; 
  deleted?: boolean; 
  lastModified?: bigint | string; 
  metadata?: MetadataPair[]; 
};
