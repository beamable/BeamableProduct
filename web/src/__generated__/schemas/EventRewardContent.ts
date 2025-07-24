/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { EventInventoryRewardCurrency } from './EventInventoryRewardCurrency';
import type { EventInventoryRewardItem } from './EventInventoryRewardItem';
import type { EventRewardObtain } from './EventRewardObtain';

export type EventRewardContent = { 
  min: number; 
  currencies?: EventInventoryRewardCurrency[]; 
  items?: EventInventoryRewardItem[]; 
  max?: number; 
  obtain?: EventRewardObtain[]; 
};
