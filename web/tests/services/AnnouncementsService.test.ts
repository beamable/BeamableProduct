import { describe, expect, it, vi } from 'vitest';
import type { BeamApi } from '@/core/BeamApi';
import {
  AnnouncementQueryResponse,
  AnnouncementView,
} from '@/__generated__/schemas';
import { AnnouncementsService } from '@/services/AnnouncementsService';
import { PlayerService } from '@/services/PlayerService';

describe('AnnouncementsService', () => {
  describe('list', () => {
    it('calls getAnnouncementByObjectId on the announcements API and returns the announcement view', async () => {
      const mockBody: AnnouncementQueryResponse = {
        announcements: [
          {
            attachments: [],
            body: 'New game update available',
            channel: 'any',
            clientDataList: [],
            id: 'Test Announcement',
            isClaimed: false,
            isDeleted: false,
            isRead: false,
            summary: 'New game update available',
            title: 'Game Update',
          },
        ],
      };
      const mockBeamApi = {
        announcements: {
          getAnnouncementByObjectId: vi
            .fn()
            .mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      const announcementsService = new AnnouncementsService({
        api: mockBeamApi,
        player: playerService,
      });
      const result = await announcementsService.list();

      expect(
        mockBeamApi.announcements.getAnnouncementByObjectId,
      ).toHaveBeenCalled();
      expect(result).toEqual(mockBody.announcements);
    });
  });

  describe('markAsClaim', () => {
    it('calls postAnnouncementClaimByObjectId on the announcements API and returns the announcement `isClaimed` equals true', async () => {
      const announcementId = 'Test Announcement';
      const mockAnnouncement: AnnouncementView = {
        attachments: [],
        body: 'New game update available',
        channel: 'any',
        clientDataList: [],
        id: 'Test Announcement',
        isClaimed: false,
        isDeleted: false,
        isRead: false,
        summary: 'New game update available',
        title: 'Game Update',
      };
      const mockBeamApi = {
        announcements: {
          postAnnouncementClaimByObjectId: vi
            .fn()
            .mockResolvedValue({ body: { result: 'ok', data: {} } }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        api: mockBeamApi,
        player: playerService,
      });
      await announcementsService.claim({ id: announcementId });

      expect(
        mockBeamApi.announcements.postAnnouncementClaimByObjectId,
      ).toHaveBeenCalledWith(playerService.id, {
        announcements: [announcementId],
      });
      expect(playerService.announcements[0].isClaimed).toEqual(true);
    });
  });

  describe('markAsRead', () => {
    it('calls putAnnouncementReadByObjectId on the announcements API and returns the announcement `isRead` equals true', async () => {
      const announcementId = 'Test Announcement';
      const mockAnnouncement: AnnouncementView = {
        attachments: [],
        body: 'New game update available',
        channel: 'any',
        clientDataList: [],
        id: 'Test Announcement',
        isClaimed: false,
        isDeleted: false,
        isRead: false,
        summary: 'New game update available',
        title: 'Game Update',
      };
      const mockBeamApi = {
        announcements: {
          putAnnouncementReadByObjectId: vi
            .fn()
            .mockResolvedValue({ body: { result: 'ok', data: {} } }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        api: mockBeamApi,
        player: playerService,
      });
      await announcementsService.markAsRead({ id: announcementId });

      expect(
        mockBeamApi.announcements.putAnnouncementReadByObjectId,
      ).toHaveBeenCalledWith(playerService.id, {
        announcements: [announcementId],
      });
      expect(playerService.announcements[0].isRead).toEqual(true);
    });
  });

  describe('delete', () => {
    it('calls deleteAnnouncementByObjectId on the announcements API and deletes the announcement', async () => {
      const announcementId = 'Test Announcement';
      const mockAnnouncement: AnnouncementView = {
        attachments: [],
        body: 'New game update available',
        channel: 'any',
        clientDataList: [],
        id: 'Test Announcement',
        isClaimed: false,
        isDeleted: false,
        isRead: false,
        summary: 'New game update available',
        title: 'Game Update',
      };
      const mockBeamApi = {
        announcements: {
          deleteAnnouncementByObjectId: vi
            .fn()
            .mockResolvedValue({ body: { result: 'ok', data: {} } }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        api: mockBeamApi,
        player: playerService,
      });
      await announcementsService.delete({ id: announcementId });

      expect(
        mockBeamApi.announcements.deleteAnnouncementByObjectId,
      ).toHaveBeenCalledWith(playerService.id, {
        announcements: [announcementId],
      });
      expect(playerService.announcements.length).toEqual(0);
    });
  });
});
