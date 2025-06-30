import { SendMailRequest } from './SendMailRequest';

export type BulkSendMailRequest = { 
  sendMailRequests: SendMailRequest[]; 
};
