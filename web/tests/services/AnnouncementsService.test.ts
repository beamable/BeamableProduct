import { describe, expect, it, vi } from 'vitest';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type {
  AnnouncementQueryResponse,
  AnnouncementView,
} from '@/__generated__/schemas';
import { AnnouncementsService } from '@/services/AnnouncementsService';
import { PlayerService } from '@/services/PlayerService';
import { BeamBase } from '@/core/BeamBase';

describe('AnnouncementsService', () => {
  describe('list', () => {
    it('calls announcementsGetByObjectId on the announcements API and returns the announcement view', async () => {
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

      vi.spyOn(apis, 'announcementsGetByObjectId').mockResolvedValue({
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
      const announcementsService = new AnnouncementsService({
        beam,
        getPlayer: () => playerService,
      });
      const result = await announcementsService.list();

      expect(apis.announcementsGetByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        false,
        playerService.id,
      );
      expect(result).toEqual(mockBody.announcements);
    });
  });

  describe('markAsClaim', () => {
    it('calls announcementsPostClaimByObjectId on the announcements API and returns the announcement `isClaimed` equals true', async () => {
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

      vi.spyOn(apis, 'announcementsPostClaimByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok', data: {} },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        beam,
        getPlayer: () => playerService,
      });
      await announcementsService.claim({ id: announcementId });

      expect(apis.announcementsPostClaimByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        { announcements: [announcementId] },
        playerService.id,
      );
      expect(playerService.announcements[0].isClaimed).toEqual(true);
    });
  });

  describe('markAsRead', () => {
    it('calls announcementsPutReadByObjectId on the announcements API and returns the announcement `isRead` equals true', async () => {
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

      vi.spyOn(apis, 'announcementsPutReadByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok', data: {} },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        beam,
        getPlayer: () => playerService,
      });
      await announcementsService.markAsRead({ id: announcementId });

      expect(apis.announcementsPutReadByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        { announcements: [announcementId] },
        playerService.id,
      );
      expect(playerService.announcements[0].isRead).toEqual(true);
    });
  });

  describe('delete', () => {
    it('calls announcementsDeleteByObjectId on the announcements API and deletes the announcement', async () => {
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

      vi.spyOn(apis, 'announcementsDeleteByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok', data: {} },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      playerService.announcements = [mockAnnouncement];
      const announcementsService = new AnnouncementsService({
        beam,
        getPlayer: () => playerService,
      });
      await announcementsService.delete({ id: announcementId });

      expect(apis.announcementsDeleteByObjectId).toHaveBeenCalledWith(
        mockRequester,
        playerService.id,
        { announcements: [announcementId] },
        playerService.id,
      );
      expect(playerService.announcements.length).toEqual(0);
    });
  });
});
