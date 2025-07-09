import { UpdateListingCooldownRequest } from './UpdateListingCooldownRequest';

export type CooldownModifierRequest = { 
  gamerTag: bigint | string; 
  updateListingCooldownRequests: UpdateListingCooldownRequest[]; 
};
