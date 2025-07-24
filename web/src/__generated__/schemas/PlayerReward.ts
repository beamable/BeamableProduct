/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CurrencyChangeReward } from './CurrencyChangeReward';
import type { ItemCreateRequest } from './ItemCreateRequest';
import type { NewItemReward } from './NewItemReward';
import type { WebhookReward } from './WebhookReward';

export type PlayerReward = { 
  addCurrencyMap: Record<string, string>; 
  addItemRequests: ItemCreateRequest[]; 
  addItems?: NewItemReward[]; 
  applyVipBonus?: boolean; 
  callWebhooks?: WebhookReward[]; 
  changeCurrencies?: CurrencyChangeReward[]; 
  description?: string; 
};
