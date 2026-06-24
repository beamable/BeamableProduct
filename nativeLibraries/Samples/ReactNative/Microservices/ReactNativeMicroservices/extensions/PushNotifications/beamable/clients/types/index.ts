/**
 * Types for the PushNotificationService endpoints.
 *
 * Mirrors the Beamable web-client generator output (see
 * src/beam/beamable/clients/types/index.ts in the app). Re-run
 * `beam portal extension add-microservice PushNotifications PushNotificationService`
 * to regenerate once the realm's portal-config endpoint is reachable.
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

export type SendResult = {
  success: boolean;
  attempted: number;
  succeeded: number;
  failed: number;
  messages: string[];
};

export type AdminSendResult = {
  success: boolean;
  attempted: number;
  succeeded: number;
  failed: number;
  messages: string[];
};

/** A single offer carried by a campaign push (§3.3). */
export type PushOffer = {
  itemId: string;
  value: string;
  customData?: string; // free-form JSON object, as a string
};

/**
 * §3.3 Notification Intent Data + the notification content. All campaign fields
 * are optional — supplying none reduces to a plain title/body/deepLink push.
 */
export type PushCampaignRequest = {
  title: string;
  body: string;
  deepLink: string; // canonical key on the wire: "deeplink"

  campaignId?: string;
  nodeId?: string;
  gamerTag?: string; // Beamable dbid; defaults to the target player id when unset
  accountId?: string; // Beamable account id
  cidPid?: string; // "<cid>.<pid>" realm scope
  offers?: PushOffer[]; // optional offers array
  campaignData?: string; // free-form JSON object, as a string
};

/** Args for SendCampaignPushToSelf — the campaign request (no playerId; the caller is the target). */
export type SendCampaignPushToSelfRequestArgs = PushCampaignRequest;

/** Args for SendCampaignPushToPlayer — the target playerId plus the campaign request fields. */
export type SendCampaignPushToPlayerRequestArgs = {
  playerId: bigint | string;
} & PushCampaignRequest;

/** A player with at least one registered device (no token is exposed). */
export type RegisteredPlayer = {
  playerId: bigint | string;
  deviceCount: number;
  platforms: string[]; // distinct: "apns" and/or "fcm"
  lastUpdated: bigint | string; // newest device's updatedAt (unix seconds)
  gamePlatform?: string; // THORIUM_GAME_PLATFORM (e.g. "Web")
  gameDevice?: string; // THORIUM_GAME_DEVICE (e.g. "Desktop")
};

/** Roster returned by ListRegisteredPlayers. */
export type RegisteredPlayerList = {
  players: RegisteredPlayer[];
  message?: string; // set only when the roster couldn't be produced
};

/** Secret-free result of CheckFcmConfig. */
export type FcmConfigStatus = {
  configured: boolean;
  privateKeyLoaded: boolean;
  projectId: string;
  clientEmail: string;
  tokenUri: string;
  message: string;
};
