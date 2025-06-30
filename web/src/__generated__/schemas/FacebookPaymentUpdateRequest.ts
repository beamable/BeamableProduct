import { FacebookUpdatedEntry } from './FacebookUpdatedEntry';

export type FacebookPaymentUpdateRequest = { 
  entry: FacebookUpdatedEntry[]; 
  object: string; 
};
