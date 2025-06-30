import { CurrencyChangeReward } from './CurrencyChangeReward';
import { ItemCreateRequest } from './ItemCreateRequest';
import { NewItemReward } from './NewItemReward';
import { WebhookReward } from './WebhookReward';

export type PlayerReward = { 
  addCurrencyMap: Record<string, string>; 
  addItemRequests: ItemCreateRequest[]; 
  addItems?: NewItemReward[]; 
  applyVipBonus?: boolean; 
  callWebhooks?: WebhookReward[]; 
  changeCurrencies?: CurrencyChangeReward[]; 
  description?: string; 
};
