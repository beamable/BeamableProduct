import { RealmRolesReport } from './RealmRolesReport';

export type AccountRolesReport = { 
  accountId: bigint | string; 
  email: string; 
  realms: RealmRolesReport[]; 
};
