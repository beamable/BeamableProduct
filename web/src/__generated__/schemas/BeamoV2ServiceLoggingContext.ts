/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2LogContextRule } from './BeamoV2LogContextRule';
import type { BeamoV2LogLevel } from './enums/BeamoV2LogLevel';

export type BeamoV2ServiceLoggingContext = { 
  defaultLogLevel: BeamoV2LogLevel; 
  routingKey: string; 
  serviceName: string; 
  id?: string; 
  rules?: BeamoV2LogContextRule[]; 
};
