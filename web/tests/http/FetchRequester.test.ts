import { afterEach, describe, expect, it, vi } from 'vitest';
import { FetchRequester } from '@/http/FetchRequester';
import { HttpMethod } from '@/http/types/HttpMethod';

type TestReq = { foo: string };
type TestRes = { ok: boolean };

const makeFetchMock = (
  body: unknown,
  opts: { status?: number; contentType?: string } = {},
) =>
  vi.fn().mockResolvedValue({
    status: opts.status ?? 200,
    headers: new Headers({
      'content-type': opts.contentType ?? 'application/json',
    }),
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(String(body)),
  } as unknown as Response);

describe('FetchRequester', () => {
  afterEach(() => {
    // clean up mocks between tests
    vi.useRealTimers();
  });

  it('performs a JSON GET request and returns parsed body', async () => {
    const fetchMock = makeFetchMock({ success: true });
    const requester = new FetchRequester({ customFetch: fetchMock });

    const res = await requester.request<TestRes, TestReq>({
      url: 'https://example.com/data',
      method: HttpMethod.GET,
    });

    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledWith(
      'https://example.com/data',
      expect.objectContaining({ method: HttpMethod.GET }),
    );
    expect(res.status).toBe(200);
    expect(res.body).toEqual({ success: true });
  });

  it('merges default headers and per-request headers', async () => {
    const fetchMock = makeFetchMock('ok', { contentType: 'text/plain' });
    const requester = new FetchRequester({
      customFetch: fetchMock,
      defaultHeaders: { 'x-default': 'A' },
    });

    await requester.request({
      url: 'https://example.com',
      headers: { 'x-custom': 'B' },
    });

    // Extract the `headers` object from the first call to fetchMock,
    // ignoring the first argument (the URL) and pulling out only the second argument (RequestInit).
    const [, { headers }] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(headers as Record<string, string>).toMatchObject({
      'x-default': 'A',
      'x-custom': 'B',
    });
  });

  it('adds an Authorization header when withAuth is true', async () => {
    const fetchMock = makeFetchMock('ok', { contentType: 'text/plain' });
    const requester = new FetchRequester({
      customFetch: fetchMock,
      tokenProvider: () => 'TOKEN_123',
    });

    await requester.request({
      url: 'https://secure.example.com',
      withAuth: true,
    });

    // Extract the `headers` object from the first call to fetchMock,
    // ignoring the first argument (the URL) and pulling out only the second argument (RequestInit).
    const [, { headers }] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((headers as Record<string, string>).Authorization).toBe(
      'Bearer TOKEN_123',
    );
  });

  it('builds the final URL from baseUrl + relative path', async () => {
    const fetchMock = makeFetchMock('ok', { contentType: 'text/plain' });
    const requester = new FetchRequester({
      baseUrl: 'https://api.example.com/',
      customFetch: fetchMock,
    });

    await requester.request({ url: '/users' });

    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.example.com/users',
      expect.any(Object),
    );
  });

  it('throws timeout error when fetch errors with AbortError', async () => {
    const timeout = 500; // in milliseconds
    const abortErr = new Error('aborted');
    abortErr.name = 'AbortError';
    const mockFetch = vi.fn().mockRejectedValue(abortErr);
    const requester = new FetchRequester({ timeout, customFetch: mockFetch });

    await expect(requester.request({ url: '/timeout' })).rejects.toThrow(
      `Request to /timeout timed out after ${timeout}ms`,
    );
  });
});
