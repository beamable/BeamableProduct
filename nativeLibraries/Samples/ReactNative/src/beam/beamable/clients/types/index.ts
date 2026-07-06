/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

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
