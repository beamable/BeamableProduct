export type Party = { 
  id?: string | null; 
  leader?: string | null; 
  maxSize?: number; 
  members?: string[] | null; 
  pendingInvites?: string[] | null; 
  restriction?: string | null; 
};
