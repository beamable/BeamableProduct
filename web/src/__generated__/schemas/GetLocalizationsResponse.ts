import { LocalizedValue } from './LocalizedValue';

export type GetLocalizationsResponse = { 
  localizations: Record<string, LocalizedValue[]>; 
};
