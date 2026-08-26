import { beforeEach, describe, expect, it, vi } from 'vitest';
import { StoreOfferService } from '@/services/StoreOfferService';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type {
  OfferEntitlementsResponse,
  OfferRedeemResponse,
  RedeemOfferRequest,
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
  return new StoreOfferService({ beam, getPlayer: () => playerService });
}

/** The payload the service actually sent on the nth `redeem` call. */
function sentRedeemPayload(call: number): RedeemOfferRequest {
  return vi.mocked(apis.storeOfferPostRedeem).mock.calls[call][1];
}

describe('StoreOfferService', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('getEntitlements', () => {
    it('calls storeOfferGetEntitlements with the federation id and the current player', async () => {
      const mockBody: OfferEntitlementsResponse = {
        playerId: '0',
        entitlements: [
          {
            grantId: 'bsg_1',
            offerId: 'offer_1',
            state: 'granted',
            grantedAtUnixSeconds: '1787677536',
            expiresAtUnixSeconds: '0',
          },
        ],
      };

      vi.spyOn(apis, 'storeOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const result = await makeService().getEntitlements('beamable_store');

      expect(apis.storeOfferGetEntitlements).toHaveBeenCalledWith(
        mockRequester,
        'beamable_store',
        '0',
        '0',
      );
      expect(result).toEqual(mockBody.entitlements);
    });

    it('returns an empty list when the store reports no entitlements', async () => {
      vi.spyOn(apis, 'storeOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: { playerId: '0' } as OfferEntitlementsResponse,
      });

      expect(await makeService().getEntitlements('beamable_store')).toEqual([]);
    });

    it('passes any federation id through — the default provider is not privileged', async () => {
      vi.spyOn(apis, 'storeOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: { playerId: '0', entitlements: [] },
      });

      await makeService().getEntitlements('my_web_shop');

      expect(apis.storeOfferGetEntitlements).toHaveBeenCalledWith(
        mockRequester,
        'my_web_shop',
        '0',
        '0',
      );
    });
  });

  describe('redeem', () => {
    const ok: OfferRedeemResponse = { grantId: 'bsg_1', success: true };

    it('sends the federation id, the current player, the grant and a transaction id', async () => {
      vi.spyOn(apis, 'storeOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      const result = await makeService().redeem('beamable_store', 'bsg_1');

      const payload = sentRedeemPayload(0);
      expect(payload.federationId).toBe('beamable_store');
      expect(payload.playerId).toBe('0');
      expect(payload.grantId).toBe('bsg_1');
      expect(payload.request?.transactionId).toBeTruthy();
      expect(payload.request?.params).toEqual({});
      expect(result).toEqual(ok);
    });

    it('reuses one transaction id per grant, so a retried claim is not read as a double-claim', async () => {
      vi.spyOn(apis, 'storeOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      const service = makeService();
      await service.redeem('beamable_store', 'bsg_1');
      await service.redeem('beamable_store', 'bsg_1');

      expect(sentRedeemPayload(1).request?.transactionId).toBe(
        sentRedeemPayload(0).request?.transactionId,
      );
    });

    it('mints a distinct transaction id per grant and per store', async () => {
      vi.spyOn(apis, 'storeOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      const service = makeService();
      await service.redeem('beamable_store', 'bsg_1');
      await service.redeem('beamable_store', 'bsg_2');
      await service.redeem('my_web_shop', 'bsg_1');

      const ids = [0, 1, 2].map((i) => sentRedeemPayload(i).request?.transactionId);
      expect(new Set(ids).size).toBe(3);
    });

    it('honours a caller-supplied transaction id and params', async () => {
      vi.spyOn(apis, 'storeOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      await makeService().redeem('beamable_store', 'bsg_1', {
        transactionId: 'txn-mine',
        params: { source: 'push' },
      });

      expect(sentRedeemPayload(0).request).toEqual({
        transactionId: 'txn-mine',
        params: { source: 'push' },
      });
    });

    it('resolves a refused claim rather than throwing — success is on the body', async () => {
      const refused: OfferRedeemResponse = {
        grantId: 'bsg_1',
        success: false,
        status: 'unavailable',
        message: 'This offer expired before it was claimed.',
      };
      vi.spyOn(apis, 'storeOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: refused,
      });

      await expect(
        makeService().redeem('beamable_store', 'bsg_1'),
      ).resolves.toEqual(refused);
    });
  });
});
