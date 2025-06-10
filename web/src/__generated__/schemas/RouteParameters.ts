import { RouteParameter } from './RouteParameter';

export type RouteParameters = { 
  parameters: RouteParameter[]; 
  objectId?: string; 
  payload?: string; 
};
