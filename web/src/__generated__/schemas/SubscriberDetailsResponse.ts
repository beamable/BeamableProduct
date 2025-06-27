export type SubscriberDetailsResponse = { 
  authenticationKey: string; 
  customChannelPrefix: string; 
  gameNotificationChannel: string; 
  playerChannel: string; 
  playerChannels: string[]; 
  playerForRealmChannel: string; 
  subscribeKey: string; 
  gameGlobalNotificationChannel?: string; 
};
