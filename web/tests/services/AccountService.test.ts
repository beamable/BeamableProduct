import { describe, expect, it, vi } from 'vitest';
import { AccountService } from '@/services/AccountService';
import type { BeamApi } from '@/core/BeamApi';
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
      const mockBeamApi = {
        accounts: {
          getAccountsMe: vi.fn().mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      const accountService = new AccountService({
        api: mockBeamApi,
        player: playerService,
      });
      const result = await accountService.current();

      expect(mockBeamApi.accounts.getAccountsMe).toHaveBeenCalledWith(
        undefined,
      );
      expect(result).toEqual(mockBody);
    });
  });
});
