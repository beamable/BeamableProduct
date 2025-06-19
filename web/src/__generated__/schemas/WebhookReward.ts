import { WebhookComet } from './WebhookComet';
import { WebhookInvocationStrategy } from './WebhookInvocationStrategy';

export type WebhookReward = { 
  strategy: WebhookInvocationStrategy; 
  webHookComet?: WebhookComet; 
  webhookSymbol?: string; 
};
