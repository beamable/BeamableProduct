/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { FileSchema } from './FileSchema';

export type RegisterDatasetFileRequest = { 
  checksum: string; 
  fileName: string; 
  sizeBytes: bigint | string; 
  declaredSchema?: FileSchema; 
  label?: string | null; 
  partSizeBytes?: bigint | string; 
};
