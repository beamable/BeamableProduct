import { ClientDataEntry } from './ClientDataEntry';
import { PlayerOfferView } from './PlayerOfferView';

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
