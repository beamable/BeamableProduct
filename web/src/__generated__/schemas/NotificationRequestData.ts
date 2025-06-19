export type NotificationRequestData = { 
  channel?: string; 
  context?: string; 
  messageFull?: string; 
  messageKey?: string; 
  messageParams?: string[]; 
  meta?: Record<string, string>; 
  shard?: string; 
};
