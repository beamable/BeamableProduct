import { describe, it, expect } from 'vitest';
import {
  BeamEnvironmentRegistry,
  BeamEnvironment,
} from '@/core/BeamEnvironmentRegistry';
import type { BeamEnvironmentConfig } from '@/configs/BeamEnvironmentConfig';

describe('BeamEnvironmentRegistry – default instance', () => {
  it('exposes the three built-in environments', () => {
    const list = BeamEnvironment.list();
    expect(list.dev).toBeDefined();
    expect(list.stg).toBeDefined();
    expect(list.prod).toBeDefined();
  });
});

describe('BeamEnvironmentRegistry – class behaviour', () => {
  /** fresh registry for every test */
  const create = () => new BeamEnvironmentRegistry();

  it('register() followed by get() returns the same config', () => {
    const reg = create();
    const cfg: BeamEnvironmentConfig = {
      apiUrl: 'http://localhost:7000',
      portalUrl: 'http://localhost:3000',
      beamMongoExpressUrl: 'http://localhost:8081',
      dockerRegistryUrl: 'http://localhost:5000/v2/',
    };

    reg.register('Local', cfg);
    expect(reg.get('Local')).toEqual(cfg);
  });

  it('get() on an unknown key throws a helpful error', () => {
    const reg = create();
    expect(() => reg.get('Nope')).toThrow(
      /Beam environment “Nope” is not registered/i,
    );
  });

  it('list() deep-freezes both the snapshot and its nested configs', () => {
    const reg = create();
    const snap = reg.list();

    // shallow freeze of the outer map
    expect(Object.isFrozen(snap)).toBe(true);

    // deep freeze of at least one nested object
    expect(Object.isFrozen(snap.Dev)).toBe(true);

    // attempts to mutate should blow up
    expect(() => {
      snap.Dev.apiUrl = 'https://bad.example.com';
    }).toThrow(TypeError);
    expect(() => {
      // @ts-expect-error intentional mutation
      snap.Foo = {} as BeamEnvironmentConfig;
    }).toThrow(TypeError);
  });

  it('findByApiUrl() matches a built-in environment (ignoring trailing slash)', () => {
    const reg = create();
    expect(reg.findByApiUrl('https://dev.api.beamable.com')?.apiUrl).toBe(
      'https://dev.api.beamable.com',
    );
    // trailing slash on the input is ignored
    expect(reg.findByApiUrl('https://api.beamable.com/')?.apiUrl).toBe(
      'https://api.beamable.com',
    );
  });

  it('findByApiUrl() returns undefined for an unknown host', () => {
    const reg = create();
    expect(reg.findByApiUrl('http://localhost:9000')).toBeUndefined();
  });

  it('fromHost() resolves a built-in host to its registered config', () => {
    const reg = create();
    expect(reg.fromHost('https://dev.api.beamable.com')).toEqual(
      reg.get('dev'),
    );
  });

  it('fromHost() treats an unknown host as a custom environment', () => {
    const reg = create();
    const cfg = reg.fromHost('http://localhost:9000/');
    expect(cfg.apiUrl).toBe('http://localhost:9000'); // trailing slash trimmed
    expect(cfg.portalUrl).toBe('');
    expect(cfg.beamMongoExpressUrl).toBe('');
    expect(cfg.dockerRegistryUrl).toBe('');
  });

  it('allows register() after list() and new env appears in subsequent list()', () => {
    const reg = create();
    const before = reg.list(); // initial snapshot

    const cfg: BeamEnvironmentConfig = {
      apiUrl: 'https://qa.api.beamable.com',
      portalUrl: 'https://qa-portal.beamable.com',
      beamMongoExpressUrl: 'https://qa.storage.beamable.com',
      dockerRegistryUrl: 'https://qa-microservices.beamable.com/v2/',
    };
    reg.register('QA', cfg); // register after snapshot

    // registry now knows about QA
    expect(reg.get('QA')).toEqual(cfg);

    // old snapshot is unchanged
    expect(before.QA).toBeUndefined();

    // new snapshot sees QA
    const after = reg.list();
    expect(after.QA).toEqual(cfg);
  });
});
