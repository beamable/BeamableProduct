export type PublishMessage = { 
  headers?: Record<string, string> | null; 
  message?: string; 
  persist?: boolean; 
  topic?: string; 
  type?: string; 
};
