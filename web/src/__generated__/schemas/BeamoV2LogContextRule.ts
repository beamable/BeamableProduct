/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2ContextRuleFilter } from './BeamoV2ContextRuleFilter';
import type { BeamoV2LogContextRuleAuthor } from './BeamoV2LogContextRuleAuthor';
import type { BeamoV2LogLevel } from './enums/BeamoV2LogLevel';

export type BeamoV2LogContextRule = { 
  enabled: boolean; 
  logLevel: BeamoV2LogLevel; 
  ruleFilters: BeamoV2ContextRuleFilter[]; 
  author?: BeamoV2LogContextRuleAuthor; 
  createdAt?: bigint | string; 
  description?: string | null; 
  expiresAt?: bigint | string | null; 
  name?: string | null; 
  ruleId?: string; 
  updatedAt?: bigint | string; 
  whoLastEdit?: BeamoV2LogContextRuleAuthor; 
};
