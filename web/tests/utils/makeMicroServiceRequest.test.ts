import { describe, it, expect, afterEach, vi } from 'vitest';
import { makeMicroServiceRequest } from '@/utils/makeMicroServiceRequest';
import { BeamBase } from '@/core/BeamBase';
import type { BeamMicroServiceHost } from '@/core/BeamMicroServiceClient';
import type { HttpRequest } from '@/network/http/types/HttpRequest';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import { HEADERS, POST } from '@/constants';

function makeHost(
  requester: { request: ReturnType<typeof vi.fn> },
  microServiceScope = 'pid',
): BeamMicroServiceHost {
  return {
    cid: 'cid',
    microServiceScope,
    requester: requester as unknown as BeamMicroServiceHost['requester'],
  };
}

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
    const host = makeHost({ request: mockRequest });

    const serviceName = 'service';
    const endpoint = 'endpoint';

    const response = await makeMicroServiceRequest({
      host,
      serviceName,
      endpoint,
      payload: undefined,
      withAuth: false,
    });

    const expectedUrl = `/basic/${host.cid}.${host.microServiceScope}.micro_${serviceName}/${endpoint}`;

    expect(mockRequest).toHaveBeenCalledOnce();
    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        method: POST,
        url: expectedUrl,
        headers: {
          [HEADERS.BEAM_SCOPE]: 'cid.pid',
          'X-BEAM-SERVICE-ROUTING-KEY': 'route-key',
        },
      } as HttpRequest),
    );
    expect(response).toBe(mockBody);
  });

  it('should route to the zone scope (cid.zid) for a zone host', async () => {
    BeamBase.env.BEAM_ROUTING_KEY = '';
    const mockResponse: HttpResponse<unknown> = {
      status: 200,
      headers: {},
      body: null,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    // A zone SDK reports its zid as the microServiceScope.
    const host = makeHost({ request: mockRequest }, 'zone-1');

    await makeMicroServiceRequest({
      host,
      serviceName: 'svc',
      endpoint: 'ep',
      payload: undefined,
      withAuth: false,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/basic/cid.zone-1.micro_svc/ep',
        headers: expect.objectContaining({ [HEADERS.BEAM_SCOPE]: 'cid.zone-1' }),
      } as unknown as HttpRequest),
    );
  });

  it('should include routingKey header when routingKey is provided', async () => {
    BeamBase.env.BEAM_ROUTING_KEY = 'route-key';
    const mockResponse: HttpResponse<unknown> = {
      status: 204,
      headers: {},
      body: undefined,
    };
    const mockRequest = vi.fn().mockResolvedValue(mockResponse);
    const host = makeHost({ request: mockRequest });

    const routingKey = 'route-key';

    await makeMicroServiceRequest({
      host,
      serviceName: 'svc',
      endpoint: 'ep',
      payload: undefined,
      withAuth: false,
    });

    expect(mockRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        headers: expect.objectContaining({ [HEADERS.ROUTING_KEY]: routingKey }),
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
    const host = makeHost({ request: mockRequest });

    const payload = { a: 1, b: 'two' };

    await makeMicroServiceRequest({
      host,
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
    const host = makeHost({ request: mockRequest });

    await makeMicroServiceRequest({
      host,
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
