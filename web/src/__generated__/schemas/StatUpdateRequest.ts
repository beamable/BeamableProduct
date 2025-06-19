export type StatUpdateRequest = { 
  add?: Record<string, string>; 
  emitAnalytics?: boolean; 
  objectId?: string; 
  set?: Record<string, string>; 
};
