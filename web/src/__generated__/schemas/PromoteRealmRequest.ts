export type PromoteRealmRequest = { 
  sourcePid: string; 
  contentManifestIds?: string[]; 
  promotions?: string[]; 
};
