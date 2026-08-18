/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DecoratorRefDto } from './DecoratorRefDto';
import type { NodeBodyDto } from './NodeBodyDto';
import type { NodeOutcomeDto } from './NodeOutcomeDto';

export type GraphNodeDto = { 
  body?: NodeBodyDto; 
  id?: string; 
  incomingDecorators?: DecoratorRefDto[]; 
  lane?: string | null; 
  outcomes?: NodeOutcomeDto[]; 
  outgoingDecorators?: DecoratorRefDto[]; 
};
