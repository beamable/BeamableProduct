export type RedisShard = { 
  masterHost: string; 
  shardId: number; 
  slaveHosts: string[]; 
};
