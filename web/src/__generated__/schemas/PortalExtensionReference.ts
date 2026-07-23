/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BundleOrigin } from './BundleOrigin';
import type { ExtensionContentReference } from './ExtensionContentReference';

export type PortalExtensionReference = { 
  archived?: boolean; 
  attributes?: Record<string, string>; 
  checksum?: string; 
  dependencies?: string[] | null; 
  enabled?: boolean; 
  files?: ExtensionContentReference[]; 
  name?: string; 
  origin?: BundleOrigin; 
};
