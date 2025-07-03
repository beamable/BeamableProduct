import { RedisShard } from './RedisShard';

export type DataDomain = { 
  memcachedHosts: string[]; 
  mongoHosts: string[]; 
  mongoSSLEnabled: boolean; 
  mongoSharded: boolean; 
  messageBusAnalytics?: string[]; 
  messageBusCommon?: string[]; 
  mongoSSL?: boolean; 
  mongoSrvAddress?: string; 
  redisShards?: RedisShard[]; 
};
