import { StringStringKeyValuePair } from './StringStringKeyValuePair';

export type ServiceCall = { 
  body?: string | null; 
  headers?: StringStringKeyValuePair[] | null; 
  method?: string; 
  type?: string; 
  uri?: string; 
};
