/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { VirtualStatNodeView } from './VirtualStatNodeView';
import type { VirtualStatReferenceView } from './VirtualStatReferenceView';
import type { VirtualStatValidationError } from './VirtualStatValidationError';
import type { VirtualStatType } from './enums/VirtualStatType';

export type ValidateVirtualStatResponse = { 
  error?: VirtualStatValidationError; 
  nodes?: VirtualStatNodeView[] | null; 
  references?: VirtualStatReferenceView[] | null; 
  resultType?: VirtualStatType; 
};
