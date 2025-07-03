import { UploadURL } from './UploadURL';

export type BeamoBasicURLResponse = { 
  s3URLs: UploadURL[]; 
  serviceName: string; 
};
