import { BeamoV2StringStringKeyValuePair } from './BeamoV2StringStringKeyValuePair';

export type BeamoV2SignedRequest = { 
  body?: string; 
  headers?: BeamoV2StringStringKeyValuePair[]; 
  method?: string; 
  url?: string; 
};
