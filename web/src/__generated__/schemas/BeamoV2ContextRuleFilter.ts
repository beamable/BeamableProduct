/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2PathRuleOperationType } from './enums/BeamoV2PathRuleOperationType';
import type { BeamoV2PlayerRuleOperationType } from './enums/BeamoV2PlayerRuleOperationType';

export type BeamoV2ContextRuleFilter = { 
  paths?: string[]; 
  pathsOperationType?: BeamoV2PathRuleOperationType; 
  playerIdOperationType?: BeamoV2PlayerRuleOperationType; 
  playerIds?: (bigint | string)[]; 
};
