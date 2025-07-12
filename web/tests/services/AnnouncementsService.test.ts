import { describe, expect, it, vi } from 'vitest';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
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
      vi.spyOn(apis, 'getAnnouncementByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });
      const mockRequester = {} as HttpRequester;
      const playerService = new PlayerService();
      const announcementsService = new AnnouncementsService({
        requester: mockRequester,
        player: playerService,
      });
      const result = await announcementsService.list();

      expect(apis.getAnnouncementByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        false,
        playerService.id,
      );
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
      vi.spyOn(apis, 'postAnnouncementClaimByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok', data: {} },
      });
      const mockRequester = {} as HttpRequester;
      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        requester: mockRequester,
        player: playerService,
      });
      await announcementsService.claim({ id: announcementId });

      expect(apis.postAnnouncementClaimByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        { announcements: [announcementId] },
        playerService.id,
      );
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
      vi.spyOn(apis, 'putAnnouncementReadByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok', data: {} },
      });
      const mockRequester = {} as HttpRequester;
      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        requester: mockRequester,
        player: playerService,
      });
      await announcementsService.markAsRead({ id: announcementId });

      expect(apis.putAnnouncementReadByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        { announcements: [announcementId] },
        playerService.id,
      );
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
      vi.spyOn(apis, 'deleteAnnouncementByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok', data: {} },
      });
      const mockRequester = {} as HttpRequester;
      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        requester: mockRequester,
        player: playerService,
      });
      await announcementsService.delete({ id: announcementId });

      expect(apis.deleteAnnouncementByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        { announcements: [announcementId] },
        playerService.id,
      );
      expect(playerService.announcements.length).toEqual(0);
    });
  });
});
