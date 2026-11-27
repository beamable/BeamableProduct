/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DurationUnit } from './enums/DurationUnit';
import type { ZoneStatKind } from './enums/ZoneStatKind';
import type { ZoneStatPreset } from './enums/ZoneStatPreset';

export type ZoneStat = { 
  kind: ZoneStatKind; 
  stat: string; 
  decimals?: number; 
  preset?: ZoneStatPreset; 
  revision?: number; 
  typeId?: string | null; 
  unit?: DurationUnit; 
};
