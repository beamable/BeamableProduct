import type { TokenStorage } from '@/platform/types/TokenStorage';
import { BrowserTokenStorage } from '@/platform/BrowserTokenStorage';
import {
  readConfigBrowser,
  saveConfigBrowser,
} from '@/platform/BrowserConfigStorage';
import type { Config } from '@/platform/BrowserConfigStorage';

/** Default token storage for browser environments. */
export function defaultTokenStorage(tag?: string): TokenStorage {
  return new BrowserTokenStorage(tag);
}

export * from '@/core/Beam';
export * from '@/configs/BeamConfig';
export * from '@/network/http/types/HttpMethod';
export * from '@/network/http/types/HttpRequest';
export * from '@/network/http/types/HttpRequester';
export * from '@/network/http/types/HttpResponse';
export * from '@/platform/types/TokenStorage';
export { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';
export { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';
export {
  Config,
  readConfigBrowser as readConfig,
  saveConfigBrowser as saveConfig,
};
export { GET, POST, PUT, PATCH, DELETE } from '@/constants';
export {
  BeamWebSocketError,
  RefreshAccessTokenError,
  NoRefreshTokenError,
} from '@/constants/Errors';
