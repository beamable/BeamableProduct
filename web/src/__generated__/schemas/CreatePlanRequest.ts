/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { RedisShardRequest } from './RedisShardRequest';

export type CreatePlanRequest = { 
  memcachedHosts: string; 
  mongoHosts: string; 
  mongoSSL: boolean; 
  name: string; 
  platformJBDC: string; 
  redisShards: RedisShardRequest[]; 
  sharded: boolean; 
  messageBusAnalytics?: string[]; 
  messageBusCommon?: string[]; 
  mongoSrvAddress?: string; 
};
