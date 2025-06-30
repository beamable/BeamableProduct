import { MetadataPair } from './MetadataPair';

export type UploadRequest = { 
  objectKey: string; 
  sizeInBytes: bigint | string; 
  checksum?: string; 
  deleted?: boolean; 
  lastModified?: bigint | string; 
  metadata?: MetadataPair[]; 
};
