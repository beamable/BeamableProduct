import { NotificationRequestData } from './NotificationRequestData';

export type NotificationRequest = { 
  payload: NotificationRequestData; 
  customChannelSuffix?: string; 
  dbid?: bigint | string; 
  dbids?: (bigint | string)[]; 
  useSignalWhenPossible?: boolean; 
};
