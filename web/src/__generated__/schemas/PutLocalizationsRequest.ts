import { LocalizedValue } from './LocalizedValue';

export type PutLocalizationsRequest = { 
  localizations: Record<string, LocalizedValue[]>; 
};
