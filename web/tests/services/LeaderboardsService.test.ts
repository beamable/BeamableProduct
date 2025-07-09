import { describe, expect, it, vi } from 'vitest';
import type { BeamApi } from '@/core/BeamApi';
import { BeamError } from '@/constants/Errors';
import {
  LeaderboardsService,
  type GetLeaderboardParams,
  type SetLeaderboardScoreParams,
} from '@/services/LeaderboardsService';
import { PlayerService } from '@/services/PlayerService';
import type { LeaderBoardViewResponse } from '@/__generated__/schemas/LeaderBoardViewResponse';
import type { LeaderBoardView } from '@/__generated__/schemas';

describe('LeaderboardsService', () => {
  describe('get', () => {
    it('calls getLeaderboardViewByObjectId on the leaderboards API and updates player leaderboards and params', async () => {
      const mockView: LeaderBoardView = {
        boardSize: 100n,
        lbId: 'testLb',
        rankings: [],
      };
      const mockBody: LeaderBoardViewResponse = { lb: mockView, result: 'ok' };
      const mockBeamApi = {
        leaderboards: {
          getLeaderboardViewByObjectId: vi
            .fn()
            .mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      const service = new LeaderboardsService({
        api: mockBeamApi,
        player: playerService,
      });
      const params: GetLeaderboardParams = {
        id: 'lb1',
        from: 1,
        max: 10,
        focus: 'player',
        outlier: 'other',
        includeFriends: true,
        includeGuilds: true,
      };
      const result = await service.get(params);

      expect(
        mockBeamApi.leaderboards.getLeaderboardViewByObjectId,
      ).toHaveBeenCalledWith(
        'lb1',
        'player',
        true,
        1,
        true,
        10,
        'other',
        playerService.id,
      );
      expect(result).toEqual(mockView);
      expect(playerService.leaderboards['lb1']).toEqual(mockView);
      expect(playerService.leaderboardsParams['lb1']).toEqual(params);
    });

    it('returns leaderboard view without updating player when no player is present', async () => {
      const mockView: LeaderBoardView = {
        boardSize: '50',
        lbId: 'noPlayerLb',
        rankings: [],
      };
      const mockBody: LeaderBoardViewResponse = { lb: mockView, result: 'ok' };
      const mockBeamApi = {
        leaderboards: {
          getLeaderboardViewByObjectId: vi
            .fn()
            .mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const service = new LeaderboardsService({
        api: mockBeamApi,
        userId: '123',
      });
      const params: GetLeaderboardParams = { id: 'lb2' };
      const result = await service.get(params);

      expect(result).toEqual(mockView);
    });
  });

  describe('getFriendRanks', () => {
    it('calls getLeaderboardFriendsByObjectId on the leaderboards API and returns the leaderboard view', async () => {
      const mockView: LeaderBoardView = {
        boardSize: 5n,
        lbId: 'friendsLb',
        rankings: [],
      };
      const mockBody: LeaderBoardViewResponse = { lb: mockView, result: 'ok' };
      const mockBeamApi = {
        leaderboards: {
          getLeaderboardFriendsByObjectId: vi
            .fn()
            .mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      const service = new LeaderboardsService({
        api: mockBeamApi,
        player: playerService,
      });
      const result = await service.getFriendRanks({ id: 'lbFriends' });

      expect(
        mockBeamApi.leaderboards.getLeaderboardFriendsByObjectId,
      ).toHaveBeenCalledWith('lbFriends', playerService.id);
      expect(result).toEqual(mockView);
    });
  });

  describe('getRanks', () => {
    it('calls getLeaderboardRanksByObjectId on the leaderboards API and returns the leaderboard view', async () => {
      const mockView: LeaderBoardView = {
        boardSize: 3n,
        lbId: 'ranksLb',
        rankings: [],
      };
      const mockBody: LeaderBoardViewResponse = { lb: mockView, result: 'ok' };
      const mockBeamApi = {
        leaderboards: {
          getLeaderboardRanksByObjectId: vi
            .fn()
            .mockResolvedValue({ body: mockBody }),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      const service = new LeaderboardsService({
        api: mockBeamApi,
        player: playerService,
      });
      const result = await service.getRanks({
        id: 'lbRanks',
        playerIds: ['a', 'b'],
      });

      expect(
        mockBeamApi.leaderboards.getLeaderboardRanksByObjectId,
      ).toHaveBeenCalledWith('lbRanks', 'a,b', playerService.id);
      expect(result).toEqual(mockView);
    });
  });

  describe('setScore', () => {
    it('calls putLeaderboardEntryByObjectId on the leaderboards API and then calls get with previous params', async () => {
      const mockBeamApi = {
        leaderboards: {
          putLeaderboardEntryByObjectId: vi.fn().mockResolvedValue({}),
        },
      } as unknown as BeamApi;

      const playerService = new PlayerService();
      // ensure previous params exist so setScore will pass them to get
      const getParams: GetLeaderboardParams = { id: 'lb', from: 1, max: 10 };
      const params: SetLeaderboardScoreParams = {
        id: 'lb',
        score: 42,
        increment: true,
        stats: { key: 'value' },
      };
      playerService.leaderboardsParams = { [getParams.id]: getParams };
      const service = new LeaderboardsService({
        api: mockBeamApi,
        player: playerService,
      });
      service.get = vi
        .fn()
        .mockResolvedValue({ boardSize: 0n, lbId: '', rankings: [] });

      await service.setScore(params);

      expect(
        mockBeamApi.leaderboards.putLeaderboardEntryByObjectId,
      ).toHaveBeenCalledWith(
        'lb',
        {
          id: playerService.id,
          score: 42,
          increment: true,
          stats: { key: 'value' },
        },
        playerService.id,
      );
      expect(service.get).toHaveBeenCalledWith(getParams);
    });
  });

  describe('freeze', () => {
    it('throws BeamError when called on client (player present)', async () => {
      const mockBeamApi = {
        leaderboards: { putLeaderboardFreezeByObjectId: vi.fn() },
      } as unknown as BeamApi;
      const playerService = new PlayerService();
      const service = new LeaderboardsService({
        api: mockBeamApi,
        player: playerService,
      });

      await expect(service.freeze({ id: 'lbFreeze' })).rejects.toThrow(
        BeamError,
      );
    });

    it('calls putLeaderboardFreezeByObjectId on the leaderboards API when no player is present', async () => {
      const mockBeamApi = {
        leaderboards: {
          putLeaderboardFreezeByObjectId: vi.fn().mockResolvedValue({}),
        },
      } as unknown as BeamApi;
      const service = new LeaderboardsService({
        api: mockBeamApi,
        userId: 'adminId',
      });

      await service.freeze({ id: 'lbFreeze' });

      expect(
        mockBeamApi.leaderboards.putLeaderboardFreezeByObjectId,
      ).toHaveBeenCalledWith('lbFreeze', 'adminId');
    });
  });
});
