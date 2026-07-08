import { describe, it, expect, afterEach } from 'vitest';
import { BeamServerWebSocket } from '@/network/websocket/BeamServerWebSocket';

describe('BeamServerWebSocket.setWebSocketUrl', () => {
  // Capture/restore globalThis.window so we can simulate browser vs non-browser envs.
  const originalWindow = (globalThis as any).window;

  afterEach(() => {
    if (originalWindow === undefined) delete (globalThis as any).window;
    else (globalThis as any).window = originalWindow;
  });

  function buildUrl(apiUrl: string): string {
    const ws = new BeamServerWebSocket();
    // setWebSocketUrl is private and does not open a socket — call it directly.
    (ws as any).setWebSocketUrl(apiUrl);
    return (ws as any).wsUrl as string;
  }

  it('keeps localhost in the browser (no host.docker.internal rewrite)', () => {
    (globalThis as any).window = { document: {} };
    expect(buildUrl('http://localhost:8080')).toBe('ws://localhost:8080/socket');
  });

  it('rewrites localhost to host.docker.internal outside the browser (e.g. in-Docker microservice)', () => {
    delete (globalThis as any).window;
    expect(buildUrl('http://localhost:8080')).toBe('ws://host.docker.internal:8080/socket');
  });

  it('uses wss for https hosts', () => {
    (globalThis as any).window = { document: {} };
    expect(buildUrl('https://api.example.com')).toBe('wss://api.example.com/socket');
  });
});
