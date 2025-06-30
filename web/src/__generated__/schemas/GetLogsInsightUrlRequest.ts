export type GetLogsInsightUrlRequest = { 
  serviceName: string; 
  endTime?: bigint | string; 
  filter?: string; 
  filters?: string[]; 
  limit?: number; 
  order?: string; 
  startTime?: bigint | string; 
};
