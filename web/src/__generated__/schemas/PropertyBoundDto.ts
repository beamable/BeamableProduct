/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BoundDto } from './BoundDto';
import type { PropertyBoundTarget } from './enums/PropertyBoundTarget';
import type { PropertySetOp } from './enums/PropertySetOp';

export type PropertyBoundDto = { 
  max?: BoundDto; 
  min?: BoundDto; 
  propertyName?: string; 
  setOp?: PropertySetOp; 
  target?: PropertyBoundTarget; 
  values?: string[] | null; 
};
