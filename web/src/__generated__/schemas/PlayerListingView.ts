/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ClientDataEntry } from './ClientDataEntry';
import type { PlayerOfferView } from './PlayerOfferView';

export type PlayerListingView = { 
  active: boolean; 
  clientData: Record<string, string>; 
  clientDataList: ClientDataEntry[]; 
  offer: PlayerOfferView; 
  queryAfterPurchase: boolean; 
  secondsActive: bigint | string; 
  symbol: string; 
  cooldown?: number; 
  purchasesRemain?: number; 
  secondsRemain?: bigint | string; 
};
