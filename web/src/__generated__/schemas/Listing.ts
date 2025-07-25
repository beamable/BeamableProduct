/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CohortRequirement } from './CohortRequirement';
import type { EntitlementRequirement } from './EntitlementRequirement';
import type { OfferRequirement } from './OfferRequirement';
import type { Period } from './Period';
import type { PlayerStatRequirement } from './PlayerStatRequirement';
import type { Price } from './Price';
import type { Schedule } from './Schedule';

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
