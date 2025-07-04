import { describe, it, expect, afterEach, vi } from 'vitest';
import * as qsModule from '@/utils/makeQueryString';
import { makeApiRequest } from '@/utils/makeApiRequest';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpRequest } from '@/network/http/types/HttpRequest';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import { DELETE, GET, PATCH, POST, PUT } from '@/constants';

describe('makeApiRequest', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should call requester.request with minimal data when only required props are provided', async () => {
    const mockResponse: HttpResponse<{ data: string }> = {
      status: 200,
      headers: {},
      body: { data: 'ok' },
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const r = {
      request: mockRequest,
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;

    const qsSpy = vi.spyOn(qsModule, 'makeQueryString');
    const e = '/endpoint';
    const m = GET;

    const response = await makeApiRequest({ r, e, m });

    expect(qsSpy).not.toHaveBeenCalled();
    expect(mockRequest).toHaveBeenCalledOnce();
    expect(mockRequest.mock.calls[0][0]).toEqual({
      url: e,
      method: m,
      headers: {},
    } as HttpRequest);
    expect(response).toBe(mockResponse);
  });

  it('should include query string when queries are provided', async () => {
    const mockResponse: HttpResponse<unknown> = {
      status: 204,
      headers: {},
      body: undefined,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const r = {
      request: mockRequest,
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;

    const q = { foo: 'bar', baz: 123 };
    const fakeQs = '?foo=bar&baz=123';
    const qsSpy = vi.spyOn(qsModule, 'makeQueryString').mockReturnValue(fakeQs);

    await makeApiRequest({
      r,
      e: '/endpoint',
      m: POST,
      q,
    });

    expect(qsSpy).toHaveBeenCalledOnce();
    expect(qsSpy).toHaveBeenCalledWith(q);
    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({ url: '/endpoint' + fakeQs }),
    );
  });

  it('should include payload in the request body when payload is provided', async () => {
    const mockResponse: HttpResponse<unknown> = {
      status: 200,
      headers: {},
      body: null,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const r = {
      request: mockRequest,
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;

    const p = { a: 1, b: 'two' };
    await makeApiRequest({
      r,
      e: '/payload',
      m: PUT,
      p,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({ body: p }),
    );
  });

  it('should set X-BEAM-GAMERTAG header when gamertag is provided', async () => {
    const mockResponse: HttpResponse<unknown> = {
      status: 200,
      headers: {},
      body: null,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const r = {
      request: mockRequest,
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;

    const g = 'player123';
    await makeApiRequest({
      r,
      e: '/tag',
      m: DELETE,
      g,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        headers: { 'X-BEAM-GAMERTAG': g },
      }),
    );
  });

  it('should include withAuth flag when withAuth is provided', async () => {
    const mockResponse: HttpResponse<unknown> = {
      status: 200,
      headers: {},
      body: null,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const r = {
      request: mockRequest,
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;

    await makeApiRequest({
      r,
      e: '/auth',
      m: PATCH,
      w: true,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({ withAuth: true }),
    );
  });
});
