/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { PropertyBoundDto } from './PropertyBoundDto';
import type { ConditionKind } from './enums/ConditionKind';

export type ConditionDto = { 
  event?: string | null; 
  kind?: ConditionKind; 
  parts?: ConditionDto[] | null; 
  property?: PropertyBoundDto; 
  segment?: string | null; 
};
