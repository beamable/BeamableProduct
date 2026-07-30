/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CustomerActorAccount } from './CustomerActorAccount';
import type { Realm } from './Realm';
import type { ActivationStatus } from './enums/ActivationStatus';
import type { PaymentStatus } from './enums/PaymentStatus';

export type CustomerActorCustomer = { 
  customerId: bigint | string; 
  name: string; 
  accounts?: CustomerActorAccount[]; 
  activationStatus?: ActivationStatus; 
  alias?: string | null; 
  config?: Record<string, string>; 
  contact?: string | null; 
  created?: Date; 
  paymentStatus?: PaymentStatus; 
  realms?: Realm[]; 
  requiresCustomTier?: boolean; 
  stripeCustomerId?: string | null; 
  updated?: Date; 
};
