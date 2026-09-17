import { describe, expect, it, vi, beforeEach, afterAll } from 'vitest';
import { MockBeamWebSocket } from '../network/websocket/MockBeamWebSocket';
import { ContentService } from '@/services/ContentService';

// Use mock WebSocket for realtime connection
vi.mock('@/network/websocket/BeamWebSocket', () => ({
  BeamWebSocket: MockBeamWebSocket,
}));

import { Beam } from '@/core/Beam';
import { AuthService } from '@/services/AuthService';
import { AccountService } from '@/services/AccountService';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';

const tokenStorage = {
  getTokenData: async () => ({
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresIn: null,
  }),
  isExpired: false,
  setTokenData: async () => {},
  clear: async () => {},
} as unknown as TokenStorage;

/** The resolved platform apiUrl is written to the inner requester's private `_baseUrl`. */
const apiUrlOf = (beam: Beam): string =>
  (beam.requester as any).inner._baseUrl;

describe('Beam.init – host / environment resolution', () => {
  const mockAccountCurrent = vi.spyOn(AccountService.prototype, 'current');
  const mockSyncContentManifests = vi.spyOn(
    ContentService.prototype,
    'syncContentManifests',
  );
  vi.spyOn(AuthService.prototype, 'loginAsGuest');
  const fromHostSpy = vi.spyOn(BeamEnvironment, 'fromHost');
  const getSpy = vi.spyOn(BeamEnvironment, 'get');

  beforeEach(() => {
    fromHostSpy.mockClear();
    getSpy.mockClear();
    mockAccountCurrent.mockResolvedValue({
      id: 'player-id',
      deviceIds: [],
      scopes: [],
      thirdPartyAppAssociations: [],
    });
    mockSyncContentManifests.mockResolvedValue();
  });

  afterAll(() => {
    vi.restoreAllMocks();
  });

  it('derives a built-in environment from the host URL', async () => {
    const beam = await Beam.init({
      cid: 'c',
      pid: 'p',
      host: 'https://dev.api.beamable.com',
      tokenStorage,
    });
    expect(fromHostSpy).toHaveBeenCalledWith('https://dev.api.beamable.com');
    expect(getSpy).not.toHaveBeenCalled();
    expect(apiUrlOf(beam)).toBe('https://dev.api.beamable.com');
  });

  it('treats an unknown host as a custom environment', async () => {
    const beam = await Beam.init({
      cid: 'c',
      pid: 'p',
      host: 'http://localhost:9000',
      tokenStorage,
    });
    expect(apiUrlOf(beam)).toBe('http://localhost:9000');
  });

  it('falls back to the named environment when no host is given', async () => {
    const beam = await Beam.init({
      cid: 'c',
      pid: 'p',
      environment: 'dev',
      tokenStorage,
    });
    expect(fromHostSpy).not.toHaveBeenCalled();
    expect(getSpy).toHaveBeenCalledWith('dev');
    expect(apiUrlOf(beam)).toBe('https://dev.api.beamable.com');
  });

  it('defaults to prod when neither host nor environment is provided', async () => {
    const beam = await Beam.init({ cid: 'c', pid: 'p', tokenStorage });
    expect(fromHostSpy).not.toHaveBeenCalled();
    expect(getSpy).toHaveBeenCalledWith('prod');
    expect(apiUrlOf(beam)).toBe('https://api.beamable.com');
  });

  it('prefers host over environment when both are set', async () => {
    const beam = await Beam.init({
      cid: 'c',
      pid: 'p',
      environment: 'dev',
      host: 'https://api.beamable.com',
      tokenStorage,
    });
    expect(fromHostSpy).toHaveBeenCalledWith('https://api.beamable.com');
    expect(apiUrlOf(beam)).toBe('https://api.beamable.com');
  });
});
