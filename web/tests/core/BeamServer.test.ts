import { describe, it, expect, beforeEach } from 'vitest';
import { BeamServer } from '@/core/BeamServer';
import { BeamServerConfig } from '@/configs/BeamServerConfig';
import { BeamRequester } from '@/network/http/BeamRequester';

describe('BeamServer', () => {
  describe('init', () => {
    let config: BeamServerConfig;

    beforeEach(() => {
      config = {
        cid: 'test-cid',
        pid: 'test-pid',
        engine: 'test-engine',
        engineVersion: '1.0.0',
      };
    });

    it('should initialize BeamServer', async () => {
      const beamServer = await BeamServer.init(config);
      expect(beamServer).toBeInstanceOf(BeamServer);
    });

    it('should create a BeamRequester with useSignedRequest set to true', async () => {
      const beamServer = await BeamServer.init({
        ...config,
        useSignedRequest: true,
      });
      const requester = beamServer.requester;
      expect(requester).toBeInstanceOf(BeamRequester);
      expect((requester as any)['useSignedRequest']).toBe(true);
    });
  });
});
