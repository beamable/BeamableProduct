/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Rule } from './Rule';
import type { SegmentCount } from './SegmentCount';
import type { SegmentIconView } from './SegmentIconView';
import type { SegmentOverrideCounts } from './SegmentOverrideCounts';
import type { SegmentSync } from './SegmentSync';
import type { SegmentState } from './enums/SegmentState';

export type SegmentResponse = { 
  createdAt: Date; 
  description: string; 
  displayName: string; 
  propertyExtremes: string[]; 
  propertyStats: string[]; 
  references: string[]; 
  segmentId: string; 
  state: SegmentState; 
  tags: string[]; 
  trackTenure: boolean; 
  updatedAt: Date; 
  version: number; 
  watchedKeys: string[]; 
  zoneId: string; 
  counts?: SegmentCount; 
  createdBy?: string | null; 
  dependents?: string[] | null; 
  icon?: SegmentIconView; 
  managedBy?: string | null; 
  overrides?: Record<string, SegmentOverrideCounts> | null; 
  realms?: string[] | null; 
  rule?: Rule; 
  ruleText?: string | null; 
  sync?: SegmentSync; 
  updatedBy?: string | null; 
};
