import { RedisShardRequest } from './RedisShardRequest';

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
