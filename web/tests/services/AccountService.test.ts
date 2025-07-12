import { describe, expect, it, vi } from 'vitest';
import { AccountService } from '@/services/AccountService';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { AccountPlayerView } from '@/__generated__/schemas';
import { PlayerService } from '@/services/PlayerService';

describe('AccountService', () => {
  describe('current', () => {
    it('calls getAccountsMe on the accounts API and returns the account player view', async () => {
      const mockBody: AccountPlayerView = {
        deviceIds: ['device1'],
        id: 'player-id',
        scopes: ['scope1'],
        thirdPartyAppAssociations: ['assoc1'],
        email: 'player@example.com',
        external: [],
        language: 'en',
      };
      vi.spyOn(apis, 'getAccountsMe').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });
      const mockRequester = {} as HttpRequester;
      const playerService = new PlayerService();
      const accountService = new AccountService({
        requester: mockRequester,
        player: playerService,
      });
      const result = await accountService.current();

      expect(apis.getAccountsMe).toHaveBeenCalledWith(mockRequester, undefined);
      expect(result).toEqual(mockBody);
    });
  });
});
