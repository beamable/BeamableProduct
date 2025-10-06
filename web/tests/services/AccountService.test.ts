import { describe, expect, it, vi } from 'vitest';
import { AccountService } from '@/services/AccountService';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type {
  AccountPlayerView,
  CommonResponse,
  EmailUpdateConfirmation,
  EmptyResponse,
  PasswordUpdateConfirmation,
} from '@/__generated__/schemas';
import { PlayerService } from '@/services/PlayerService';
import { BeamBase } from '@/core/BeamBase';
import { CredentialStatus, ThirdPartyAuthProvider } from '@/services/enums';
import { Beam } from '@/core/Beam';

describe('AccountService', () => {
  describe('current', () => {
    it('calls accountsGetMeBasic on the accounts API and returns the account player view', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: ['scope1'],
        thirdPartyAppAssociations: ['assoc1'],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };

      vi.spyOn(apis, 'accountsGetMeBasic').mockResolvedValue({
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

      const playerService = new PlayerService();
      const accountService = new AccountService({
        beam,
        getPlayer: () => playerService,
      });
      const result = await accountService.current();

      expect(apis.accountsGetMeBasic).toHaveBeenCalledWith(mockRequester, '0');
      expect(result).toEqual(mockBody);
      expect(playerService.account).toEqual(mockBody);
    });
  });

  describe('addCredentials', () => {
    it('calls accountsPostRegisterBasic on the accounts API and returns the account player view', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: [],
        thirdPartyAppAssociations: [],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };

      vi.spyOn(apis, 'accountsPostRegisterBasic').mockResolvedValue({
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
      const playerService = new PlayerService();
      const accountService = new AccountService({
        beam,
        getPlayer: () => playerService,
      });
      const result = await accountService.addCredentials({
        email: 'player@example.com',
        password: 'password123',
      });

      expect(apis.accountsPostRegisterBasic).toHaveBeenCalledWith(
        mockRequester,
        {
          email: 'player@example.com',
          password: 'password123',
        },
        '0',
      );
      expect(result).toEqual(mockBody);
      expect(playerService.account).toEqual(mockBody);
    });
  });

  describe('addThirdPartyCredentials', () => {
    it('calls accountsPutMeBasic on the accounts API and returns the account player view', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: [],
        thirdPartyAppAssociations: [],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };

      vi.spyOn(apis, 'accountsPutMeBasic').mockResolvedValue({
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
      const playerService = new PlayerService();
      const accountService = new AccountService({
        beam,
        getPlayer: () => playerService,
      });
      const result = await accountService.addThirdParty({
        provider: ThirdPartyAuthProvider.Google,
        token: 'token123',
      });

      expect(apis.accountsPutMeBasic).toHaveBeenCalledWith(
        mockRequester,
        {
          hasThirdPartyToken: true,
          thirdParty: ThirdPartyAuthProvider.Google,
          token: 'token123',
        },
        '0',
      );
      expect(result).toEqual(mockBody);
      expect(playerService.account).toEqual(mockBody);
    });
  });

  describe('removeExternalIdentity', () => {
    it('calls accountsDeleteExternalIdentityBasic on the accounts API and returns the updated account', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: [],
        thirdPartyAppAssociations: [],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };

      vi.spyOn(apis, 'accountsDeleteExternalIdentityBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {} as CommonResponse,
      });
      vi.spyOn(AccountService.prototype, 'current').mockResolvedValue(mockBody);

      const mockRequester = {} as HttpRequester;
      const player = new PlayerService();
      player.account = {
        id: '0',
        external: [
          {
            providerService: 'serviceName',
            providerNamespace: 'namespace',
            userId: 'user123',
          },
        ],
      } as unknown as AccountPlayerView;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => player,
      });
      const result = await accountService.removeExternalIdentity({
        providerService: 'serviceName',
        providerNamespace: 'namespace',
        externalUserId: 'user123',
      });

      expect(apis.accountsDeleteExternalIdentityBasic).toHaveBeenCalledWith(
        mockRequester,
        {
          provider_namespace: 'namespace',
          provider_service: 'serviceName',
          user_id: 'user123',
        },
        '0',
      );
      expect(result).toEqual(mockBody);
    });
  });

  describe('getExternalIdentityStatus', () => {
    it('returns NotAssigned if available is true', async () => {
      vi.spyOn(
        apis,
        'accountsGetAvailableExternalIdentityBasic',
      ).mockResolvedValue({
        status: 200,
        headers: {},
        body: { available: true },
      });

      const mockRequester = {} as HttpRequester;
      const player = new PlayerService();
      player.account = {
        id: '0',
        external: [
          {
            providerService: 'serviceName',
            providerNamespace: 'namespace',
            userId: 'user123',
          },
        ],
      } as unknown as AccountPlayerView;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => player,
      });
      const status = await accountService.getExternalIdentityStatus({
        providerService: 'serviceName',
        providerNamespace: 'namespace',
        externalUserId: 'user123',
      });

      expect(
        apis.accountsGetAvailableExternalIdentityBasic,
      ).toHaveBeenCalledWith(
        mockRequester,
        'serviceName',
        'user123',
        'namespace',
        '0',
      );
      expect(status).toBe(CredentialStatus.NotAssigned);
    });

    it('returns Assigned if available is false', async () => {
      vi.spyOn(
        apis,
        'accountsGetAvailableExternalIdentityBasic',
      ).mockResolvedValue({
        status: 200,
        headers: {},
        body: { available: false },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: 'user123' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({ beam });
      const status = await accountService.getExternalIdentityStatus({
        providerService: 'serviceName',
        providerNamespace: 'namespace',
        externalUserId: 'user123',
      });

      expect(status).toBe(CredentialStatus.Assigned);
    });

    it('returns Unknown if the API throws', async () => {
      vi.spyOn(
        apis,
        'accountsGetAvailableExternalIdentityBasic',
      ).mockRejectedValue(new Error('fail'));

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: 'user123' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({ beam });
      const status = await accountService.getExternalIdentityStatus({
        providerService: 'serviceName',
        providerNamespace: 'namespace',
        externalUserId: 'user123',
      });

      expect(status).toBe(CredentialStatus.Unknown);
    });
  });
  describe('getEmailCredentialStatus', () => {
    it('returns NotAssigned if available is true', async () => {
      vi.spyOn(apis, 'accountsGetAvailableBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: { available: true },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: '0' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const status = await accountService.getEmailCredentialStatus({
        email: 'player@example.com',
      });

      expect(apis.accountsGetAvailableBasic).toHaveBeenCalledWith(
        mockRequester,
        'player@example.com',
        '0',
      );
      expect(status).toBe(CredentialStatus.NotAssigned);
    });

    it('returns Assigned if available is false', async () => {
      vi.spyOn(apis, 'accountsGetAvailableBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: { available: false },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({ beam });
      const status = await accountService.getEmailCredentialStatus({
        email: 'player@example.com',
      });

      expect(status).toBe(CredentialStatus.Assigned);
    });

    it('returns Unknown if the API throws', async () => {
      vi.spyOn(apis, 'accountsGetAvailableBasic').mockRejectedValue(
        new Error('fail'),
      );

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({ beam });
      const status = await accountService.getEmailCredentialStatus({
        email: 'player@example.com',
      });

      expect(status).toBe(CredentialStatus.Unknown);
    });
  });

  describe('getThirdPartyStatus', () => {
    it('returns NotAssigned if available is true', async () => {
      vi.spyOn(apis, 'accountsGetAvailableThirdPartyBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: { available: true },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: '0' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const status = await accountService.getThirdPartyStatus({
        provider: ThirdPartyAuthProvider.Facebook,
        token: 'token123',
      });

      expect(apis.accountsGetAvailableThirdPartyBasic).toHaveBeenCalledWith(
        mockRequester,
        ThirdPartyAuthProvider.Facebook,
        'token123',
        '0',
      );
      expect(status).toBe(CredentialStatus.NotAssigned);
    });

    it('returns Assigned if available is false', async () => {
      vi.spyOn(apis, 'accountsGetAvailableThirdPartyBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: { available: false },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({ beam });
      const status = await accountService.getThirdPartyStatus({
        provider: ThirdPartyAuthProvider.Facebook,
        token: 'token123',
      });

      expect(status).toBe(CredentialStatus.Assigned);
    });

    it('returns Unknown if the API throws', async () => {
      vi.spyOn(apis, 'accountsGetAvailableThirdPartyBasic').mockRejectedValue(
        new Error('fail'),
      );

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({ beam });
      const status = await accountService.getThirdPartyStatus({
        provider: ThirdPartyAuthProvider.Facebook,
        token: 'token123',
      });

      expect(status).toBe(CredentialStatus.Unknown);
    });
  });

  describe('addExternalIdentity', () => {
    it('calls accountsPostExternalIdentityBasic and returns current account on token flow', async () => {
      const mockCurrentAccountResponse = { id: 'player-id' } as any;
      vi.spyOn(apis, 'accountsPostExternalIdentityBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok' },
      });
      vi.spyOn(AccountService.prototype, 'current').mockResolvedValue(
        mockCurrentAccountResponse,
      );

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player: { id: '0' },
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const result = await accountService.addExternalIdentity({
        externalToken: 'ext-token',
        providerService: 'svc',
        providerNamespace: 'ns',
      });

      expect(apis.accountsPostExternalIdentityBasic).toHaveBeenCalledWith(
        mockRequester,
        {
          external_token: 'ext-token',
          provider_service: 'svc',
          provider_namespace: 'ns',
          challenge_solution: undefined,
        },
        '0',
      );
      expect(result).toEqual(mockCurrentAccountResponse);
    });

    it('handles challenge flow by calling challengeHandler and returning current after resolution', async () => {
      const mockCurrentAccountResponse = { id: 'player-id' } as any;
      vi.spyOn(apis, 'accountsPostExternalIdentityBasic')
        .mockResolvedValueOnce({
          status: 200,
          headers: {},
          body: { result: 'challenge', challenge_token: 'ctoken' },
        })
        .mockResolvedValueOnce({
          status: 200,
          headers: {},
          body: { result: 'ok' },
        });
      vi.spyOn(AccountService.prototype, 'current').mockResolvedValue(
        mockCurrentAccountResponse,
      );

      const challengeHandler = vi.fn().mockResolvedValue('solution');
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
        player: { id: '0' },
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const result = await accountService.addExternalIdentity({
        externalToken: 'ext-token',
        providerService: 'svc',
        providerNamespace: 'ns',
        challengeHandler,
      });

      expect(challengeHandler).toHaveBeenCalledWith('ctoken');
      expect(apis.accountsPostExternalIdentityBasic).toHaveBeenCalledWith(
        mockRequester,
        {
          external_token: 'ext-token',
          provider_service: 'svc',
          provider_namespace: 'ns',
          challenge_solution: {
            challenge_token: 'ctoken',
            solution: 'solution',
          },
        },
        '0',
      );
      expect(result).toEqual(mockCurrentAccountResponse);
    });
  });

  describe('removeThirdPartyCredentials', () => {
    it('calls accountsDeleteMeThirdPartyBasic on the accounts API and returns the account player view', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: [],
        thirdPartyAppAssociations: [],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };

      vi.spyOn(apis, 'accountsDeleteMeThirdPartyBasic').mockResolvedValue({
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
      const playerService = new PlayerService();
      const accountService = new AccountService({
        beam,
        getPlayer: () => playerService,
      });
      const result = await accountService.removeThirdParty({
        provider: ThirdPartyAuthProvider.Google,
        token: 'token123',
      });

      expect(apis.accountsDeleteMeThirdPartyBasic).toHaveBeenCalledWith(
        mockRequester,
        { thirdParty: ThirdPartyAuthProvider.Google, token: 'token123' },
        '0',
      );
      expect(result).toEqual(mockBody);
      expect(playerService.account).toEqual(mockBody);
    });
  });

  describe('initiateEmailUpdate', () => {
    it('calls accountsPostEmailUpdateInitBasic on the accounts API', async () => {
      vi.spyOn(apis, 'accountsPostEmailUpdateInitBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {} as EmptyResponse,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: '0' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const result = await accountService.initiateEmailUpdate({
        newEmail: 'new@example.com',
      });

      expect(apis.accountsPostEmailUpdateInitBasic).toHaveBeenCalledWith(
        mockRequester,
        { newEmail: 'new@example.com' },
        '0',
      );
      expect(result).toBeUndefined();
    });
  });

  describe('confirmEmailUpdate', () => {
    it('calls accountsPostEmailUpdateConfirmBasic on the accounts API', async () => {
      const params: EmailUpdateConfirmation = {
        code: 'code123',
        password: 'password123',
      };
      vi.spyOn(apis, 'accountsPostEmailUpdateConfirmBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {} as EmptyResponse,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: '0' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const result = await accountService.confirmEmailUpdate(params);

      expect(apis.accountsPostEmailUpdateConfirmBasic).toHaveBeenCalledWith(
        mockRequester,
        params,
        '0',
      );
      expect(result).toBeUndefined();
    });
  });

  describe('initiatePasswordUpdate', () => {
    it('calls accountsPostPasswordUpdateInitBasic on the accounts API', async () => {
      vi.spyOn(apis, 'accountsPostPasswordUpdateInitBasic').mockResolvedValue({
        status: 200,
        headers: {},
        body: {} as EmptyResponse,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: '0' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const result = await accountService.initiatePasswordUpdate({
        email: 'player@example.com',
      });

      expect(apis.accountsPostPasswordUpdateInitBasic).toHaveBeenCalledWith(
        mockRequester,
        { email: 'player@example.com', codeType: 'PIN' },
        '0',
      );
      expect(result).toBeUndefined();
    });
  });

  describe('confirmPasswordUpdate', () => {
    it('calls accountsPostPasswordUpdateConfirmBasic on the accounts API', async () => {
      const params: PasswordUpdateConfirmation = {
        code: 'code123',
        newPassword: 'newPass123',
      };
      vi.spyOn(
        apis,
        'accountsPostPasswordUpdateConfirmBasic',
      ).mockResolvedValue({
        status: 200,
        headers: {},
        body: {} as EmptyResponse,
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        player: { id: '0' },
        requester: mockRequester,
      } as unknown as BeamBase;
      const accountService = new AccountService({
        beam,
        getPlayer: () => (beam as Beam).player,
      });
      const result = await accountService.confirmPasswordUpdate(params);

      expect(apis.accountsPostPasswordUpdateConfirmBasic).toHaveBeenCalledWith(
        mockRequester,
        params,
        '0',
      );
      expect(result).toBeUndefined();
    });
  });
});
