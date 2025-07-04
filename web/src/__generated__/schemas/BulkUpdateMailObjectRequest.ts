import { MyMailUpdate } from './MyMailUpdate';

export type BulkUpdateMailObjectRequest = { 
  updateMailRequests: MyMailUpdate[]; 
};
