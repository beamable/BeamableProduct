export type NewCustomerRequest = { 
  email: string; 
  password: string; 
  projectName: string; 
  alias?: string; 
  customerName?: string; 
  hierarchy?: boolean; 
};
