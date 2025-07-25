/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SteamOrderInfoItem } from './SteamOrderInfoItem';

export type SteamOrderInfoResponse = { 
  country: string; 
  currency: string; 
  items: SteamOrderInfoItem[]; 
  orderid: bigint | string; 
  status: string; 
  steamid: bigint | string; 
  time: string; 
  timecreated: string; 
  transid: bigint | string; 
  usstate: string; 
};
