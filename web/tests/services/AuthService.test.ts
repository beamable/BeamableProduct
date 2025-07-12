import { describe, expect, it, vi } from 'vitest';
import { AuthService } from '@/services/AuthService';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
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
      vi.spyOn(apis, 'postAuthToken').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });
      const mockRequester = {} as HttpRequester;
      const authService = new AuthService({ requester: mockRequester });
      const result = await authService.signInAsGuest();

      expect(apis.postAuthToken).toHaveBeenCalledWith(mockRequester, payload);
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
      vi.spyOn(apis, 'postAuthToken').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });
      const mockRequester = {} as HttpRequester;
      const authService = new AuthService({ requester: mockRequester });
      const result = await authService.refreshAuthToken({ refreshToken });

      expect(apis.postAuthToken).toHaveBeenCalledWith(mockRequester, payload);
      expect(result).toEqual(mockBody);
    });
  });
});
