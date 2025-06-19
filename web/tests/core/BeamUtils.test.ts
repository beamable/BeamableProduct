import { describe, expect, it, vi } from 'vitest';
import { saveToken } from '@/core/BeamUtils';
import type { TokenStorage } from '@/platform/types/TokenStorage';
import type { TokenResponse } from '@/__generated__/schemas';

describe('saveToken()', () => {
  describe('saveToken', () => {
    it('calls setAccessToken, setRefreshToken, and setExpiresIn on storage', async () => {
      const tokenResponse: TokenResponse = {
        expires_in: '123',
        token_type: 'Bearer',
        access_token: 'access-token',
        refresh_token: 'refresh-token',
        scopes: [],
      };
      const storage: TokenStorage = {
        setAccessToken: vi.fn().mockResolvedValue(undefined),
        setRefreshToken: vi.fn().mockResolvedValue(undefined),
        setExpiresIn: vi.fn().mockResolvedValue(undefined),
      } as unknown as TokenStorage;

      await saveToken(storage, tokenResponse);

      expect(storage.setAccessToken).toHaveBeenCalledWith('access-token');
      expect(storage.setRefreshToken).toHaveBeenCalledWith('refresh-token');
      expect(storage.setExpiresIn).toHaveBeenCalled();
    });
  });
});
