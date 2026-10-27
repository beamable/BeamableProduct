import { beforeEach, describe, expect, it, vi } from 'vitest';
import { OfferService } from '@/services/OfferService';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import {
  OfferGrantState,
  type PlayerOffersPage,
  type RedeemOfferResult,
} from '@/__generated__/schemas';
import { PlayerService } from '@/services/PlayerService';
import { BeamBase } from '@/core/BeamBase';

const mockRequester = {} as HttpRequester;

function makeService() {
  const beam = {
    cid: 'cid',
    pid: 'pid',
    requester: mockRequester,
  } as unknown as BeamBase;

  const playerService = new PlayerService();
  return new OfferService({ beam, getPlayer: () => playerService });
}

function ok<T>(body: T) {
  return { status: 200, headers: {}, body };
}

describe('OfferService', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('getOffers', () => {
    it('asks for the current player, every provider and the default states', async () => {
      const page: PlayerOffersPage = {
        offers: [
          {
            grantId: 'g1',
            federationId: 'beamable_virtual_store',
            offerId: 'store/listing',
            state: OfferGrantState.Granted,
            available: true,
            offer: { offerId: 'store/listing', title: 'Gems', cost: [], rewards: [] },
          },
        ],
        next: 'cursor-1',
      };
      vi.spyOn(apis, 'offersGet').mockResolvedValue(ok(page));

      const result = await makeService().getOffers();

      expect(apis.offersGet).toHaveBeenCalledWith(
        mockRequester,
        undefined,
        undefined,
        '0',
        undefined,
        '0',
      );
      expect(result).toEqual(page);
    });

    it('passes the cursor, the provider filter and the states through', async () => {
      vi.spyOn(apis, 'offersGet').mockResolvedValue(ok<PlayerOffersPage>({ offers: [] }));

      await makeService().getOffers({
        cursor: 'cursor-1',
        federationId: 'my_web_shop',
        states: [OfferGrantState.Redeemed, OfferGrantState.Expired],
      });

      expect(apis.offersGet).toHaveBeenCalledWith(
        mockRequester,
        'cursor-1',
        'my_web_shop',
        '0',
        [OfferGrantState.Redeemed, OfferGrantState.Expired],
        '0',
      );
    });
  });

  describe('redeem', () => {
    it('sends the current player, the grant and the params — no provider, no transaction id', async () => {
      const result: RedeemOfferResult = { grantId: 'g1', success: true, status: 'redeemed' };
      vi.spyOn(apis, 'offersPostRedeem').mockResolvedValue(ok(result));

      const response = await makeService().redeem('g1', { choice: 'gold' });

      expect(apis.offersPostRedeem).toHaveBeenCalledWith(
        mockRequester,
        { playerId: '0', grantId: 'g1', params: { choice: 'gold' } },
        '0',
      );
      expect(response).toEqual(result);
    });

    it('resolves a refused or pending redeem rather than throwing — success is on the body', async () => {
      const pending: RedeemOfferResult = {
        grantId: 'g1',
        success: false,
        status: 'pending',
        state: OfferGrantState.Claiming,
      };
      vi.spyOn(apis, 'offersPostRedeem').mockResolvedValue(ok(pending));

      await expect(makeService().redeem('g1')).resolves.toEqual(pending);
      expect(vi.mocked(apis.offersPostRedeem).mock.calls[0][1].params).toEqual({});
    });
  });
});
