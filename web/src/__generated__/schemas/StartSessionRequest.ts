import { SessionLanguageContext } from './SessionLanguageContext';

export type StartSessionRequest = { 
  customParams?: Record<string, string>; 
  device?: string; 
  deviceParams?: Record<string, string>; 
  gamer?: bigint | string; 
  language?: SessionLanguageContext; 
  locale?: string; 
  platform?: string; 
  shard?: string; 
  source?: string; 
  time?: bigint | string; 
};
