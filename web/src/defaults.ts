import type { TokenStorage } from '@/platform/types/TokenStorage';
import { NodeTokenStorage } from '@/platform/NodeTokenStorage';
import { readConfigNode, saveConfigNode } from '@/platform/NodeConfigStorage';

/** Default token storage for Node.js environments. */
export function defaultTokenStorage(tag?: string): TokenStorage {
  return new NodeTokenStorage(tag);
}

export { readConfigNode as readConfig, saveConfigNode as saveConfig };
