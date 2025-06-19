import { EventInventoryRewardCurrency } from './EventInventoryRewardCurrency';
import { EventInventoryRewardItem } from './EventInventoryRewardItem';
import { EventRewardObtain } from './EventRewardObtain';

export type EventRewardContent = { 
  min: number; 
  currencies?: EventInventoryRewardCurrency[]; 
  items?: EventInventoryRewardItem[]; 
  max?: number; 
  obtain?: EventRewardObtain[]; 
};
