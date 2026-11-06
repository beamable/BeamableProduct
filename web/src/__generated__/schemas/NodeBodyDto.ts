/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AnalyticsGateBodyDto } from './AnalyticsGateBodyDto';
import type { EntryBodyDto } from './EntryBodyDto';
import type { ScheduleBodyDto } from './ScheduleBodyDto';
import type { SegmentBodyDto } from './SegmentBodyDto';
import type { SendBodyDto } from './SendBodyDto';
import type { NodeKind } from './enums/NodeKind';

export type NodeBodyDto = { 
  analyticsGate?: AnalyticsGateBodyDto; 
  customProperties?: Record<string, string> | null; 
  entry?: EntryBodyDto; 
  kind?: NodeKind; 
  schedule?: ScheduleBodyDto; 
  segment?: SegmentBodyDto; 
  send?: SendBodyDto; 
};
