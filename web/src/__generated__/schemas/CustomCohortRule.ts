export type CustomCohortRule = { 
  customAttr: string; 
  customOp: string; 
  customVal: string[]; 
  access?: string; 
  customItems?: CustomCohortRule[]; 
  domain?: string; 
};
