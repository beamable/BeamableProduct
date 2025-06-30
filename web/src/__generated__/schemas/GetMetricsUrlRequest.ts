export type GetMetricsUrlRequest = { 
  metricName: string; 
  serviceName: string; 
  endTime?: bigint | string; 
  period?: number; 
  startTime?: bigint | string; 
};
