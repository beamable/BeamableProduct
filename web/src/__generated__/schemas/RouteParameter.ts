import { VariableReference } from './VariableReference';

export type RouteParameter = { 
  body: string; 
  name: string; 
  typeName: string; 
  variableRef?: VariableReference; 
};
