import { describe, expect, it, vi } from 'vitest';
import { AccountService } from '@/services/AccountService';
import type { BeamApi } from '@/core/BeamApi';
import type { AccountPlayerView } from '@/__generated__/schemas';

describe('AccountService', () => {
  describe('getCurrentPlayer', () => {
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

      const accountService = new AccountService(mockBeamApi);
      const result = await accountService.getCurrentPlayer();

      expect(mockBeamApi.accounts.getAccountsMe).toHaveBeenCalled();
      expect(result).toEqual(mockBody);
    });
  });
});
