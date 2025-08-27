import type { TokenStorage } from '@/platform/types/TokenStorage';
import { NodeTokenStorage } from '@/platform/NodeTokenStorage';
import { readConfigNode, saveConfigNode } from '@/platform/NodeConfigStorage';
import { ContentStorage } from '@/platform/types/ContentStorage';
import { NodeContentStorage } from '@/platform/NodeContentStorage';

/** Default token storage for Node.js environments. */
export function defaultTokenStorage(pid: string, tag?: string): TokenStorage {
  return new NodeTokenStorage(pid, tag);
}

/** Default content storage for Node.js environments. */
export async function defaultContentStorage(): Promise<ContentStorage> {
  return await NodeContentStorage.open();
}

export { readConfigNode as readConfig, saveConfigNode as saveConfig };
