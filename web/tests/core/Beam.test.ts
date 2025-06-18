import {
  describe,
  expect,
  it,
  vi,
  beforeEach,
  afterEach,
  beforeAll,
} from 'vitest';
import { Beam } from '@/core/Beam';
import { BeamUtils } from '@/core/BeamUtils';
import { NodeTokenStorage } from '@/platform';
import type { TokenStorage } from '@/platform/types/TokenStorage';

describe('Beam', () => {
  let nodeTokenStorage: NodeTokenStorage;

  beforeAll(() => {
    // Mock NodeTokenStorage
    nodeTokenStorage = {
      getRefreshToken: vi.fn(),
      setAccessToken: vi.fn(),
      setRefreshToken: vi.fn(),
      removeRefreshToken: vi.fn(),
    } as unknown as NodeTokenStorage;
  });

  it('returns a formatted summary of the instance configuration', () => {
    const cid = '1713028771755577';
    const pid = 'DE_1740294079885317';
    const beam = new Beam({
      tokenStorage: nodeTokenStorage,
      environment: 'Dev',
      cid,
      pid,
    });

    expect(beam.toString()).toBe(`Beam(config: cid=${cid}, pid=${pid})`);
  });

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
      beam.account.getCurrentPlayer = vi
        .fn()
        .mockResolvedValue(dummyPlayer as any);

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
      beam.account.getCurrentPlayer = vi
        .fn()
        .mockResolvedValue(dummyPlayer as any);

      await beam.ready();

      expect(beam.auth.refreshAuthToken).toHaveBeenCalledWith('refreshToken');
      expect(saveTokenSpy).toHaveBeenCalledWith(storage, dummyTokenResponse);
      expect(beam.player.account).toEqual(dummyPlayer);
    });

    it('falls back to guest sign-in when refresh token does not exist', async () => {
      storage.getAccessToken.mockResolvedValue('access');
      storage.isExpired = true;
      storage.getRefreshToken.mockResolvedValue(null);
      beam.auth.signInAsGuest = vi
        .fn()
        .mockResolvedValue(dummyTokenResponse as any);
      beam.account.getCurrentPlayer = vi
        .fn()
        .mockResolvedValue(dummyPlayer as any);

      await beam.ready();

      expect(beam.auth.signInAsGuest).toHaveBeenCalled();
      expect(saveTokenSpy).toHaveBeenCalledWith(storage, dummyTokenResponse);
      expect(beam.player.account).toEqual(dummyPlayer);
    });

    it('only sets player account when token exists and not expired', async () => {
      storage.getAccessToken.mockResolvedValue('access');
      storage.isExpired = false;
      beam.account.getCurrentPlayer = vi
        .fn()
        .mockResolvedValue(dummyPlayer as any);
      await beam.ready();
      expect(beam.auth.signInAsGuest).not.toHaveBeenCalled();
      expect(saveTokenSpy).not.toHaveBeenCalled();
      expect(beam.player.account).toEqual(dummyPlayer);
    });
  });
});
