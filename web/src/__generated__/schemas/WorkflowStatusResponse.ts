/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { WorkflowStatus } from './enums/WorkflowStatus';

export type WorkflowStatusResponse = { 
  currentStepIndex?: number; 
  currentStepRetryCount?: number; 
  error?: string | null; 
  executionId?: string | null; 
  status?: WorkflowStatus; 
  workflowType?: string | null; 
};
