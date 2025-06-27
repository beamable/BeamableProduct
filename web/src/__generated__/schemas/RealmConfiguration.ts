import { WebSocketConfiguration } from './WebSocketConfiguration';

export type RealmConfiguration = { 
  environment: string; 
  microserviceEcrURI: string; 
  microserviceURI: string; 
  portalURI: string; 
  storageBrowserURI: string; 
  websocketConfig: WebSocketConfiguration; 
};
