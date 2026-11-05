/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ProtoError } from './ProtoError';
import type { SegmentRealmPreview } from './SegmentRealmPreview';

export type SegmentPreviewResult = { 
  error?: ProtoError; 
  exact?: boolean | null; 
  members?: bigint | string | null; 
  realms?: Record<string, SegmentRealmPreview> | null; 
};
