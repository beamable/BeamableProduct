import type { TokenStorage } from '@/platform/types/TokenStorage';
import { NodeTokenStorage } from '@/platform/NodeTokenStorage';
import { readConfigNode, saveConfigNode } from '@/platform/NodeConfigStorage';
import type { Config } from '@/platform/NodeConfigStorage';

/** Default token storage for Node.js environments. */
export function defaultTokenStorage(tag?: string): TokenStorage {
  return new NodeTokenStorage(tag);
}

export * from '@/core/Beam';
export * from '@/configs/BeamConfig';
export * from '@/http/types/HttpMethod';
export * from '@/http/types/HttpRequest';
export * from '@/http/types/HttpRequester';
export * from '@/http/types/HttpResponse';
export * from '@/platform/types/TokenStorage';
export { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';
export { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';
export { Config, readConfigNode as readConfig, saveConfigNode as saveConfig };
export { GET, POST, PUT, PATCH, DELETE } from '@/constants';
export {
  ConfigurationError,
  RefreshAccessTokenError,
  NoRefreshTokenError,
} from '@/constants/Errors';
