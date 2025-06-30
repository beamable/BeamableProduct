import { BinaryReference } from './BinaryReference';
import { ContentReference } from './ContentReference';
import { TextReference } from './TextReference';

export type ContentBasicManifest = { 
  checksum: string; 
  created: bigint | string; 
  id: string; 
  references: (ContentReference | TextReference | BinaryReference)[]; 
  archived?: boolean; 
  publisherAccountId?: bigint | string; 
  uid?: string; 
};
