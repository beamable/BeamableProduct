import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { BeamRequester } from '@/network/http/BeamRequester';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';
import {
  RefreshAccessTokenError,
  NoRefreshTokenError,
} from '@/constants/Errors';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { TokenStorage } from '@/platform/types/TokenStorage';
import * as apis from '@/__generated__/apis';
import { BeamBase } from '@/core/BeamBase';

type ObjReq = { date: Date; big: bigint; normal: string };
type ObjRes = { date: Date; num: bigint };

vi.mock('node:crypto', () => {
  const update = vi.fn().mockReturnThis();
  const digest = vi.fn().mockReturnValue('fake-b64');
  const createHash = vi.fn(() => ({ update, digest }));
  return { createHash };
});

describe('BeamRequester', () => {
  const pid = 'pid';
  let tokenStorage: TokenStorage;

  beforeEach(() => {
    tokenStorage = {
      getTokenData: vi.fn().mockResolvedValue({
        accessToken: 'test_token',
        refreshToken: null,
        expiresIn: null,
      }),
      setTokenData: vi.fn(),
    } as unknown as TokenStorage;
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('serializes request body using replacer', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    const reqBody: ObjReq = {
      date: new Date('2025-01-01T00:00:00.000Z'),
      big: BigInt(123),
      normal: 'test',
    };
    await requester.request<unknown, ObjReq>({ url: '/test', body: reqBody });

    expect(inner.request).toHaveBeenCalledOnce();
    // Get the first argument of the first call
    const calledReq = (inner.request as any).mock.calls[0][0];
    expect(calledReq.body).toBe(
      JSON.stringify(reqBody, BeamJsonUtils.replacer),
    );
  });

  it('parses and re-serializes JSON string body', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    const original: ObjReq = {
      date: new Date('2025-01-02T00:00:00.000Z'),
      big: BigInt(456),
      normal: 'foo',
    };
    const rawJson = JSON.stringify(original, BeamJsonUtils.replacer);
    await requester.request<unknown, string>({ url: '/test', body: rawJson });

    expect(inner.request).toHaveBeenCalledOnce();
    // Get the first argument of the first call
    const calledReq = (inner.request as any).mock.calls[0][0];
    expect(calledReq.body).toBe(
      JSON.stringify(
        JSON.parse(rawJson, BeamJsonUtils.reviver),
        BeamJsonUtils.replacer,
      ),
    );
  });

  it('leaves unparsable JSON string body intact', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    const raw = 'not a json';
    await requester.request<unknown, string>({ url: '/test', body: raw });

    expect(inner.request).toHaveBeenCalledOnce();
    // Get the first argument of the first call
    const calledReq = (inner.request as any).mock.calls[0][0];
    expect(calledReq.body).toBe(raw);
  });

  it('deserializes response body string using reviver', async () => {
    const date = new Date('2024-04-04T04:04:04.004Z');
    const numStr = '1234567890123';
    const bodyJson = JSON.stringify({ date: date.toISOString(), num: numStr });
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: bodyJson }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    const res = await requester.request<ObjRes, unknown>({ url: '/test' });
    expect(res.body).toEqual({ date, num: BigInt(numStr) });
  });

  it('leaves unparsable response body string intact', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: 'not json' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    const res = await requester.request<unknown, unknown>({ url: '/test' });
    expect(res.body).toBe('not json');
  });

  it('refreshes token and retries request on 401', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValueOnce({ status: 401, headers: {}, body: '' })
        .mockResolvedValueOnce({ status: 200, headers: {}, body: '"ok"' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    (tokenStorage.getTokenData as any).mockResolvedValue({
      accessToken: 'test_token',
      refreshToken: 'rt',
      expiresIn: null,
    });
    const mockRefreshResponse = {
      status: 200,
      headers: {},
      body: { access_token: 'at', refresh_token: 'nrt' },
    };
    vi.spyOn(apis, 'authPostTokenBasic').mockResolvedValue(
      mockRefreshResponse as any,
    );
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });
    const res = await requester.request<unknown, unknown>({ url: '/test' });
    expect(inner.request).toHaveBeenCalledTimes(2);
    expect(tokenStorage.getTokenData).toHaveBeenCalled();
    expect(apis.authPostTokenBasic).toHaveBeenCalledWith(requester, {
      grant_type: 'refresh_token',
      refresh_token: 'rt',
    });
    expect(tokenStorage.setTokenData).toHaveBeenCalledWith(
      expect.objectContaining({ accessToken: 'at', refreshToken: 'nrt' }),
    );
    expect(res.body).toBe('ok');
  });

  it('throws NoRefreshTokenError if no refresh token is available', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 401, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    (tokenStorage.getTokenData as any).mockResolvedValue({
      accessToken: 'test_token',
      refreshToken: null,
      expiresIn: null,
    });
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });
    await expect(requester.request({ url: '/test' })).rejects.toBeInstanceOf(
      NoRefreshTokenError,
    );
  });

  it('throws RefreshAccessTokenError and removes refresh token when refresh fails', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 401, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    (tokenStorage.getTokenData as any).mockResolvedValue({
      accessToken: 'test_token',
      refreshToken: 'rt',
      expiresIn: null,
    });
    const mockRefreshResponse = { status: 400, body: {} };
    vi.spyOn(apis, 'authPostTokenBasic').mockResolvedValue(
      mockRefreshResponse as any,
    );
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });
    await expect(requester.request({ url: '/test' })).rejects.toBeInstanceOf(
      RefreshAccessTokenError,
    );
    expect(tokenStorage.setTokenData).toHaveBeenCalledWith({
      refreshToken: null,
    });
  });

  it('throws BeamError on non-2xx response', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 404, headers: {}, body: 'Not Found' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    await expect(requester.request({ url: '/test' })).rejects.toThrow(
      "Request to '/test' failed with status 404: Not Found",
    );
  });

  it('should not sign request if useSignedRequest is false', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    await requester.request({ url: '/test' });

    const calledReq = (inner.request as any).mock.calls[0][0];
    if (calledReq.headers) {
      expect(calledReq.headers).not.toHaveProperty('X-BEAM-SIGNATURE');
    } else {
      expect(calledReq.headers).toBeUndefined();
    }
  });

  it('should sign request if useSignedRequest is true', async () => {
    BeamBase.env.BEAM_REALM_SECRET = 'test_secret';
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;
    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: true,
    });

    await requester.request({
      url: '/test',
      method: 'POST',
      body: { foo: 'bar' },
    });

    const calledReq = (inner.request as any).mock.calls[0][0];
    expect(calledReq.headers).toHaveProperty('X-BEAM-SIGNATURE');
    expect(calledReq.headers['X-BEAM-SIGNATURE']).toBe('fake-b64');
  });

  it('should include access token if withAuth is true and useSignedRequest is false', async () => {
    const inner = {
      request: vi
        .fn()
        .mockResolvedValue({ status: 200, headers: {}, body: '' }),
      setBaseUrl: vi.fn(),
      setDefaultHeader: vi.fn(),
      setTokenProvider: vi.fn(),
    } as unknown as HttpRequester;

    const requester = new BeamRequester({
      inner,
      tokenStorage,
      pid,
      useSignedRequest: false,
    });

    await requester.request({ url: '/test', withAuth: true });

    const calledReq = (inner.request as any).mock.calls[0][0];
    expect(calledReq.headers).toHaveProperty(
      'Authorization',
      'Bearer test_token',
    );
  });
});
