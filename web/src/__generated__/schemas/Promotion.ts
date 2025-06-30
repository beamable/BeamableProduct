import { Promotable } from './Promotable';

export type Promotion = { 
  destination: Promotable; 
  id: string; 
  source: Promotable; 
};
