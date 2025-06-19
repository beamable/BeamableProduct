import { FederationType } from './enums/FederationType';

export type SupportedFederation = { 
  type: FederationType; 
  nameSpace?: string; 
  settings?: string; 
};
