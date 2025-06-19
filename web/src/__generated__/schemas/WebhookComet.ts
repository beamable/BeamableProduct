import { RouteParameters } from './RouteParameters';
import { RouteVariables } from './RouteVariables';
import { ServiceRoute } from './ServiceRoute';

export type WebhookComet = { 
  method: string; 
  parameters: RouteParameters; 
  route: ServiceRoute; 
  symbol: string; 
  variables: RouteVariables; 
  description?: string; 
};
