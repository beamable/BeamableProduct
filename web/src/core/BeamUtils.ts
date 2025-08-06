import type { TokenResponse } from '@/__generated__/schemas';
import { TokenStorage } from '@/platform/types/TokenStorage';

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
