import { MetadataPair } from './MetadataPair';

export type UploadRequestFromPortal = { 
  objectKey: string; 
  sizeInBytes: bigint | string; 
  lastModified?: bigint | string; 
  metadata?: MetadataPair[]; 
};
