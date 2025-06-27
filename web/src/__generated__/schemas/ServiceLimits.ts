import { BeamoLimits } from './BeamoLimits';
import { ContentLimits } from './ContentLimits';
import { GatewayLimits } from './GatewayLimits';

export type ServiceLimits = { 
  beamo?: BeamoLimits; 
  content?: ContentLimits; 
  gateway?: GatewayLimits; 
};
