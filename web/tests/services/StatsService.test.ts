import { describe, expect, it, vi } from 'vitest';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import { PlayerService } from '@/services/PlayerService';
import { StatsService } from '@/services/StatsService';
import { BeamBase } from '@/core/BeamBase';

describe('StatsService', () => {
  describe('set', () => {
    it('calls statsPostClientByObjectId on the stats API and updates the stats if domainType is "client"', async () => {
      const mockStats: Record<string, string> = {
        level: '10',
        score: '1000',
      };

      vi.spyOn(apis, 'statsPostClientByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok' },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      const statsService = new StatsService({
        beam,
        getPlayer: () => playerService,
      });
      const objectId = `client.private.player.${playerService.id}`;
      await statsService.set({ accessType: 'private', stats: mockStats });

      expect(apis.statsPostClientByObjectId).toHaveBeenCalledWith(
        mockRequester,
        objectId,
        { set: mockStats },
        playerService.id,
      );
      expect(playerService.stats).toEqual(mockStats);
    });

    it('calls statsPostByObjectId on the stats API and updates the stats if domainType is "game"', async () => {
      const mockStats: Record<string, string> = {
        level: '10',
        score: '1000',
      };

      vi.spyOn(apis, 'statsPostByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { result: 'ok' },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      const statsService = new StatsService({
        beam,
        getPlayer: () => playerService,
      });
      const objectId = `game.private.player.${playerService.id}`;
      await statsService.set({
        domainType: 'game',
        accessType: 'private',
        stats: mockStats,
      });

      expect(apis.statsPostByObjectId).toHaveBeenCalledWith(
        mockRequester,
        objectId,
        { set: mockStats },
        playerService.id,
      );
      expect(playerService.stats).toEqual(mockStats);
    });
  });

  describe('get', () => {
    it('calls statsGetClientByObjectId on the stats API and returns all stats if domainType is "client"', async () => {
      const mockStats: Record<string, string> = {
        level: '10',
        score: '1000',
      };

      vi.spyOn(apis, 'statsGetClientByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { id: '123', stats: mockStats },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      const statsService = new StatsService({
        beam,
        getPlayer: () => playerService,
      });
      const objectId = `client.public.player.${playerService.id}`;
      const result = await statsService.get({ accessType: 'public' });

      expect(apis.statsGetClientByObjectId).toHaveBeenCalledWith(
        mockRequester,
        objectId,
        undefined,
        playerService.id,
      );
      expect(playerService.stats).toEqual(result);
    });

    it('calls statsGetByObjectId on the stats API and returns all stats if domainType is "game"', async () => {
      const mockStats: Record<string, string> = {
        level: '10',
        score: '1000',
      };

      vi.spyOn(apis, 'statsGetByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { id: '123', stats: mockStats },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      const statsService = new StatsService({
        beam,
        getPlayer: () => playerService,
      });
      const objectId = `game.public.player.${playerService.id}`;
      const result = await statsService.get({
        domainType: 'game',
        accessType: 'public',
      });

      expect(apis.statsGetByObjectId).toHaveBeenCalledWith(
        mockRequester,
        objectId,
        undefined,
        playerService.id,
      );
      expect(playerService.stats).toEqual(result);
    });

    it('calls statsGetClientByObjectId on the stats API and returns specific stats', async () => {
      const levelStats: Record<string, string> = {
        level: '10',
      };
      const scoreStats: Record<string, string> = {
        score: '1000',
      };

      vi.spyOn(apis, 'statsGetClientByObjectId').mockResolvedValue({
        status: 200,
        headers: {},
        body: { id: '123', stats: scoreStats },
      });

      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      playerService.stats = levelStats; // Initial stats
      const statsService = new StatsService({
        beam,
        getPlayer: () => playerService,
      });
      const objectId = `client.public.player.${playerService.id}`;
      const result = await statsService.get({
        accessType: 'public',
        stats: ['score'],
      });

      expect(apis.statsGetClientByObjectId).toHaveBeenCalledWith(
        mockRequester,
        objectId,
        'score',
        playerService.id,
      );
      expect(result).toEqual(scoreStats);
      expect(playerService.stats).toEqual({ ...levelStats, ...scoreStats });
    });
  });
});
