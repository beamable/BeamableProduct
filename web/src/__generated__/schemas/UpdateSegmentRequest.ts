/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Rule } from './Rule';

export type UpdateSegmentRequest = { 
  description?: string | null; 
  displayName?: string | null; 
  expectedActiveId?: string | null; 
  managedBy?: string | null; 
  propertyExtremes?: string[] | null; 
  propertyStats?: string[] | null; 
  realms?: string[] | null; 
  removeRule?: boolean; 
  rule?: Rule; 
  tags?: string[] | null; 
  trackTenure?: boolean | null; 
};
