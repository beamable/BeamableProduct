import { describe, expect, it } from 'vitest';
import { PlayerService } from '@/services/PlayerService';
import { ThirdPartyAuthProvider } from '@/services/enums';
import type {
  AccountPlayerView,
  AnnouncementView,
} from '@/__generated__/schemas';

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

  it('allows setting and getting the announcement', () => {
    const playerService = new PlayerService();
    const customAnnouncements: AnnouncementView[] = [
      {
        attachments: [],
        body: 'New game update available',
        channel: 'any',
        clientDataList: [],
        id: 'Test Announcement',
        isClaimed: false,
        isDeleted: false,
        isRead: true,
        summary: 'New game update available',
        title: 'Game Update',
      },
    ];
    playerService.announcements = customAnnouncements;
    expect(playerService.announcements).toEqual(customAnnouncements);
  });
  describe('hasThirdPartyAssociation', () => {
    it('returns true when association exists', () => {
      const playerService = new PlayerService();
      playerService.account = {
        ...playerService.account,
        thirdPartyAppAssociations: ['google'],
      };
      expect(
        playerService.hasThirdPartyAssociation(ThirdPartyAuthProvider.Google),
      ).toBe(true);
    });

    it('returns false when association does not exist', () => {
      const playerService = new PlayerService();
      playerService.account = {
        ...playerService.account,
        thirdPartyAppAssociations: [],
      };
      expect(
        playerService.hasThirdPartyAssociation(ThirdPartyAuthProvider.Facebook),
      ).toBe(false);
    });
  });
});
