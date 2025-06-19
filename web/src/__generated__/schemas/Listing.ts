import { CohortRequirement } from './CohortRequirement';
import { EntitlementRequirement } from './EntitlementRequirement';
import { OfferRequirement } from './OfferRequirement';
import { Period } from './Period';
import { PlayerStatRequirement } from './PlayerStatRequirement';
import { Price } from './Price';
import { Schedule } from './Schedule';

export type Listing = { 
  clientData: Record<string, string>; 
  cohortRequirements: CohortRequirement[]; 
  entitlementRequirements: EntitlementRequirement[]; 
  offerRequirements: OfferRequirement[]; 
  offerSymbol: string; 
  playerStatRequirements: PlayerStatRequirement[]; 
  price: Price; 
  symbol: string; 
  activeDurationCoolDownSeconds?: number; 
  activeDurationPurchaseLimit?: number; 
  activeDurationSeconds?: number; 
  activePeriod?: Period; 
  buttonText?: Record<string, string>; 
  purchaseLimit?: number; 
  schedule?: Schedule; 
  scheduleInstancePurchaseLimit?: number; 
};
