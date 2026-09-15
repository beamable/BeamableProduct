import { describe, expect, it, vi, beforeEach, afterAll } from 'vitest';
import { MockBeamWebSocket } from '../network/websocket/MockBeamWebSocket';
import { ContentService } from '@/services/ContentService';

// Use mock WebSocket for realtime connection
vi.mock('@/network/websocket/BeamWebSocket', () => ({
  BeamWebSocket: MockBeamWebSocket,
}));

import { Beam } from '@/core/Beam';
import * as BeamUtils from '@/core/BeamUtils';
import { AuthService } from '@/services/AuthService';
import { AccountService } from '@/services/AccountService';
import { BeamConfig } from '@/configs/BeamConfig';
import { TokenStorage } from '@/platform/types/TokenStorage';

describe('Beam', () => {
  describe('init', () => {
    const mockSaveToken = vi.spyOn(BeamUtils, 'saveToken');
    const mockLoginAsGuest = vi.spyOn(AuthService.prototype, 'loginAsGuest');
    const mockRefreshAuthToken = vi.spyOn(
      AuthService.prototype,
      'refreshAuthToken',
    );
    const mockAccountCurrent = vi.spyOn(AccountService.prototype, 'current');
    const mockSyncContentManifests = vi.spyOn(
      ContentService.prototype,
      'syncContentManifests',
    );
    let config: BeamConfig;

    beforeEach(() => {
      mockSaveToken.mockClear();
      mockLoginAsGuest.mockClear();
      mockRefreshAuthToken.mockClear();
      mockAccountCurrent.mockClear();
      mockSyncContentManifests.mockClear();
    });

    afterAll(() => {
      vi.restoreAllMocks();
    });

    it('should initialize Beam and login as guest if no token exists', async () => {
      config = {
        cid: 'test-cid',
        pid: 'test-pid',
        tokenStorage: {
          getTokenData: async () => ({
            accessToken: null,
            refreshToken: 'refresh-token',
            expiresIn: null,
          }),
          isExpired: false,
          setTokenData: async () => {},
          clear: async () => {},
        } as unknown as TokenStorage,
      };

      mockLoginAsGuest.mockResolvedValue({
        access_token: 'guest-token',
        refresh_token: 'guest-refresh',
        expires_in: '3600',
        token_type: 'Bearer',
        scopes: [],
      });
      mockAccountCurrent.mockResolvedValue({
        id: 'player-id',
        deviceIds: [],
        scopes: [],
        thirdPartyAppAssociations: [],
      });
      mockSyncContentManifests.mockResolvedValue();

      const beam = await Beam.init(config);

      expect(beam).toBeInstanceOf(Beam);
      expect(mockLoginAsGuest).toHaveBeenCalled();
      expect(mockSaveToken).toHaveBeenCalledWith(
        config.tokenStorage,
        expect.objectContaining({ access_token: 'guest-token' }),
      );
      expect(mockAccountCurrent).toHaveBeenCalled();
      expect(mockSyncContentManifests).toHaveBeenCalled();
    });

    it('should initialize Beam and refresh token if access token is expired', async () => {
      config = {
        cid: 'test-cid',
        pid: 'test-pid',
        tokenStorage: {
          getTokenData: async () => ({
            accessToken: 'access-token',
            refreshToken: 'refresh-token',
            expiresIn: null,
          }),
          isExpired: true,
          setTokenData: async () => {},
          clear: async () => {},
        } as unknown as TokenStorage,
      };

      mockRefreshAuthToken.mockResolvedValue({
        access_token: 'new-access-token',
        refresh_token: 'new-refresh-token',
        expires_in: '3600',
        token_type: 'Bearer',
        scopes: [],
      });
      mockAccountCurrent.mockResolvedValue({
        id: 'player-id',
        deviceIds: [],
        scopes: [],
        thirdPartyAppAssociations: [],
      });
      mockSyncContentManifests.mockResolvedValue();

      const beam = await Beam.init(config);

      expect(beam).toBeInstanceOf(Beam);
      expect(mockRefreshAuthToken).toHaveBeenCalledWith({
        refreshToken: 'refresh-token',
      });
      expect(mockSaveToken).toHaveBeenCalledWith(
        config.tokenStorage,
        expect.objectContaining({ access_token: 'new-access-token' }),
      );
      expect(mockAccountCurrent).toHaveBeenCalled();
      expect(mockSyncContentManifests).toHaveBeenCalled();
    });

    it('should initialize Beam and use existing token if not expired', async () => {
      config = {
        cid: 'test-cid',
        pid: 'test-pid',
        tokenStorage: {
          getTokenData: async () => ({
            accessToken: 'access-token',
            refreshToken: 'refresh-token',
            expiresIn: null,
          }),
          isExpired: false,
          setTokenData: async () => {},
          clear: async () => {},
        } as unknown as TokenStorage,
      };

      mockAccountCurrent.mockResolvedValue({
        id: 'player-id',
        deviceIds: [],
        scopes: [],
        thirdPartyAppAssociations: [],
      });
      mockSyncContentManifests.mockResolvedValue();

      const beam = await Beam.init(config);

      expect(beam).toBeInstanceOf(Beam);
      expect(mockLoginAsGuest).not.toHaveBeenCalled();
      expect(mockAccountCurrent).toHaveBeenCalled();
      expect(mockSyncContentManifests).toHaveBeenCalled();
    });
  });

  // --- realtime subscriptions ---

  describe('on / off', () => {
    /** The exact wire shape the gateway sends: camelCase fields, enums as strings. */
    const change = {
      segmentId: 'whales',
      kind: 'Enter',
      cause: 'Rule',
      ruleVersion: 3,
      timestamp: '2026-09-09T12:00:00.000000Z',
    };

    async function initBeam() {
      // Spied per call rather than once for the describe: the init suite above ends with
      // vi.restoreAllMocks(), which would otherwise strip these and let the tests hit the network.
      const mockLoginAsGuest = vi.spyOn(AuthService.prototype, 'loginAsGuest');
      const mockAccountCurrent = vi.spyOn(AccountService.prototype, 'current');
      const mockSyncContentManifests = vi.spyOn(
        ContentService.prototype,
        'syncContentManifests',
      );
      mockLoginAsGuest.mockResolvedValue({
        access_token: 'guest-token',
        refresh_token: 'guest-refresh',
        expires_in: '3600',
        token_type: 'Bearer',
        scopes: [],
      });
      mockAccountCurrent.mockResolvedValue({
        id: 'player-id',
        deviceIds: [],
        scopes: [],
        thirdPartyAppAssociations: [],
      } as any);
      mockSyncContentManifests.mockResolvedValue();

      const beam = await Beam.init({
        cid: 'test-cid',
        pid: 'test-pid',
        tokenStorage: {
          getTokenData: async () => ({
            accessToken: 'token',
            refreshToken: 'refresh-token',
            expiresIn: 3600,
          }),
          isExpired: false,
          setTokenData: async () => {},
          clear: async () => {},
        } as unknown as TokenStorage,
      });
      return { beam, ws: (beam as any).ws as MockBeamWebSocket };
    }

    afterAll(() => {
      vi.restoreAllMocks();
    });

    it('delivers a notification payload straight to the handler', async () => {
      const { beam, ws } = await initBeam();
      const seen: unknown[] = [];
      beam.on('segments.transition', (data) => seen.push(data));

      ws.emit('segments.transition', change);

      // The payload IS the data — the double JSON.parse is done for the caller, and nothing was
      // re-fetched on their behalf.
      expect(seen).toEqual([change]);
    });

    it('does not refresh a service for a notification context', async () => {
      const { beam, ws } = await initBeam();
      const refresh = vi.spyOn(ContentService.prototype, 'refresh');
      beam.on('segments.transition', () => {});

      ws.emit('segments.transition', change);

      // Asserted with a spy because final state cannot tell "handed the payload over" from
      // "re-fetched a service and handed that over".
      expect(refresh).not.toHaveBeenCalled();
      refresh.mockRestore();
    });

    it('ignores frames for a different context', async () => {
      const { beam, ws } = await initBeam();
      const seen: unknown[] = [];
      beam.on('segments.transition', (data) => seen.push(data));

      ws.emit('content.refresh', { scopes: [], delay: 0, data: {} });

      expect(seen).toEqual([]);
    });

    it('survives a malformed payload without killing other handlers', async () => {
      const { beam, ws } = await initBeam();
      const seen: unknown[] = [];
      beam.on('segments.transition', () => {
        throw new Error('handler blew up');
      });
      beam.on('segments.transition', (data) => seen.push(data));

      expect(() => ws.emit('segments.transition', change)).not.toThrow();
      expect(seen).toEqual([change]);
    });

    it('off(context, handler) removes only that handler', async () => {
      const { beam, ws } = await initBeam();
      const a: unknown[] = [];
      const b: unknown[] = [];
      const handlerA = (d: unknown) => a.push(d);
      const handlerB = (d: unknown) => b.push(d);
      beam.on('segments.transition', handlerA);
      beam.on('segments.transition', handlerB);

      beam.off('segments.transition', handlerA);
      ws.emit('segments.transition', change);

      expect(a).toEqual([]);
      expect(b).toEqual([change]);
    });

    it('off(context) removes every handler and detaches from the socket', async () => {
      const { beam, ws } = await initBeam();
      const seen: unknown[] = [];
      beam.on('segments.transition', (d) => seen.push(d));
      beam.on('segments.transition', (d) => seen.push(d));
      const before = ws.listenerCount;

      beam.off('segments.transition');
      ws.emit('segments.transition', change);

      expect(seen).toEqual([]);
      // Detached, not merely forgotten: a listener left on the socket would keep re-parsing every
      // frame for a subscription nobody holds.
      expect(ws.listenerCount).toBe(before - 2);
    });

    it('rejects an unknown context and lists the valid ones', async () => {
      const { beam } = await initBeam();

      expect(() => beam.on('nope.nothing' as any, () => {})).toThrow(
        /not supported/,
      );
      // The notification contexts have to appear in that list too, or the error tells an author
      // that a context which does work is unavailable.
      expect(() => beam.on('nope.nothing' as any, () => {})).toThrow(
        /segments\.transition/,
      );
    });

    it('still supports a refreshable context', async () => {
      // The existing behaviour, unchanged: the handler receives what refresh() returned, not the
      // notification payload.
      const { beam, ws } = await initBeam();
      const refreshed = { checksums: [{ id: 'global', checksum: 'abc' }] };
      const refresh = vi
        .spyOn(ContentService.prototype, 'refresh')
        .mockResolvedValue(refreshed as any);
      const seen: unknown[] = [];
      beam.on('content.refresh', (data) => seen.push(data));

      ws.emit('content.refresh', { scopes: [], data: { manifest: 'global' } });
      await vi.waitFor(() => expect(seen.length).toBe(1));

      expect(refresh).toHaveBeenCalled();
      expect(seen).toEqual([refreshed]);
      refresh.mockRestore();
    });
  });
});
