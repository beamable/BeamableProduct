import { ClientContentInfoJson } from './ClientContentInfoJson';

export type ClientManifestJsonResponse = { 
  entries: ClientContentInfoJson[]; 
  publisherAccountId?: bigint | string; 
  uid?: string; 
};
