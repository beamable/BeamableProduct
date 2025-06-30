import { StringStringKeyValuePair } from './StringStringKeyValuePair';

export type HttpCall = { 
  body?: string | null; 
  contentType?: string | null; 
  headers?: StringStringKeyValuePair[] | null; 
  method?: string; 
  type?: string; 
  uri?: string; 
};
