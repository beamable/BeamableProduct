import { describe, expect, it, vi } from 'vitest';
import { AuthService } from '@/services/AuthService';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type {
  AccountPlayerView,
  TokenRequestWrapper,
  TokenResponse,
} from '@/__generated__/schemas';
import { BeamBase } from '@/core/BeamBase';
import { ThirdPartyAuthProvider, CredentialStatus } from '@/services/enums';
import { AccountService } from '@/services/AccountService';
import { BeamError } from '@/constants/Errors';

describe('AuthService', () => {
  describe('loginAsGuest', () => {
    it('calls authPostTokenBasic on the auth API and returns the token response body', async () => {
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

      vi.spyOn(apis, 'authPostTokenBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const authService = new AuthService({ beam });
      const result = await authService.loginAsGuest();

      expect(apis.authPostTokenBasic).toHaveBeenCalledWith(
        mockRequester,
        payload,
      );
      expect(result).toEqual(mockBody);
    });
  });

  describe('loginWithEmail', () => {
    it('calls authPostTokenBasic on the auth API and returns the token response body', async () => {
      const payload: TokenRequestWrapper = {
        grant_type: 'password',
        username: 'player@example.com',
        password: 'password123',
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

      vi.spyOn(apis, 'authPostTokenBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const authService = new AuthService({ beam });
      const result = await authService.loginWithEmail({
        email: 'player@example.com',
        password: 'password123',
      });

      expect(apis.authPostTokenBasic).toHaveBeenCalledWith(
        mockRequester,
        payload,
      );
      expect(result).toEqual(mockBody);
    });
  });

  describe('refreshAuthToken', () => {
    it('calls authPostTokenBasic on the auth API with refresh_token payload and returns the token response body', async () => {
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
      vi.spyOn(apis, 'authPostTokenBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const authService = new AuthService({ beam });
      const result = await authService.refreshAuthToken({ refreshToken });

      expect(apis.authPostTokenBasic).toHaveBeenCalledWith(
        mockRequester,
        payload,
      );
      expect(result).toEqual(mockBody);
    });
  });

  describe('loginWithThirdParty', () => {
    it('calls authPostTokenBasic on the auth API and returns the token response body', async () => {
      const params = {
        provider: ThirdPartyAuthProvider.Google,
        token: 'test-token',
      };
      const payload: TokenRequestWrapper = {
        grant_type: 'third_party',
        third_party: params.provider,
        token: params.token,
        context: { device: 'Desktop', platform: 'Node' },
      };
      const mockBody: TokenResponse = {
        expires_in: '3600',
        token_type: 'Bearer',
        access_token: 'test-access-token',
        refresh_token: 'test-refresh-token',
        scopes: ['scope1', 'scope2'],
      };

      vi.spyOn(apis, 'authPostTokenBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const authService = new AuthService({ beam });
      const result = await authService.loginWithThirdParty(params);

      expect(apis.authPostTokenBasic).toHaveBeenCalledWith(
        mockRequester,
        payload,
      );
      expect(result).toEqual(mockBody);
    });
  });

  describe('loginWithExternalIdentity', () => {
    it('calls authPostTokenBasic and returns tokenResponse on token flow', async () => {
      const params = {
        externalToken: 'ext-token',
        providerService: 'svc',
        providerNamespace: 'ns',
      };
      const mockBody: TokenResponse = {
        expires_in: '3600',
        token_type: 'Bearer',
        access_token: 'access',
        refresh_token: 'refresh',
        scopes: ['s1'],
      };

      vi.spyOn(apis, 'authPostTokenBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const authService = new AuthService({ beam });
      const result = await authService.loginWithExternalIdentity(params);

      expect(apis.authPostTokenBasic).toHaveBeenCalledWith(mockRequester, {
        grant_type: 'external',
        external_token: 'ext-token',
        provider_service: 'svc',
        provider_namespace: 'ns',
        context: { device: 'Desktop', platform: 'Node' },
      });
      expect(result).toEqual(mockBody);
    });

    it('handles challenge flow by calling challengeHandler and returning tokenResponse', async () => {
      const challengeHandler = vi.fn().mockResolvedValue('solution');
      const initialResponse = {
        status: 200,
        headers: {},
        body: {
          token_type: 'challenge',
          challenge_token: 'ctoken',
          expires_in: '100',
        },
      };
      const finalBody: TokenResponse = {
        expires_in: '3600',
        token_type: 'Bearer',
        access_token: 'access',
        refresh_token: 'refresh',
        scopes: ['s1'],
      };
      const nextResponse = { status: 200, headers: {}, body: finalBody };

      vi.spyOn(apis, 'authPostTokenBasic')
        .mockResolvedValueOnce(initialResponse)
        .mockResolvedValueOnce(nextResponse);

      const params = {
        externalToken: 'ext-token',
        providerService: 'svc',
        providerNamespace: 'ns',
        challengeHandler,
      };
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const authService = new AuthService({ beam });
      const result = await authService.loginWithExternalIdentity(params);

      expect(challengeHandler).toHaveBeenCalledWith('ctoken');
      expect(apis.authPostTokenBasic).toHaveBeenCalledWith(mockRequester, {
        grant_type: 'external',
        external_token: 'ext-token',
        provider_service: 'svc',
        provider_namespace: 'ns',
        context: { device: 'Desktop', platform: 'Node' },
      });
      expect(apis.authPostTokenBasic).toHaveBeenCalledWith(mockRequester, {
        grant_type: 'external',
        external_token: 'ext-token',
        provider_service: 'svc',
        provider_namespace: 'ns',
        challenge_solution: { challenge_token: 'ctoken', solution: 'solution' },
        context: { device: 'Desktop', platform: 'Node' },
      });
      expect(result).toEqual(finalBody);
    });
  });

  describe('handleThirdPartyAuthFlow', () => {
    const params = {
      provider: ThirdPartyAuthProvider.Google,
      token: 'test-token',
    };

    it('throws BeamError if no player is available', async () => {
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const authService = new AuthService({ beam });

      await expect(
        authService.handleThirdPartyAuthFlow(params),
      ).rejects.toBeInstanceOf(BeamError);
    });

    it('throws BeamError on unknown credential status', async () => {
      const player = { hasThirdPartyAssociation: vi.fn() };
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player,
      } as unknown as BeamBase;
      const authService = new AuthService({
        beam,
        getPlayer: () => player as any,
      });

      vi.spyOn(
        AccountService.prototype,
        'getThirdPartyStatus',
      ).mockResolvedValue(CredentialStatus.Unknown);

      await expect(
        authService.handleThirdPartyAuthFlow(params),
      ).rejects.toThrow(
        'Unknown credential status: invalid token or unconfigured third-party provider',
      );
    });

    it('logs in when credential status is Assigned', async () => {
      const player = { hasThirdPartyAssociation: vi.fn() };
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player,
      } as unknown as BeamBase;
      const authService = new AuthService({
        beam,
        getPlayer: () => player as any,
      });

      vi.spyOn(
        AccountService.prototype,
        'getThirdPartyStatus',
      ).mockResolvedValue(CredentialStatus.Assigned);
      const loginSpy = vi
        .spyOn(authService, 'loginWithThirdParty')
        .mockResolvedValue({} as TokenResponse);

      await authService.handleThirdPartyAuthFlow(params);

      expect(loginSpy).toHaveBeenCalledWith(params);
    });

    it('links to current user when credential status is NotAssigned and user not linked', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: ['scope1'],
        thirdPartyAppAssociations: ['assoc1'],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };
      const player = {
        hasThirdPartyAssociation: vi.fn().mockReturnValue(false),
      };
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player,
      } as unknown as BeamBase;
      const authService = new AuthService({
        beam,
        getPlayer: () => player as any,
      });

      vi.spyOn(
        AccountService.prototype,
        'getThirdPartyStatus',
      ).mockResolvedValue(CredentialStatus.NotAssigned);
      const addCredSpy = vi
        .spyOn(AccountService.prototype, 'addThirdParty')
        .mockResolvedValue(mockBody);

      await authService.handleThirdPartyAuthFlow(params);

      expect(addCredSpy).toHaveBeenCalledWith(params);
    });

    it('creates guest and links when credential status is NotAssigned and user already linked', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: ['scope1'],
        thirdPartyAppAssociations: ['assoc1'],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };
      const player = {
        hasThirdPartyAssociation: vi.fn().mockReturnValue(true),
      };
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player,
      } as unknown as BeamBase;
      const authService = new AuthService({
        beam,
        getPlayer: () => player as any,
      });

      vi.spyOn(
        AccountService.prototype,
        'getThirdPartyStatus',
      ).mockResolvedValue(CredentialStatus.NotAssigned);
      const guestToken = {} as TokenResponse;
      const guestSpy = vi
        .spyOn(authService, 'loginAsGuest')
        .mockResolvedValue(guestToken);
      const addCredSpy = vi
        .spyOn(AccountService.prototype, 'addThirdParty')
        .mockResolvedValue(mockBody);

      await authService.handleThirdPartyAuthFlow(params);

      expect(guestSpy).toHaveBeenCalled();
      expect(addCredSpy).toHaveBeenCalledWith(params);
    });
  });
});
