/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { PropertyAggregate } from './enums/PropertyAggregate';
import type { PropertyKind } from './enums/PropertyKind';

export type SegmentPropertyResponse = { 
  kind: PropertyKind; 
  aggregate?: PropertyAggregate; 
  attribute?: string | null; 
  computedAt?: Date | null; 
  memberCount?: bigint | string | null; 
  value?: any | null; 
};
