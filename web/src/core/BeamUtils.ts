import type { TokenResponse } from '@/__generated__/schemas';
import { TokenStorage } from '@/platform/types/TokenStorage';
import type { RefreshableServiceMap } from '@/core/types';

/** Saves the access token, refresh token, and expiration time from a token response to the token storage. */
export async function saveToken(
  tokenStorage: TokenStorage,
  tokenResponse: TokenResponse,
): Promise<void> {
  await tokenStorage.setAccessToken(tokenResponse.access_token as string);
  await tokenStorage.setRefreshToken(tokenResponse.refresh_token as string);
  await tokenStorage.setExpiresIn(
    Date.now() + Number(tokenResponse.expires_in),
  );
}

/** Parses a raw socket message string into a structured object based on the expected `RefreshableServiceMap` type. */
export function parseSocketMessage<K extends keyof RefreshableServiceMap>(
  rawMessage: string,
): RefreshableServiceMap[K] {
  const parsed = JSON.parse(rawMessage);

  const { scopes, delay, ...rest } = parsed;

  // The unknown field will be the remaining key in `rest`
  const [unknownKey] = Object.keys(rest);
  const data = rest[unknownKey] as RefreshableServiceMap[K]['data'];

  return {
    data,
    ...parsed,
  };
}
