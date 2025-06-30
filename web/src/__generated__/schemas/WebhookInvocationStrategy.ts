import { WebhookInvocationType } from './enums/WebhookInvocationType';
import { WebhookRetryType } from './enums/WebhookRetryType';

export type WebhookInvocationStrategy = { 
  invocationType: WebhookInvocationType; 
  retryType: WebhookRetryType; 
};
