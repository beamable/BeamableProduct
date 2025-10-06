import { describe, expect, it, vi } from 'vitest';
import { saveToken } from '@/core/BeamUtils';
import type { TokenStorage } from '@/platform/types/TokenStorage';
import type { TokenResponse } from '@/__generated__/schemas';

describe('saveToken()', () => {
  describe('saveToken', () => {
    it('calls setTokenData on storage with access, refresh, and expiresIn', async () => {
      const tokenResponse: TokenResponse = {
        expires_in: '123',
        token_type: 'Bearer',
        access_token: 'access-token',
        refresh_token: 'refresh-token',
        scopes: [],
      };
      const storage: TokenStorage = {
        setTokenData: vi.fn().mockResolvedValue(undefined),
      } as unknown as TokenStorage;

      await saveToken(storage, tokenResponse);

      expect(storage.setTokenData).toHaveBeenCalledWith(
        expect.objectContaining({
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
        }),
      );
    });
  });
});
