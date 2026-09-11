import { describe, expect, it, vi } from 'vitest';
import * as apis from '@/__generated__/apis';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { Message } from '@/__generated__/schemas';
import { MailService, MailState } from '@/services/MailService';
import { FUNNEL_CATEGORY } from '@/services/AnalyticsService';
import { BeamBase } from '@/core/BeamBase';

/**
 * The behaviour that matters here is the IMPLICIT funnel reporting: a game reading mail through this
 * service must get the campaign `Opened` stage for free. If that stops happening, campaigns delivered
 * over the in-game rail silently report no engagement — which looks like "nobody read it" rather than
 * like a bug, so it is worth pinning precisely.
 */
function mail(id: string, metadata?: Record<string, string>): Message {
  return {
    attachments: [],
    category: 'campaign',
    id,
    receiverGamerTag: '1',
    senderGamerTag: '0',
    sent: '0',
    state: MailState.Unread,
    ...(metadata ? { metadata } : {}),
  } as Message;
}

function build() {
  const tracked: any[] = [];
  const beam = {
    cid: 'cid',
    pid: 'pid',
    requester: {} as HttpRequester,
    analytics: { trackSafely: (e: unknown) => tracked.push(e) },
  } as unknown as BeamBase;

  const service = new MailService({ beam });
  service.userId = '1';
  return { service, tracked };
}

function mockSearch(messages: Message[]) {
  vi.spyOn(apis, 'mailPostSearchByObjectId').mockResolvedValue({
    status: 200,
    headers: {},
    body: { results: [{ count: messages.length, name: 'inbox', content: messages }] },
  } as any);
}

describe('MailService', () => {
  it('lists mail through the search endpoint', async () => {
    mockSearch([mail('1')]);
    const { service } = build();

    const result = await service.list({ limit: 5 });

    expect(result).toHaveLength(1);
    expect(apis.mailPostSearchByObjectId).toHaveBeenCalled();
  });

  it('reports the campaign Opened stage when campaign mail is marked read', async () => {
    mockSearch([mail('1', { beam_outreach: 'outreach-1', trackId: 'campaign:c1:1:send' })]);
    const update = vi
      .spyOn(apis, 'mailPutBulkByObjectId')
      .mockResolvedValue({ status: 200, headers: {}, body: {} } as any);
    const { service, tracked } = build();

    await service.list();
    await service.markAsRead('1');

    expect(update).toHaveBeenCalled();
    expect(tracked).toHaveLength(1);
    // Shape must match what the push SDKs send, or the platform's campaign consumer drops it: it
    // routes on the category and needs BOTH the outreach id and the track ref to attribute a stage.
    expect(tracked[0]).toMatchObject({
      name: 'Opened',
      category: FUNNEL_CATEGORY,
      params: {
        outreachId: 'outreach-1',
        trackId: 'campaign:c1:1:send',
        funnelType: 'ingame',
        mailId: '1',
      },
    });
  });

  it('reports nothing for ordinary game mail', async () => {
    mockSearch([mail('1')]);
    vi.spyOn(apis, 'mailPutBulkByObjectId').mockResolvedValue({
      status: 200,
      headers: {},
      body: {},
    } as any);
    const { service, tracked } = build();

    await service.list();
    await service.markAsRead('1');

    expect(tracked).toHaveLength(0);
  });

  it('reports nothing when only half the attribution is present', async () => {
    // outreachId without trackId is unusable: the consumer parses trackId to find the campaign and
    // node, and drops the stage as `no_track_ref` without it. Better to send nothing than noise.
    mockSearch([mail('1', { beam_outreach: 'outreach-1' })]);
    vi.spyOn(apis, 'mailPutBulkByObjectId').mockResolvedValue({
      status: 200,
      headers: {},
      body: {},
    } as any);
    const { service, tracked } = build();

    await service.list();
    await service.markAsRead('1');

    expect(tracked).toHaveLength(0);
  });

  it('does not report twice when the same mail is re-read', async () => {
    mockSearch([mail('1', { beam_outreach: 'o1', trackId: 't1' })]);
    vi.spyOn(apis, 'mailPutBulkByObjectId').mockResolvedValue({
      status: 200,
      headers: {},
      body: {},
    } as any);
    const { service, tracked } = build();

    await service.list();
    await service.markAsRead('1');
    await service.markAsRead('1');

    expect(tracked).toHaveLength(1);
  });

  it('only reports on a transition to Read, not on any state change', async () => {
    mockSearch([mail('1', { beam_outreach: 'o1', trackId: 't1' })]);
    vi.spyOn(apis, 'mailPutBulkByObjectId').mockResolvedValue({
      status: 200,
      headers: {},
      body: {},
    } as any);
    const { service, tracked } = build();

    await service.list();
    await service.update({ id: '1', state: MailState.Deleted });

    expect(tracked).toHaveLength(0);
  });

  it('reports one event per campaign mail in a bulk update', async () => {
    mockSearch([
      mail('1', { beam_outreach: 'o1', trackId: 't1' }),
      mail('2', { beam_outreach: 'o2', trackId: 't2' }),
      mail('3'),
    ]);
    vi.spyOn(apis, 'mailPutBulkByObjectId').mockResolvedValue({
      status: 200,
      headers: {},
      body: {},
    } as any);
    const { service, tracked } = build();

    await service.list();
    await service.markAsRead(['1', '2', '3']);

    expect(tracked.map((e) => e.params.mailId)).toEqual(['1', '2']);
  });

  it('still marks mail read when the analytics accessor throws', async () => {
    // The mail update is the correctness-bearing call; reporting is decoration on top of it. A game
    // that registered mail but NOT analytics must not fail to read mail.
    //
    // Modelled on the real accessor, not on a plain absent property: `beam.analytics` for an
    // unregistered service is a getter that THROWS (BeamBase.throwServiceUnavailable). An earlier
    // version of this test used `{}`, which passes against a truthiness check and so missed exactly
    // the failure a game hits in practice - markAsRead rejecting AFTER the server-side update
    // succeeded, which is indistinguishable from a failed read.
    mockSearch([mail('1', { beam_outreach: 'o1', trackId: 't1' })]);
    const update = vi
      .spyOn(apis, 'mailPutBulkByObjectId')
      .mockResolvedValue({ status: 200, headers: {}, body: {} } as any);

    const beam = { cid: 'c', pid: 'p', requester: {} as HttpRequester } as unknown as BeamBase;
    Object.defineProperty(beam, 'analytics', {
      get() {
        throw new Error('Call `beam.use(AnalyticsService)` to enable the analytics service.');
      },
    });
    const service = new MailService({ beam });
    service.userId = '1';

    await service.list();
    await expect(service.markAsRead('1')).resolves.toBeUndefined();
    expect(update).toHaveBeenCalled();
  });
});
