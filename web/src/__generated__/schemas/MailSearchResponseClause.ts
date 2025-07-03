import { Message } from './Message';

export type MailSearchResponseClause = { 
  count: bigint | string; 
  name: string; 
  content?: Message[]; 
};
