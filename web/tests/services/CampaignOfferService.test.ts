import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CampaignOfferService } from '@/services/CampaignOfferService';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type {
  CampaignOfferEntitlementsResponse,
  CampaignOfferRedeemResponse,
  RedeemCampaignOfferRequest,
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
  return new CampaignOfferService({ beam, getPlayer: () => playerService });
}

/** The payload the service actually sent on the nth `redeem` call. */
function sentRedeemPayload(call: number): RedeemCampaignOfferRequest {
  return vi.mocked(apis.campaignOfferPostRedeem).mock.calls[call][1];
}

describe('CampaignOfferService', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('getEntitlements', () => {
    it('calls campaignOfferGetEntitlements with the federation id and the current player', async () => {
      const mockBody: CampaignOfferEntitlementsResponse = {
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

      vi.spyOn(apis, 'campaignOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const result = await makeService().getEntitlements('beamable_virtual_store');

      expect(apis.campaignOfferGetEntitlements).toHaveBeenCalledWith(
        mockRequester,
        'beamable_virtual_store',
        '0',
        '0',
      );
      expect(result).toEqual(mockBody.entitlements);
    });

    it('returns an empty list when the store reports no entitlements', async () => {
      vi.spyOn(apis, 'campaignOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: { playerId: '0' } as CampaignOfferEntitlementsResponse,
      });

      expect(await makeService().getEntitlements('beamable_virtual_store')).toEqual([]);
    });

    it('passes the whole offer through, so one call can render a store screen', async () => {
      // The entitlement embeds the offer precisely so a client does not fan out a GetOffer per row.
      // If any of these are dropped on the way through, a real-money bundle reaches the player as a
      // title and a price with no disclosure of what is in it — which is what `rewards` exists to fix.
      const mockBody: CampaignOfferEntitlementsResponse = {
        playerId: '0',
        entitlements: [
          {
            grantId: 'bsg_1',
            offerId: 'starter_store/bundle_10',
            state: 'granted',
            grantedAtUnixSeconds: '1787677536',
            expiresAtUnixSeconds: '0',
            offer: {
              offerId: 'starter_store/bundle_10',
              title: 'Starter Bundle',
              priceLabel: '350 Coins',
              rewards: [
                { type: 'currency', symbol: 'currency.gems', amount: '1200' },
                { type: 'item', symbol: 'items.blade', amount: '1', title: 'Blade' },
                // A store's own vocabulary must survive untouched — nothing may close the set.
                { type: 'steam_dlc', symbol: '480', amount: '1' },
              ],
              listings: [
                {
                  listingSymbol: 'bundle_10',
                  storeSymbol: 'starter_store',
                  price: {
                    type: 'currency',
                    symbol: 'currency.coins',
                    // The numbers, not just the label: a client needs them to tell whether the
                    // player can afford this.
                    amount: '350',
                    label: '350 Coins',
                  },
                },
              ],
            },
            available: false,
            unavailableReasons: [
              { code: 'stat-requirement', message: 'Reach level 10.' },
            ],
          },
        ],
      };

      vi.spyOn(apis, 'campaignOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: mockBody,
      });

      const [held] = await makeService().getEntitlements('beamable_virtual_store');

      expect(held.offer?.rewards).toHaveLength(3);
      expect(held.offer?.rewards?.[0]).toEqual({
        type: 'currency',
        symbol: 'currency.gems',
        amount: '1200',
      });
      expect(held.offer?.rewards?.[2].type).toBe('steam_dlc');
      expect(held.offer?.listings?.[0].price?.symbol).toBe('currency.coins');
      expect(held.offer?.listings?.[0].price?.amount).toBe('350');
      // `available` is not `state` — a granted grant can still be unactionable.
      expect(held.state).toBe('granted');
      expect(held.available).toBe(false);
      expect(held.unavailableReasons?.[0].code).toBe('stat-requirement');
    });

    it('tolerates a provider that sends no offer, rather than requiring one', async () => {
      // The contract allows a null offer, and a third-party store that cannot afford to embed it
      // is legitimate. The client falls back to the opaque offerId.
      vi.spyOn(apis, 'campaignOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: {
          playerId: '0',
          entitlements: [
            { grantId: 'bsg_1', offerId: 'opaque', state: 'granted' },
          ],
        },
      });

      const [held] = await makeService().getEntitlements('beamable_virtual_store');

      expect(held.offer).toBeUndefined();
      expect(held.offerId).toBe('opaque');
    });

    it('passes any federation id through — the default provider is not privileged', async () => {
      vi.spyOn(apis, 'campaignOfferGetEntitlements').mockResolvedValue({
        status: 200,
        headers: {},
        body: { playerId: '0', entitlements: [] },
      });

      await makeService().getEntitlements('my_web_shop');

      expect(apis.campaignOfferGetEntitlements).toHaveBeenCalledWith(
        mockRequester,
        'my_web_shop',
        '0',
        '0',
      );
    });
  });

  describe('redeem', () => {
    const ok: CampaignOfferRedeemResponse = { grantId: 'bsg_1', success: true };

    it('sends the federation id, the current player, the grant and a transaction id', async () => {
      vi.spyOn(apis, 'campaignOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      const result = await makeService().redeem('beamable_virtual_store', 'bsg_1');

      const payload = sentRedeemPayload(0);
      expect(payload.federationId).toBe('beamable_virtual_store');
      expect(payload.playerId).toBe('0');
      expect(payload.grantId).toBe('bsg_1');
      expect(payload.request?.transactionId).toBeTruthy();
      expect(payload.request?.params).toEqual({});
      expect(result).toEqual(ok);
    });

    it('reuses one transaction id per grant, so a retried claim is not read as a double-claim', async () => {
      vi.spyOn(apis, 'campaignOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      const service = makeService();
      await service.redeem('beamable_virtual_store', 'bsg_1');
      await service.redeem('beamable_virtual_store', 'bsg_1');

      expect(sentRedeemPayload(1).request?.transactionId).toBe(
        sentRedeemPayload(0).request?.transactionId,
      );
    });

    it('mints a distinct transaction id per grant and per store', async () => {
      vi.spyOn(apis, 'campaignOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      const service = makeService();
      await service.redeem('beamable_virtual_store', 'bsg_1');
      await service.redeem('beamable_virtual_store', 'bsg_2');
      await service.redeem('my_web_shop', 'bsg_1');

      const ids = [0, 1, 2].map((i) => sentRedeemPayload(i).request?.transactionId);
      expect(new Set(ids).size).toBe(3);
    });

    it('honours a caller-supplied transaction id and params', async () => {
      vi.spyOn(apis, 'campaignOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: ok,
      });

      await makeService().redeem('beamable_virtual_store', 'bsg_1', {
        transactionId: 'txn-mine',
        params: { source: 'push' },
      });

      expect(sentRedeemPayload(0).request).toEqual({
        transactionId: 'txn-mine',
        params: { source: 'push' },
      });
    });

    it('resolves a refused claim rather than throwing — success is on the body', async () => {
      const refused: CampaignOfferRedeemResponse = {
        grantId: 'bsg_1',
        success: false,
        status: 'unavailable',
        message: 'This offer expired before it was claimed.',
      };
      vi.spyOn(apis, 'campaignOfferPostRedeem').mockResolvedValue({
        status: 200,
        headers: {},
        body: refused,
      });

      await expect(
        makeService().redeem('beamable_virtual_store', 'bsg_1'),
      ).resolves.toEqual(refused);
    });
  });
});
