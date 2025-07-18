export type BeamoV2PASuggestedIndex = { 
  id: string; 
  impact: string[]; 
  index: Record<string, number>[]; 
  namespace: string; 
  weight: number; 
};
