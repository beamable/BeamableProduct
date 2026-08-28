/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

export type NotificationFieldDto = { 
  id: string; 
  label: string; 
  type: string; 
  options: string[]; 
  required: boolean; 
  defaultValue: string; 
  help: string; 
  section: string; 
};

export type NotificationStyleDto = { 
  id: string; 
  label: string; 
  fieldIds: string[]; 
  delivery: string; 
  attributesType: string; 
  attributeFieldIds: string[]; 
  contentStateFieldIds: string[]; 
  contentStateDefaultsJson: string; 
};

export type NotificationStyleConfigDto = { 
  fields: NotificationFieldDto[]; 
  styles: NotificationStyleDto[]; 
};

export type SaveNotificationStyleConfigRequestArgs = { 
  config: NotificationStyleConfigDto; 
};

export type PushConfigStatus = { 
  apnsConfigured: boolean; 
  apnsSummary: string; 
  apnsKeyLoaded: boolean; 
  apnsProbe: string; 
  fcmConfigured: boolean; 
  fcmPrivateKeyLoaded: boolean; 
  fcmSummary: string; 
  apnsMissingKeys: string[]; 
  fcmMissingKeys: string[]; 
  apnsInvalidReason: string; 
  fcmInvalidReason: string; 
  secretsReadable: boolean; 
  secretsError: string; 
  message: string; 
};

export type OptedInPlayer = { 
  playerId: bigint | string; 
  deviceCount: number; 
  platforms: string[]; 
  lastUpdated: bigint | string; 
};

export type OptedInPlayerList = { 
  players: OptedInPlayer[]; 
  message: string; 
};

export type RunningLiveActivity = { 
  activityId: string; 
  attributesType: string; 
  updatedAt: bigint | string; 
};

export type LiveActivityList = { 
  activities: RunningLiveActivity[]; 
  message: string; 
};

export type ListLiveActivitiesRequestArgs = { 
  playerId: bigint | string; 
};

export type MailCategoryList = { 
  categories: string[]; 
  sampledPlayers: number; 
  message: string; 
};

export type MilestoneInfo = { 
  id: string; 
  name: string; 
  description: string; 
  headerColor: string; 
  iconSize: string; 
};

export type GetMilestoneInfoRequestArgs = { 
  milestoneId: string; 
};

export type DockedInfo = { 
  id: string; 
  label: string; 
  value: string; 
  change: string; 
  tone: string; 
  spark: number[]; 
};

export type DockedInfoListResponse = { 
  items: DockedInfo[]; 
};

export type StringListResponse = { 
  items: string[]; 
};

export type SegmentSummary = { 
  id: string; 
  name: string; 
  players: number; 
  additions30d: number; 
  activePlayers5d: number; 
  avgLtv: number; 
  arpu: number; 
};

export type SegmentSummaryListResponse = { 
  items: SegmentSummary[]; 
};

export type VipPlayer = { 
  id: string; 
  name: string; 
  ltv: number; 
  agentId: string; 
  lastSession: string; 
  status: string; 
};

export type VipPlayerListResponse = { 
  items: VipPlayer[]; 
};

export type VipAgent = { 
  id: string; 
  name: string; 
  playerCount: number; 
  avatar: string; 
};

export type VipAgentListResponse = { 
  items: VipAgent[]; 
};

export type ManagementGetAgentPlayersRequestArgs = { 
  agentId: string; 
};

export type AgentInfoKpi = { 
  id: string; 
  label: string; 
  value: string; 
  change: string; 
  tone: string; 
  spark: number[]; 
};

export type AgentInfoKpiListResponse = { 
  items: AgentInfoKpi[]; 
};

export type ManagementGetAgentSummaryRequestArgs = { 
  agentId: string; 
};

export type ManagementCreateAgentRequestArgs = { 
  name: string; 
};

export type ManagementDeleteAgentRequestArgs = { 
  agentId: string; 
};

export type ManagementAssignPlayerAgentRequestArgs = { 
  playerId: string; 
  agentId: string; 
};

export type ManagementRemovePlayerRequestArgs = { 
  playerId: string; 
};

export type OrgUser = { 
  id: string; 
  email: string; 
  role: string; 
};

export type OrgUserListResponse = { 
  items: OrgUser[]; 
};

export type OutreachRow = { 
  id: string; 
  metric: string; 
  lastDay: string; 
  lastMonth: string; 
};

export type OutreachRowListResponse = { 
  items: OutreachRow[]; 
};

export type ManagementGetAgentOutreachRequestArgs = { 
  agentId: string; 
};

export type VipAlert = { 
  id: string; 
  title: string; 
  time: string; 
  severity: string; 
  agent: string; 
};

export type VipAlertListResponse = { 
  items: VipAlert[]; 
};

export type VipTicket = { 
  id: string; 
  player: string; 
  playerAgent: string; 
  assignedTo: string; 
  lastUpdate: string; 
  description: string; 
  status: string; 
};

export type VipTicketListResponse = { 
  items: VipTicket[]; 
};

export type VipCampaign = { 
  id: string; 
  name: string; 
  segments: string[]; 
  status: string; 
  schedule: string; 
  createdBy: string; 
};

export type VipCampaignListResponse = { 
  items: VipCampaign[]; 
};

export type VipAutomation = { 
  id: string; 
  name: string; 
  creator: string; 
  status: string; 
};

export type VipAutomationListResponse = { 
  items: VipAutomation[]; 
};

export type AgentGetDockedInfoCatalogRequestArgs = { 
  agentName: string; 
};

export type AgentGetSegmentCatalogRequestArgs = { 
  agentName: string; 
};

export type AgentGetPlayersRequestArgs = { 
  agentName: string; 
};

export type AgentGetAlertsRequestArgs = { 
  agentName: string; 
};

export type AgentGetTicketsRequestArgs = { 
  agentName: string; 
};

export type AgentGetCampaignsRequestArgs = { 
  agentName: string; 
};

export type AgentGetAutomationsRequestArgs = { 
  agentName: string; 
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

export type StatEntry = { 
  key: string; 
  value: string; 
};

export type StatListResponse = { 
  items: StatEntry[]; 
  playerId: bigint | string; 
};

export type StatResult = { 
  success: boolean; 
  key: string; 
  value: string; 
  message: string; 
};

export type SetMyStatRequestArgs = { 
  key: string; 
  value: string; 
};

export type AddToMyStatRequestArgs = { 
  key: string; 
  amount: bigint | string; 
};

export type DeleteMyStatRequestArgs = { 
  key: string; 
};

export type CreatedPlayer = { 
  playerId: bigint | string; 
  value: string; 
};

export type BulkCreateResult = { 
  success: boolean; 
  requested: number; 
  created: number; 
  players: CreatedPlayer[]; 
  message: string; 
};

export type CreatePlayersWithStatRequestArgs = { 
  count: number; 
  key: string; 
  value: string; 
};

export type GetPlayerStatsRequestArgs = { 
  playerId: bigint | string; 
};

export type SetPlayerStatRequestArgs = { 
  playerId: bigint | string; 
  key: string; 
  value: string; 
};
