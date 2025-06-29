import { describe, expect, it, vi } from 'vitest';
import type { BeamApi } from '@/core/BeamApi';
import { PlayerService } from '@/services/PlayerService';
import { StatsService } from '@/services/StatsService';

describe('StatsService', () => {
  describe('set', () => {
    it('calls postStatClientByObjectId on the stats API and updates the stats', async () => {
      const mockStats: Record<string, string> = {
        level: '10',
        score: '1000',
      };
      const mockBeamApi = {
        stats: {
          postStatClientByObjectId: vi
            .fn()
            .mockResolvedValue({ body: { result: 'ok' } }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      const statsService = new StatsService({
        api: mockBeamApi,
        player: playerService,
      });
      const objectId = `client.private.player.${playerService.id}`;
      await statsService.set({ accessType: 'private', stats: mockStats });

      expect(mockBeamApi.stats.postStatClientByObjectId).toHaveBeenCalledWith(
        objectId,
        { set: mockStats },
      );
      expect(playerService.stats).toEqual(mockStats);
    });
  });

  describe('get', () => {
    it('calls getStatClientByObjectId on the stats API and returns all stats', async () => {
      const mockStats: Record<string, string> = {
        level: '10',
        score: '1000',
      };
      const mockBeamApi = {
        stats: {
          getStatClientByObjectId: vi
            .fn()
            .mockResolvedValue({ body: { stats: mockStats } }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      const statsService = new StatsService({
        api: mockBeamApi,
        player: playerService,
      });
      const objectId = `client.public.player.${playerService.id}`;
      const result = await statsService.get({ accessType: 'public' });

      expect(mockBeamApi.stats.getStatClientByObjectId).toHaveBeenCalledWith(
        objectId,
        undefined,
      );
      expect(playerService.stats).toEqual(result);
    });

    it('calls getStatClientByObjectId on the stats API and returns specific stats', async () => {
      const levelStats: Record<string, string> = {
        level: '10',
      };
      const scoreStats: Record<string, string> = {
        score: '1000',
      };
      const mockBeamApi = {
        stats: {
          getStatClientByObjectId: vi
            .fn()
            .mockResolvedValue({ body: { stats: scoreStats } }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      playerService.stats = levelStats; // Initial stats
      const statsService = new StatsService({
        api: mockBeamApi,
        player: playerService,
      });
      const objectId = `client.public.player.${playerService.id}`;
      const result = await statsService.get({
        accessType: 'public',
        stats: ['score'],
      });

      expect(mockBeamApi.stats.getStatClientByObjectId).toHaveBeenCalledWith(
        objectId,
        'score',
      );
      expect(result).toEqual(scoreStats);
      expect(playerService.stats).toEqual({ ...levelStats, ...scoreStats });
    });
  });
});
