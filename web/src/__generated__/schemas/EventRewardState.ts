import { EventInventoryPendingRewards } from './EventInventoryPendingRewards';
import { EventInventoryRewardCurrency } from './EventInventoryRewardCurrency';
import { EventInventoryRewardItem } from './EventInventoryRewardItem';
import { EventRewardObtain } from './EventRewardObtain';
import { ItemCreateRequest } from './ItemCreateRequest';

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
