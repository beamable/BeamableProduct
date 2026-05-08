/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BranchStepConfig } from './BranchStepConfig';
import type { MicroserviceCallStepConfig } from './MicroserviceCallStepConfig';
import type { SetUserContextStepConfig } from './SetUserContextStepConfig';

export type UserStepConfig = { 
  id: string; 
  type: string; 
  branch?: BranchStepConfig; 
  microserviceCall?: MicroserviceCallStepConfig; 
  setUserContext?: SetUserContextStepConfig; 
};
