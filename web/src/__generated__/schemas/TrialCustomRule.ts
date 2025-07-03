export type TrialCustomRule = { 
  customAttr: string; 
  customOp: string; 
  customVal: string[]; 
  access?: string; 
  customItems?: TrialCustomRule[]; 
  domain?: string; 
};
