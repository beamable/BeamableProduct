/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

export type AddRequestArgs = { 
  a: number; 
  b: number; 
};

export type GreetRequestArgs = { 
  name: string; 
};

export type WhoAmIResult = { 
  userId: bigint | string; 
  cid: string; 
  pid: string; 
  isAdmin: boolean; 
};

export type VisitResult = { 
  userId: bigint | string; 
  visits: number; 
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

export type SendResult = { 
  success: boolean; 
  attempted: number; 
  succeeded: number; 
  failed: number; 
  messages: string[]; 
};

export type SendPushToSelfRequestArgs = { 
  title: string; 
  body: string; 
  deepLink: string; 
};

export type AdminSendResult = { 
  success: boolean; 
  attempted: number; 
  succeeded: number; 
  failed: number; 
  messages: string[]; 
};

export type SendPushToPlayerRequestArgs = { 
  playerId: bigint | string; 
  title: string; 
  body: string; 
  deepLink: string; 
};
