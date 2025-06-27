export type RealmConfigChangeRequest = { 
  deletes?: string[]; 
  upserts?: Record<string, string>; 
};
