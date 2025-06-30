import { CurrencyView } from './CurrencyView';
import { ItemGroup } from './ItemGroup';

export type InventoryView = { 
  currencies: CurrencyView[]; 
  items: ItemGroup[]; 
  scope?: string; 
};
