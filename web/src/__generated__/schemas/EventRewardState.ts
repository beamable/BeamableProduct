/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { EventInventoryPendingRewards } from './EventInventoryPendingRewards';
import type { EventInventoryRewardCurrency } from './EventInventoryRewardCurrency';
import type { EventInventoryRewardItem } from './EventInventoryRewardItem';
import type { EventRewardObtain } from './EventRewardObtain';
import type { ItemCreateRequest } from './ItemCreateRequest';

export type EventRewardState = { 
  claimed: boolean; 
  earned: boolean; 
  min: number; 
  pendingInventoryRewards: EventInventoryPendingRewards; 
  currencies?: EventInventoryRewardCurrency[]; 
  items?: EventInventoryRewardItem[]; 
  max?: number; 
  obtain?: EventRewardObtain[]; 
  pendingCurrencyRewards?: Record<string, string>; 
  pendingEntitlementRewards?: Record<string, string>; 
  pendingItemRewards?: ItemCreateRequest[]; 
};
