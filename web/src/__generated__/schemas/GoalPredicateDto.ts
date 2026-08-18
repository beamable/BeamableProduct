/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { GoalPredicateKind } from './enums/GoalPredicateKind';

export type GoalPredicateDto = { 
  event?: string | null; 
  kind?: GoalPredicateKind; 
  predicates?: GoalPredicateDto[] | null; 
  withinMs?: bigint | string | null; 
};
