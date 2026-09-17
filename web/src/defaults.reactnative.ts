import type { TokenStorage } from '@/platform/types/TokenStorage';
import { ReactNativeTokenStorage } from '@/platform/ReactNativeTokenStorage';
import {
  readConfigReactNative,
  saveConfigReactNative,
} from '@/platform/ReactNativeConfigStorage';
import { ContentStorage } from '@/platform/types/ContentStorage';
import { ReactNativeContentStorage } from '@/platform/ReactNativeContentStorage';
import type { DefaultTokenStorageProps } from '@/platform/types/DefaultTokenStorageProps';

/** Default token storage for React Native environments. */
export function defaultTokenStorage(
  props: DefaultTokenStorageProps,
): TokenStorage {
  return new ReactNativeTokenStorage(props.pid, props.tag);
}

/** Default content storage for React Native environments. */
export async function defaultContentStorage(): Promise<ContentStorage> {
  return await ReactNativeContentStorage.open();
}

export {
  readConfigReactNative as readConfig,
  saveConfigReactNative as saveConfig,
};
