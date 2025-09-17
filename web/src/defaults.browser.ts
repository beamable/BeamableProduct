import type { TokenStorage } from '@/platform/types/TokenStorage';
import { BrowserTokenStorage } from '@/platform/BrowserTokenStorage';
import {
  readConfigBrowser,
  saveConfigBrowser,
} from '@/platform/BrowserConfigStorage';
import { ContentStorage } from '@/platform/types/ContentStorage';
import { BrowserContentStorage } from '@/platform/BrowserContentStorage';
import type { DefaultTokenStorageProps } from '@/platform/types/DefaultTokenStorageProps';

/** Default token storage for browser environments. */
export function defaultTokenStorage(
  props: DefaultTokenStorageProps,
): TokenStorage {
  return new BrowserTokenStorage(props.pid, props.tag);
}

/** Default content storage for browser environments. */
export async function defaultContentStorage(): Promise<ContentStorage> {
  return await BrowserContentStorage.open();
}

export { readConfigBrowser as readConfig, saveConfigBrowser as saveConfig };
