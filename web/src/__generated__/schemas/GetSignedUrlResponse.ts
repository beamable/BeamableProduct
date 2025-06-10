import { GetLogsUrlHeader } from './GetLogsUrlHeader';

export type GetSignedUrlResponse = { 
  body: string; 
  headers: GetLogsUrlHeader[]; 
  method: string; 
  url: string; 
};
