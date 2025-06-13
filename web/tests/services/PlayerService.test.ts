import { describe, expect, it } from 'vitest';
import { PlayerService } from '@/services/PlayerService';
import type { AccountPlayerView } from '@/__generated__/schemas';

describe('PlayerService', () => {
  it('initializes account with default values', () => {
    const playerService = new PlayerService();
    expect(playerService.account).toEqual({
      deviceIds: [],
      id: '0',
      scopes: [],
      thirdPartyAppAssociations: [],
      email: '',
      external: [],
      language: '',
    });
  });

  it('allows setting and getting the account', () => {
    const playerService = new PlayerService();
    const customAccount: AccountPlayerView = {
      deviceIds: ['d1'],
      id: '123',
      scopes: ['s1'],
      thirdPartyAppAssociations: ['a1'],
      email: 'user@test.com',
      external: [],
      language: 'fr',
    };
    playerService.account = customAccount;
    expect(playerService.account).toEqual(customAccount);
  });
});
