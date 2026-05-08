/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ValueSource } from './ValueSource';
import type { BranchOp } from './enums/BranchOp';

export type BranchCondition = { 
  left: ValueSource; 
  op: BranchOp; 
  right?: ValueSource; 
};
