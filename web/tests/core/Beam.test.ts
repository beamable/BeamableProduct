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
});
