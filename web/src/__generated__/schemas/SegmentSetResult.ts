/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ProtoError } from './ProtoError';
import type { SegmentPropertyTotal } from './SegmentPropertyTotal';

export type SegmentSetResult = { 
  error?: ProtoError; 
  members?: bigint | string | null; 
  realmMembers?: Record<string, bigint | string> | null; 
  sample?: Record<string, (bigint | string)[]> | null; 
  values?: SegmentPropertyTotal[] | null; 
};
