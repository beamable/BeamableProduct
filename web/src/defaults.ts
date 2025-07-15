import type { TokenStorage } from '@/platform/types/TokenStorage';
import { NodeTokenStorage } from '@/platform/NodeTokenStorage';
import { readConfigNode, saveConfigNode } from '@/platform/NodeConfigStorage';

/** Default token storage for Node.js environments. */
export function defaultTokenStorage(pid: string, tag?: string): TokenStorage {
  return new NodeTokenStorage(pid, tag);
}

export { readConfigNode as readConfig, saveConfigNode as saveConfig };
