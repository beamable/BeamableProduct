/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Operator } from './enums/Operator';
import type { TimeUnit } from './enums/TimeUnit';

export type RuleLeaf = { 
  key?: string | null; 
  ns?: string | null; 
  op?: Operator; 
  since?: TimeUnit; 
  values?: (string | number | number | boolean | (string | number | number | boolean)[])[] | null; 
};
