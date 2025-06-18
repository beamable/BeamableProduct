import { HttpRequester } from './types/HttpRequester';
import { HttpRequest } from './types/HttpRequest';
import { HttpResponse } from './types/HttpResponse';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { AuthApi } from '@/__generated__/apis';
import { RefreshAccessTokenError, NoRefreshTokenError } from './Errors';

type BeamRequesterConfig = {
  inner: HttpRequester;
  tokenStorage: TokenStorage;
  cid: string;
  pid: string;
};

/**
 * A decorator over `HttpRequester` that adds automatic access token refresh and custom JSON serialization/deserialization support.
 * - Retries requests that fail with a 401 Unauthorized once, after attempting to refresh the token.
 * - Serializes request bodies using `BeamJsonUtils.replacer` (supports BigInt, Date, etc.).
 * - Deserializes JSON response bodies using `BeamJsonUtils.reviver`.
 * - Leaves non-JSON bodies (e.g., FormData) untouched.
 */
export class BeamRequester implements HttpRequester {
  private readonly inner: HttpRequester;
  private readonly tokenStorage: TokenStorage;
  private readonly cid: string;
  private readonly pid: string;

  constructor(config: BeamRequesterConfig) {
    this.inner = config.inner;
    this.tokenStorage = config.tokenStorage;
    this.cid = config.cid;
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

    const newReq: HttpRequest<any> = { ...req, body };
    let response = await this.inner.request<TRes, any>(newReq);

    if (response.status === 401) {
      await this.handleRefresh();
      response = await this.inner.request<TRes, TReq>(req);
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

  setBaseUrl(url: string): void {
    this.inner.setBaseUrl(url);
  }

  setDefaultHeader(key: string, value?: string): void {
    this.inner.setDefaultHeader(key, value);
  }

  setTokenProvider(provider: () => Promise<string> | string): void {
    this.inner.setTokenProvider(provider);
  }

  private async handleRefresh(): Promise<void> {
    const refreshToken = await this.tokenStorage.getRefreshToken();
    if (!refreshToken) {
      throw new NoRefreshTokenError();
    }

    const response = await new AuthApi(this).postAuthToken({
      grant_type: 'refresh_token',
      refresh_token: refreshToken,
    });
    if (response.status !== 200) {
      await this.tokenStorage.removeRefreshToken();
      throw new RefreshAccessTokenError();
    }

    const {
      access_token: newAccessToken,
      refresh_token: newRefreshToken,
      expires_in: newExpiresIn,
    } = response.body;

    if (newAccessToken) {
      await this.tokenStorage.setAccessToken(newAccessToken);
    }

    if (newRefreshToken) {
      await this.tokenStorage.setRefreshToken(newRefreshToken);
    }

    if (newExpiresIn) {
      await this.tokenStorage.setExpiresIn(Date.now() + Number(newExpiresIn));
    }
  }
}
