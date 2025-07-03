export type JobRetryPolicy = { 
  maxRetryCount?: number; 
  retryDelayMs?: number; 
  useExponentialBackoff?: boolean; 
};
