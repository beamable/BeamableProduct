import { describe, expect, it, vi } from 'vitest';
import type { HttpRequest } from '@/network/http/types/HttpRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import { AnalyticsService } from '@/services/AnalyticsService';
import { BeamBase } from '@/core/BeamBase';

/**
 * The thing worth pinning here is the ROUTE.
 *
 * `/api/analytics/events` is served by the C# gateway, which a deployed environment's load balancer
 * selects on `/api/*`; an unprefixed `/analytics/events` falls through to the Scala gateway and comes
 * back `404 RouteNotFoundError`. It is an easy prefix to lose, because a local stack proxies
 * everything unmatched to the C# gateway and so answers either path happily — the drift only shows up
 * against a real realm. This is the SDK's only hand-written endpoint (every other one is generated
 * with the prefix already in it), so nothing else guards it.
 */
function build() {
  const sent: HttpRequest<unknown>[] = [];
  const requester = {
    request: vi.fn((req: HttpRequest<unknown>) => {
      sent.push(req);
      return Promise.resolve({ status: 202, headers: {}, body: undefined });
    }),
  } as unknown as HttpRequester;

  const beam = { cid: 'cid', pid: 'pid', requester } as unknown as BeamBase;
  return { service: new AnalyticsService({ beam }), sent, requester };
}

describe('AnalyticsService', () => {
  it('posts to the /api-prefixed gateway ingest route', async () => {
    const { service, sent } = build();

    await service.track({ name: 'purchase' });

    expect(sent).toHaveLength(1);
    expect(sent[0].url).toBe('/api/analytics/events');
    expect(sent[0].method).toBe('POST');
    expect(sent[0].withAuth).toBe(true);
  });

  it('sends the compact wire shape the platform binds', async () => {
    const { service, sent } = build();

    await service.track({
      name: 'purchase',
      params: { currency: 'USD' },
      time: 1700000000000,
    });

    expect(sent[0].body).toEqual([{ e: 'purchase', p: { currency: 'USD' }, time: 1700000000000 }]);
  });

  it('omits time when it is not set', async () => {
    const { service, sent } = build();

    await service.track({ name: 'level_complete', params: { level: 7 } });

    expect(sent[0].body).toEqual([{ e: 'level_complete', p: { level: 7 } }]);
  });

  it('batches several events into one request', async () => {
    const { service, sent } = build();

    await service.trackBatch([{ name: 'a' }, { name: 'b' }]);

    expect(sent).toHaveLength(1);
    expect(sent[0].body).toHaveLength(2);
  });

  it('sends nothing for an empty batch', async () => {
    const { service, requester } = build();

    await service.trackBatch([]);

    expect(requester.request).not.toHaveBeenCalled();
  });

  it('trackSafely swallows a failed send', async () => {
    const { service } = build();
    vi.spyOn(service, 'track').mockRejectedValue(new Error('boom'));

    // No await: the point is that the rejection never surfaces as an unhandled one either.
    expect(() => service.trackSafely({ name: 'purchase' })).not.toThrow();
    await Promise.resolve();
  });
});
