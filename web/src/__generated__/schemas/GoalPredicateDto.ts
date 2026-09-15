/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { PropertyBoundDto } from './PropertyBoundDto';
import type { GoalPredicateKind } from './enums/GoalPredicateKind';
import type { PropertyBoundsMode } from './enums/PropertyBoundsMode';

export type GoalPredicateDto = { 
  event?: string | null; 
  kind?: GoalPredicateKind; 
  predicates?: GoalPredicateDto[] | null; 
  properties?: PropertyBoundDto[] | null; 
  propertiesMode?: PropertyBoundsMode; 
  withinMs?: bigint | string | null; 
};
