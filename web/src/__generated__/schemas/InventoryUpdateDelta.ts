/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CurrencyDelta } from './CurrencyDelta';
import type { ItemDeltas } from './ItemDeltas';

export type InventoryUpdateDelta = { 
  currencies: Record<string, CurrencyDelta>; 
  items: ItemDeltas; 
};
