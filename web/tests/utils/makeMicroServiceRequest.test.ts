import { describe, it, expect, afterEach, vi } from 'vitest';
import { makeMicroServiceRequest } from '@/utils/makeMicroServiceRequest';
import { BeamBase } from '@/core/BeamBase';
import type { HttpRequest } from '@/network/http/types/HttpRequest';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import { HEADERS, POST } from '@/constants';

describe('makeMicroServiceRequest', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should call requester.request with minimal data when only required props are provided', async () => {
    BeamBase.env.BEAM_ROUTING_KEY = 'route-key';
    const mockBody = { data: 'ok' };
    const mockResponse: HttpResponse<{ data: string }> = {
      status: 200,
      headers: {},
      body: mockBody,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const beam = {
      cid: 'cid',
      pid: 'pid',
      requester: { request: mockRequest },
    } as unknown as BeamBase;

    const serviceName = 'service';
    const endpoint = 'endpoint';

    const response = await makeMicroServiceRequest({
      beam,
      serviceName,
      endpoint,
      payload: undefined,
      withAuth: false,
    });

    const expectedUrl = `/basic/${beam.cid}.${beam.pid}.micro_${serviceName}/${endpoint}`;

    expect(mockRequest).toHaveBeenCalledOnce();
    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        method: POST,
        url: expectedUrl,
        headers: {
          'X-BEAM-SERVICE-ROUTING-KEY': 'route-key',
        },
      } as HttpRequest),
    );
    expect(response).toBe(mockBody);
  });

  it('should include routingKey header when routingKey is provided', async () => {
    BeamBase.env.BEAM_ROUTING_KEY = 'route-key';
    const mockResponse: HttpResponse<unknown> = {
      status: 204,
      headers: {},
      body: undefined,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const beam = {
      cid: 'cid',
      pid: 'pid',
      requester: { request: mockRequest },
    } as unknown as BeamBase;

    const routingKey = 'route-key';

    await makeMicroServiceRequest({
      beam,
      serviceName: 'svc',
      endpoint: 'ep',
      payload: undefined,
      withAuth: false,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        headers: { [HEADERS.ROUTING_KEY]: routingKey },
      } as unknown as HttpRequest),
    );
  });

  it('should include payload in the request body when payload is provided', async () => {
    const mockResponse: HttpResponse<unknown> = {
      status: 201,
      headers: {},
      body: null,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const beam = {
      cid: 'cid',
      pid: 'pid',
      requester: { request: mockRequest },
    } as unknown as BeamBase;

    const payload = { a: 1, b: 'two' };

    await makeMicroServiceRequest({
      beam,
      serviceName: 'svc',
      endpoint: 'ep',
      payload,
      withAuth: false,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({ body: payload } as HttpRequest),
    );
  });

  it('should include withAuth flag when withAuth is provided', async () => {
    const mockResponse: HttpResponse<unknown> = {
      status: 200,
      headers: {},
      body: null,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const beam = {
      cid: 'cid',
      pid: 'pid',
      requester: { request: mockRequest },
    } as unknown as BeamBase;

    await makeMicroServiceRequest({
      beam,
      serviceName: 'svc',
      endpoint: 'ep',
      payload: undefined,
      withAuth: true,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({ withAuth: true } as HttpRequest),
    );
  });
});
