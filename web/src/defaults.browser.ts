import type { TokenStorage } from '@/platform/types/TokenStorage';
import { BrowserTokenStorage } from '@/platform/BrowserTokenStorage';
import {
  readConfigBrowser,
  saveConfigBrowser,
} from '@/platform/BrowserConfigStorage';

/** Default token storage for browser environments. */
export function defaultTokenStorage(pid: string, tag?: string): TokenStorage {
  return new BrowserTokenStorage(pid, tag);
}

export { readConfigBrowser as readConfig, saveConfigBrowser as saveConfig };
