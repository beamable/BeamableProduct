export type RedisShardRequest = { 
  masterHost: string; 
  shardId: number; 
  slaveHosts: string; 
};
