/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Rule } from './Rule';
import type { SegmentPropertyInput } from './SegmentPropertyInput';

export type UpdateSegmentRequest = { 
  description?: string | null; 
  displayName?: string | null; 
  expectedActiveId?: string | null; 
  properties?: Record<string, SegmentPropertyInput> | null; 
  rule?: Rule; 
};
