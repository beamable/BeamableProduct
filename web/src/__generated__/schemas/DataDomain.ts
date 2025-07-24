/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { RedisShard } from './RedisShard';

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
