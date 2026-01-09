import { describe, expect, it, vi } from 'vitest';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import { BeamError } from '@/constants/Errors';
import {
  LeaderboardsService,
  type GetLeaderboardParams,
  type SetLeaderboardScoreParams,
} from '@/services/LeaderboardsService';
import { PlayerService } from '@/services/PlayerService';
import type {
  LeaderboardAssignmentInfo,
  LeaderBoardView,
  LeaderBoardViewResponse,
} from '@/__generated__/schemas';
import { BeamBase } from '@/core/BeamBase';

describe('LeaderboardsService', () => {
  describe('get', () => {
    it('calls leaderboardsGetViewByObjectId on the leaderboards API and updates player leaderboards and params', async () => {
      const mockView: LeaderBoardView = {
        boardSize: 100n,
        lbId: 'testLb',
        rankings: [],
      };
      const mockBody: LeaderBoardViewResponse = { lb: mockView, result: 'ok' };
      vi.spyOn(apis, 'leaderboardsGetViewByObjectId').mockResolvedValue({
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
      const service = new LeaderboardsService({
        beam,
        getPlayer: () => playerService,
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

      expect(apis.leaderboardsGetViewByObjectId).toHaveBeenCalledWith(
        mockRequester,
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
      vi.spyOn(apis, 'leaderboardsGetViewByObjectId').mockResolvedValue({
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
      const service = new LeaderboardsService({
        beam,
      });
      service.userId = '123';
      const params: GetLeaderboardParams = { id: 'lb2' };
      const result = await service.get(params);

      expect(result).toEqual(mockView);
    });
  });

  describe('getAssignedBoard', () => {
    it('throws BeamError when assignment is not found', async () => {
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const service = new LeaderboardsService({
        beam,
      });
      service.userId = '123';
      vi.spyOn(service as any, 'getAssignment').mockResolvedValue(
        undefined as any,
      );
      await expect(
        service.getAssignedBoard({ id: 'assignmentId' }),
      ).rejects.toThrow(BeamError);
    });

    it('calls getAssignment and get with the returned leaderboardId', async () => {
      const assignment: LeaderboardAssignmentInfo = {
        leaderboardId: 'lbNew',
      } as LeaderboardAssignmentInfo;
      const mockView: LeaderBoardView = {
        boardSize: 1n,
        lbId: 'lbNew',
        rankings: [],
      };
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const service = new LeaderboardsService({
        beam,
        getPlayer: () => new PlayerService(),
      });
      const params: GetLeaderboardParams = {
        id: 'assignId',
        from: 1,
        max: 2,
        focus: 'f',
        outlier: 'o',
      };
      vi.spyOn(service as any, 'getAssignment').mockResolvedValue(assignment);
      vi.spyOn(service, 'get').mockResolvedValue(mockView);
      const result = await service.getAssignedBoard(params);
      expect((service as any).getAssignment).toHaveBeenCalledWith(
        'assignId',
        true,
      );
      expect(service.get).toHaveBeenCalledWith({ ...params, id: 'lbNew' });
      expect(result).toEqual(mockView);
    });
  });

  describe('getFriendRanks', () => {
    it('calls leaderboardsGetFriendsByObjectId on the leaderboards API and returns the leaderboard view', async () => {
      const mockView: LeaderBoardView = {
        boardSize: 5n,
        lbId: 'friendsLb',
        rankings: [],
      };
      const mockBody: LeaderBoardViewResponse = { lb: mockView, result: 'ok' };
      vi.spyOn(apis, 'leaderboardsGetFriendsByObjectId').mockResolvedValue({
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
      const service = new LeaderboardsService({
        beam,
        getPlayer: () => playerService,
      });
      const result = await service.getFriendRanks({ id: 'lbFriends' });

      expect(apis.leaderboardsGetFriendsByObjectId).toHaveBeenCalledWith(
        mockRequester,
        'lbFriends',
        playerService.id,
      );
      expect(result).toEqual(mockView);
    });
  });

  describe('getRanks', () => {
    it('calls leaderboardsGetRanksByObjectId on the leaderboards API and returns the leaderboard view', async () => {
      const mockView: LeaderBoardView = {
        boardSize: 3n,
        lbId: 'ranksLb',
        rankings: [],
      };
      const mockBody: LeaderBoardViewResponse = { lb: mockView, result: 'ok' };
      vi.spyOn(apis, 'leaderboardsGetRanksByObjectId').mockResolvedValue({
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
      const service = new LeaderboardsService({
        beam,
        getPlayer: () => playerService,
      });
      const result = await service.getRanks({
        id: 'lbRanks',
        playerIds: ['a', 'b'],
      });

      expect(apis.leaderboardsGetRanksByObjectId).toHaveBeenCalledWith(
        mockRequester,
        'lbRanks',
        'a,b',
        playerService.id,
      );
      expect(result).toEqual(mockView);
    });
  });

  describe('setScore', () => {
    it('calls leaderboardsPutEntryByObjectId on the leaderboards API and then calls get with previous params', async () => {
      vi.spyOn(apis, 'leaderboardsPutEntryByObjectId').mockResolvedValue({
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
        beam,
        getPlayer: () => playerService,
      });
      // mock assignment resolution
      vi.spyOn(service as any, 'getAssignment').mockResolvedValue({
        leaderboardId: getParams.id,
      } as LeaderboardAssignmentInfo);
      service.get = vi
        .fn()
        .mockResolvedValue({ boardSize: 0n, lbId: '', rankings: [] });

      await service.setScore(params);

      expect(apis.leaderboardsPutEntryByObjectId).toHaveBeenCalledWith(
        mockRequester,
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

    it('throws BeamError when assignment is not found', async () => {
      const mockRequester = {} as HttpRequester;
      const beam = {
        cid: 'cid',
        pid: 'pid',
        requester: mockRequester,
      } as unknown as BeamBase;
      const playerService = new PlayerService();
      const service = new LeaderboardsService({
        beam,
        getPlayer: () => playerService,
      });
      vi.spyOn(service as any, 'getAssignment').mockResolvedValue(
        undefined as any,
      );
      await expect(
        service.setScore({ id: 'lb', score: 1 } as SetLeaderboardScoreParams),
      ).rejects.toThrow(BeamError);
    });

    it('does not call get when no player is present', async () => {
      vi.spyOn(apis, 'leaderboardsPutEntryByObjectId').mockResolvedValue({
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
      const service = new LeaderboardsService({
        beam,
      });
      service.userId = '123';
      vi.spyOn(service as any, 'getAssignment').mockResolvedValue({
        leaderboardId: 'lb',
      } as LeaderboardAssignmentInfo);
      service.get = vi.fn();
      await service.setScore({
        id: 'lb',
        score: 2,
      } as SetLeaderboardScoreParams);
      expect(apis.leaderboardsPutEntryByObjectId).toHaveBeenCalledWith(
        mockRequester,
        'lb',
        {
          id: (service as any).accountId,
          score: 2,
          increment: false,
          stats: undefined,
        },
        (service as any).accountId,
      );
      expect(service.get).not.toHaveBeenCalled();
    });
  });

  describe('freeze', () => {
    it('throws BeamError when called on client (player present)', async () => {
      vi.spyOn(apis, 'leaderboardsPutFreezeByObjectId').mockResolvedValue({
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
      const service = new LeaderboardsService({
        beam,
        getPlayer: () => playerService,
      });

      await expect(service.freeze({ id: 'lbFreeze' })).rejects.toThrow(
        BeamError,
      );
    });

    it('calls leaderboardsPutFreezeByObjectId on the leaderboards API when no player is present', async () => {
      vi.spyOn(apis, 'leaderboardsPutFreezeByObjectId').mockResolvedValue({
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
      const service = new LeaderboardsService({
        beam,
      });
      service.userId = 'adminId';

      await service.freeze({ id: 'lbFreeze' });

      expect(apis.leaderboardsPutFreezeByObjectId).toHaveBeenCalledWith(
        mockRequester,
        'lbFreeze',
        'adminId',
      );
    });
  });
});
