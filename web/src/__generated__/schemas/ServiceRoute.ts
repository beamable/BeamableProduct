import { WebhookServiceType } from './enums/WebhookServiceType';

export type ServiceRoute = { 
  endpoint: string; 
  service: string; 
  serviceTypeStr: WebhookServiceType; 
};
