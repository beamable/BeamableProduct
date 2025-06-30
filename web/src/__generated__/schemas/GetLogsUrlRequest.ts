export type GetLogsUrlRequest = { 
  serviceName: string; 
  endTime?: bigint | string; 
  filter?: string; 
  limit?: number; 
  nextToken?: string; 
  startTime?: bigint | string; 
};
