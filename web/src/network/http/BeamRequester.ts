import { HttpRequester } from './types/HttpRequester';
import { HttpRequest } from './types/HttpRequest';
import { HttpResponse } from './types/HttpResponse';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { authPostTokenBasic } from '@/__generated__/apis';
import {
  RefreshAccessTokenError,
  NoRefreshTokenError,
  BeamError,
} from '@/constants/Errors';
import { DELETE, GET, HEADERS } from '@/constants';
import { HttpMethod } from '@/network/http/types/HttpMethod';
import { getPathAndQuery } from '@/utils/getPathAndQuery';
import { createHash } from '@/utils/createHash';
import { BeamBase } from '@/core/BeamBase';

type BeamRequesterConfig = {
  inner: HttpRequester;
  tokenStorage: TokenStorage;
  useSignedRequest: boolean;
  pid: string;
};

/**
 * A decorator over `HttpRequester` that adds automatic access token refresh and custom JSON serialization/deserialization support.
 * - Retries requests that fail with a 401 Unauthorized once, after attempting to refresh the token.
 * - Serializes request bodies using `BeamJsonUtils.replacer` (supports BigInt, Date, etc.).
 * - Deserializes JSON response bodies using `BeamJsonUtils.reviver`.
 * - Leaves non-JSON bodies (e.g., FormData) untouched.
 * - Automatically attaches the Bearer token to the `Authorization` header for authenticated requests.
 * - Signs requests with a signature header when `useSignedRequest` is enabled.
 */
export class BeamRequester implements HttpRequester {
  private readonly inner: HttpRequester;
  private readonly tokenStorage?: TokenStorage;
  private readonly useSignedRequest?: boolean;
  private readonly pid: string;

  constructor(config: BeamRequesterConfig) {
    this.inner = config.inner;
    this.tokenStorage = config.tokenStorage;
    this.useSignedRequest = config.useSignedRequest;
    this.pid = config.pid;
  }

  async request<TRes = any, TReq = any>(
    req: HttpRequest<TReq>,
  ): Promise<HttpResponse<TRes>> {
    let body = req.body as any;

    // If request body is a plain object (or anything that isn’t already a string or FormData),
    // run it through the custom replacer so date/bigint/etc. are serialized correctly.
    if (
      body != null &&
      typeof body !== 'string' &&
      !(body instanceof FormData)
    ) {
      body = JSON.stringify(body, BeamJsonUtils.replacer);
    }

    // If body is already a JSON string, try to parse it (applying the custom reviver),
    // then re‐stringify with the replacer, so pre‐stringify payloads still honor the custom rules.
    if (typeof body === 'string') {
      try {
        const parsed = JSON.parse(body, BeamJsonUtils.reviver);
        body = JSON.stringify(parsed, BeamJsonUtils.replacer);
      } catch {
        // leave request body as-is if not JSON parseable
      }
    }

    if (this.useSignedRequest) {
      const body = this.getBodyToSign(req.method ?? GET, req.body);
      // add the signature to the request headers
      req.headers = {
        ...req.headers,
        [HEADERS.BEAM_SIGNATURE]: this.generateSignature(req.url, body),
      };
    } else if (req.withAuth) {
      await this.attachAuthHeader(req);
    }

    // add the routing key to the request headers if it exists
    if (BeamBase.env.BEAM_ROUTING_KEY) {
      req.headers = {
        ...req.headers,
        [HEADERS.ROUTING_KEY]: BeamBase.env.BEAM_ROUTING_KEY,
      };
    }

    const newReq: HttpRequest = { ...req, body };
    let response = await this.inner.request<TRes, any>(newReq);

    const errorKeys = [
      'InvalidCredentialsError',
      'InvalidRefreshTokenError',
      'TokenValidationError',
    ] as const;

    const hasKnownError =
      typeof response.body === 'string' &&
      errorKeys.some((key) => (response.body as string).includes(key));

    if (response.status === 401 && !this.useSignedRequest && !hasKnownError) {
      await this.handleRefresh();
      // Re-attach Authorization header with the updated access token
      await this.attachAuthHeader(newReq);
      response = await this.inner.request<TRes, TReq>(newReq);
    }

    // throw a beam error if the response is not successful
    if (response.status < 200 || response.status >= 300) {
      throw new BeamError(
        `Request to '${req.url}' failed with status ${response.status}: ${response.body}`,
        {
          context: {
            request: {
              url: newReq.url,
              method: newReq.method,
              headers: newReq.headers,
              body: newReq.body,
            },
            response: {
              status: response.status,
              message: response.body,
            },
          },
        },
      );
    }

    let newBody: any = response.body;

    if (typeof response.body === 'string') {
      try {
        newBody = JSON.parse(response.body, BeamJsonUtils.reviver);
      } catch {
        // leave response body as-is if invalid JSON
      }
    }
    return { ...response, body: newBody };
  }

  set baseUrl(url: string) {
    this.inner.baseUrl = url;
  }

  set defaultHeaders(headers: Record<string, string>) {
    this.inner.defaultHeaders = headers;
  }

  private getBodyToSign(method: HttpMethod, body: unknown): string | null {
    if (method === GET || method === DELETE) return null;
    if (body == null) return null;
    if (typeof body === 'string') return body;
    return JSON.stringify(body, BeamJsonUtils.replacer);
  }

  private generateSignature(url: string, body: string | null): string {
    const version = '1';
    const secret = BeamBase.env.BEAM_REALM_SECRET;
    if (!secret) {
      throw new BeamError(
        '`BEAM_REALM_SECRET` environment variable is not set. Assign `BeamServer.env.BEAM_REALM_SECRET = process.env.BEAM_REALM_SECRET` to enable signed requests.',
      );
    }

    let data = `${secret}${this.pid}${version}${getPathAndQuery(url)}`;
    if (body) data += body;

    // hash using md5 to base64 string
    return createHash('md5').update(data).digest('base64');
  }

  private async attachAuthHeader(req: HttpRequest<any>): Promise<void> {
    const data = await this.tokenStorage?.getTokenData();
    const token = data?.accessToken ?? null;
    if (token) {
      req.headers = {
        ...req.headers,
        [HEADERS.AUTHORIZATION]: `Bearer ${token}`,
      };
    }
  }

  private async handleRefresh(): Promise<void> {
    const tokenData = await this.tokenStorage?.getTokenData();
    const refreshToken = tokenData?.refreshToken ?? null;
    if (!refreshToken) {
      throw new NoRefreshTokenError();
    }

    const response = await authPostTokenBasic(this, {
      grant_type: 'refresh_token',
      refresh_token: refreshToken,
    });
    if (response.status !== 200) {
      await this.tokenStorage?.setTokenData({ refreshToken: null });
      throw new RefreshAccessTokenError();
    }

    const {
      access_token: newAccessToken,
      refresh_token: newRefreshToken,
      expires_in: newExpiresIn,
    } = response.body;

    const update: any = {};
    if (newAccessToken) update['accessToken'] = newAccessToken;
    if (newRefreshToken) update['refreshToken'] = newRefreshToken;
    if (newExpiresIn) update['expiresIn'] = Date.now() + Number(newExpiresIn);
    if (Object.keys(update).length > 0) {
      await this.tokenStorage?.setTokenData(update);
    }
  }
}
