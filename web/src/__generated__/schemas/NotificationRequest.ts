/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { NotificationRequestData } from './NotificationRequestData';

export type NotificationRequest = { 
  payload: NotificationRequestData; 
  customChannelSuffix?: string; 
  dbid?: bigint | string; 
  dbids?: (bigint | string)[]; 
  useSignalWhenPossible?: boolean; 
};
