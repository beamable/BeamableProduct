import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { Beam } from '@/core/Beam';
import * as BeamUtils from '@/core/BeamUtils';
import { MockBeamWebSocket } from '../network/websocket/MockBeamWebSocket';
import * as ApiModule from '@/__generated__/apis';

describe('Beam', () => {
  describe('ready', () => {
    const dummyTokenResponse = {
      access_token: 'access',
      refresh_token: 'refresh',
      expires_in: 123,
    };
    const dummyPlayer = {
      id: 'playerId',
      deviceIds: [],
      scopes: [],
      thirdPartyAppAssociations: [],
      email: '',
      external: [],
      language: '',
    };

    let beam: Beam;
    let storage: any;
    let saveTokenSpy: any;

    beforeEach(() => {
      storage = {
        getAccessToken: vi.fn(),
        getRefreshToken: vi.fn(),
        getExpiresIn: vi.fn(),
        setAccessToken: vi.fn(),
        setRefreshToken: vi.fn(),
        setExpiresIn: vi.fn(),
        removeAccessToken: vi.fn(),
        removeRefreshToken: vi.fn(),
        removeExpiresIn: vi.fn(),
        isExpired: false,
        clear: vi.fn(),
        dispose: vi.fn(),
      };
      beam = new Beam({
        environment: 'Dev',
        cid: 'cid',
        pid: 'pid',
        tokenStorage: storage,
      });
      // Use mock WebSocket for realtime connection
      (beam as any).ws = new MockBeamWebSocket();
      // Stub realms client defaults for setupRealtimeConnection
      vi.spyOn(ApiModule, 'getRealmsClientDefaults').mockResolvedValue({
        body: {
          websocketConfig: { provider: 'beamable', uri: 'wss://test.com' },
        },
      } as any);
      // Default refresh token for setupRealtimeConnection
      storage.getRefreshToken.mockResolvedValue('refresh-token');
      saveTokenSpy = vi.spyOn(BeamUtils, 'saveToken').mockResolvedValue();
      vi.spyOn(beam.auth, 'signInAsGuest').mockResolvedValue(
        dummyTokenResponse as any,
      );
      vi.spyOn(beam.auth, 'refreshAuthToken').mockResolvedValue(
        dummyTokenResponse as any,
      );
    });

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('signs in as guest when no access token exists', async () => {
      storage.getAccessToken.mockResolvedValue(null);
      beam.auth.signInAsGuest = vi
        .fn()
        .mockResolvedValue(dummyTokenResponse as any);
      beam.account.current = vi.fn().mockResolvedValue(dummyPlayer as any);

      await beam.ready();

      expect(beam.auth.signInAsGuest).toHaveBeenCalled();
      expect(saveTokenSpy).toHaveBeenCalledWith(storage, dummyTokenResponse);
      expect(beam.player.account).toEqual(dummyPlayer);
    });

    it('refreshes token when access token expired and refresh-token exists', async () => {
      storage.getAccessToken.mockResolvedValue('access');
      storage.isExpired = true;
      storage.getRefreshToken.mockResolvedValue('refreshToken');
      beam.auth.refreshAuthToken = vi
        .fn()
        .mockResolvedValue(dummyTokenResponse as any);
      beam.account.current = vi.fn().mockResolvedValue(dummyPlayer as any);

      await beam.ready();

      expect(beam.auth.refreshAuthToken).toHaveBeenCalledWith({
        refreshToken: 'refreshToken',
      });
      expect(saveTokenSpy).toHaveBeenCalledWith(storage, dummyTokenResponse);
      expect(beam.player.account).toEqual(dummyPlayer);
    });

    it('falls back to guest sign-in when refresh token does not exist', async () => {
      storage.getAccessToken.mockResolvedValue('access');
      storage.isExpired = true;
      storage.getRefreshToken.mockResolvedValueOnce(null);
      beam.auth.signInAsGuest = vi
        .fn()
        .mockResolvedValue(dummyTokenResponse as any);
      beam.account.current = vi.fn().mockResolvedValue(dummyPlayer as any);

      await beam.ready();

      expect(beam.auth.signInAsGuest).toHaveBeenCalled();
      expect(saveTokenSpy).toHaveBeenCalledWith(storage, dummyTokenResponse);
      expect(beam.player.account).toEqual(dummyPlayer);
    });

    it('only sets player account when token exists and not expired', async () => {
      storage.getAccessToken.mockResolvedValue('access');
      storage.isExpired = false;
      beam.account.current = vi.fn().mockResolvedValue(dummyPlayer as any);
      await beam.ready();
      expect(beam.auth.signInAsGuest).not.toHaveBeenCalled();
      expect(saveTokenSpy).not.toHaveBeenCalled();
      expect(beam.player.account).toEqual(dummyPlayer);
    });

    it('isReadyPromise resolves to true when setup is complete', async () => {
      storage.getAccessToken.mockResolvedValue('access');
      storage.isExpired = false;
      beam.account.current = vi.fn().mockResolvedValue(dummyPlayer as any);

      await beam.ready();

      expect(beam.isReady).toBe(true);
    });

    it('isReadyPromise resolves to false when setup fails', async () => {
      storage.getAccessToken.mockRejectedValue(new Error('Setup failed'));
      await expect(beam.ready()).rejects.toThrow('Setup failed');
      expect(beam.isReady).toBe(false);
    });
  });
});
