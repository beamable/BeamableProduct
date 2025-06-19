import { SendNotification } from './SendNotification';

export type SendMsg = { 
  to: (bigint | string)[]; 
  data?: Record<string, string>; 
  notification?: SendNotification; 
};
