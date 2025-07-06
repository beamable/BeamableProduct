import type { TokenStorage } from '@/platform/types/TokenStorage';
import { BrowserTokenStorage } from '@/platform/BrowserTokenStorage';
import {
  readConfigBrowser,
  saveConfigBrowser,
} from '@/platform/BrowserConfigStorage';

/** Default token storage for browser environments. */
export function defaultTokenStorage(tag?: string): TokenStorage {
  return new BrowserTokenStorage(tag);
}

export { readConfigBrowser as readConfig, saveConfigBrowser as saveConfig };
