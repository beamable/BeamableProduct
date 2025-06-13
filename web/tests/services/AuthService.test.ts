import { describe, expect, it, vi } from 'vitest';
import { AuthService } from '@/services/AuthService';
import type { BeamApi } from '@/core/BeamApi';
import type {
  TokenRequestWrapper,
  TokenResponse,
} from '@/__generated__/schemas';

describe('AuthService', () => {
  describe('signInAsGuest', () => {
    it('calls postAuthToken on the auth API and returns the token response body', async () => {
      const payload: TokenRequestWrapper = {
        grant_type: 'guest',
        context: {
          device: 'Desktop',
          platform: 'Node',
        },
      };
      const mockBody: TokenResponse = {
        expires_in: '3600',
        token_type: 'Bearer',
        access_token: 'test-access-token',
        refresh_token: 'test-refresh-token',
        scopes: ['scope1', 'scope2'],
      };
      const mockBeamApi = {
        auth: {
          postAuthToken: vi.fn().mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const authService = new AuthService(mockBeamApi);
      const result = await authService.signInAsGuest();

      expect(mockBeamApi.auth.postAuthToken).toHaveBeenCalledWith(payload);
      expect(result).toEqual(mockBody);
    });
  });

  describe('refreshAuthToken', () => {
    it('calls postAuthToken on the auth API with refresh_token payload and returns the token response body', async () => {
      const refreshToken = 'existing-refresh-token';
      const payload: TokenRequestWrapper = {
        grant_type: 'refresh_token',
        refresh_token: refreshToken,
      };
      const mockBody: TokenResponse = {
        expires_in: '7200',
        token_type: 'Bearer',
        access_token: 'new-access-token',
        refresh_token: 'new-refresh-token',
        scopes: ['scopeA'],
      };
      const mockBeamApi = {
        auth: {
          postAuthToken: vi.fn().mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const authService = new AuthService(mockBeamApi);
      const result = await authService.refreshAuthToken(refreshToken);

      expect(mockBeamApi.auth.postAuthToken).toHaveBeenCalledWith(payload);
      expect(result).toEqual(mockBody);
    });
  });
});
