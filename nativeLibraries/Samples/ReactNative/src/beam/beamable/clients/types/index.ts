/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

export type PushConfigStatus = { 
  apnsConfigured: boolean; 
  apnsSummary: string; 
  fcmConfigured: boolean; 
  fcmPrivateKeyLoaded: boolean; 
  fcmSummary: string; 
  message: string; 
};

export type RegisterResult = { 
  success: boolean; 
  deviceCount: number; 
  message: string; 
};

export type RegisterDeviceTokenRequestArgs = { 
  token: string; 
  environment: string; 
  platform: string; 
};

export type UnregisterResult = { 
  success: boolean; 
  deviceCount: number; 
  message: string; 
};

export type UnregisterDeviceTokenRequestArgs = { 
  token: string; 
};

export type DeviceInfo = { 
  token: string; 
  platform: string; 
  environment: string; 
  updatedAt: bigint | string; 
};

export type DeviceList = { 
  devices: DeviceInfo[]; 
};

export type WebhookResult = { 
  success: boolean; 
  statusCode: number; 
  message: string; 
};

export type ForwardFunnelToSlackRequestArgs = { 
  funnelData: string; 
};

export type RegisteredPlayer = { 
  playerId: bigint | string; 
  deviceCount: number; 
  platforms: string[]; 
  lastUpdated: bigint | string; 
  gamePlatform: string; 
  gameDevice: string; 
};

export type RegisteredPlayerList = { 
  players: RegisteredPlayer[]; 
  message: string; 
};

export type AdminSendResult = { 
  success: boolean; 
  attempted: number; 
  succeeded: number; 
  failed: number; 
  messages: string[]; 
};

export type PushOffer = { 
  itemId: string; 
  value: string; 
  customData?: string; 
};

export type PushCampaignRequest = { 
  title: string; 
  body: string; 
  deepLink: string; 
  campaignId?: string; 
  nodeId?: string; 
  gamerTag?: string; 
  accountId?: string; 
  cidPid?: string; 
  offers?: PushOffer[]; 
  campaignData?: string; 
};

export type SendCampaignPushToPlayerRequestArgs = { 
  playerId: bigint | string; 
  request: PushCampaignRequest; 
};

export type LaunchResult = { 
  success: boolean; 
  playersAttempted: number; 
  playersDelivered: number; 
  devicesDelivered: number; 
  devicesFailed: number; 
  messages: string[]; 
};

export type CampaignPayload = { 
  name: string; 
  campaignId: string; 
  nodeId: string; 
  title: string; 
  body: string; 
  deepLink: string; 
  targetPlayerIds: string[]; 
  offers: PushOffer[]; 
  campaignData: string; 
};

export type LaunchCampaignRequestArgs = { 
  campaign: CampaignPayload; 
};

export type AudienceEstimate = { 
  total: number; 
  reachable: number; 
  suppressed: number; 
};

export type EstimateAudienceRequestArgs = { 
  segments: string[]; 
};

export type FcmConfigStatus = { 
  configured: boolean; 
  privateKeyLoaded: boolean; 
  projectId: string; 
  clientEmail: string; 
  tokenUri: string; 
  message: string; 
};
