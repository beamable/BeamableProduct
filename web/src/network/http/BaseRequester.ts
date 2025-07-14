import type { HttpRequest } from './types/HttpRequest';
import type { HttpResponse } from './types/HttpResponse';
import type { HttpRequester } from './types/HttpRequester';
import type { BaseRequesterConfig } from '@/configs/BaseRequesterConfig';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';
import { GET } from '@/constants';
import { BeamError } from '@/constants/Errors';

/** A basic HttpRequester implementation using the Fetch API. */
export class BaseRequester implements HttpRequester {
  private readonly timeout: number;
  private readonly fetchImpl: typeof fetch;
  private _baseUrl?: string;
  private _defaultHeaders?: Record<string, string>;

  constructor(config: BaseRequesterConfig = {}) {
    this.timeout = config.timeout ?? 10_000; // defaults to 10 seconds
    this.fetchImpl = config.customFetch ?? fetch;

    this.fetchImpl =
      config.customFetch ??
      (typeof globalThis.fetch === 'function'
        ? globalThis.fetch.bind(globalThis)
        : (undefined as never));

    if (typeof this.fetchImpl !== 'function') {
      throw new Error(
        'Fetch is not available. Provide a customFetch implementation.',
      );
    }
  }

  async request<TRes = any, TReq = any>(
    req: HttpRequest<TReq>,
  ): Promise<HttpResponse<TRes>> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    const finalUrl = this._baseUrl
      ? new URL(req.url, this._baseUrl).toString()
      : req.url;

    const requestHeaders = {
      ...this._defaultHeaders,
      ...req.headers,
    };

    try {
      const response = await this.fetchImpl(finalUrl, {
        method: req.method ?? GET,
        headers: requestHeaders,
        body: req.body as BodyInit | null | undefined,
        signal: controller.signal,
        credentials: 'same-origin',
      });

      const contentType = response.headers.get('content-type');
      const text = await response.text();
      const responseBody = contentType?.includes('application/json')
        ? JSON.parse(text, BeamJsonUtils.reviver) // deserialize with custom reviver
        : text;

      const responseHeaders: Record<string, string> = {};
      response.headers.forEach((v, k) => (responseHeaders[k] = v));

      return {
        status: response.status,
        headers: responseHeaders,
        body: responseBody,
      } as HttpResponse<TRes>;
    } catch (error: any) {
      const context = {
        url: finalUrl,
        method: req.method,
        headers: requestHeaders,
        body: req.body,
      };

      if (error.name === 'AbortError') {
        throw new BeamError(
          `Request to ${finalUrl} timed out after ${this.timeout}ms`,
          {
            cause: error,
            context,
          },
        );
      }

      throw new BeamError('Request failed', {
        cause: error,
        context,
      });
    } finally {
      clearTimeout(timeoutId);
    }
  }

  set baseUrl(url: string) {
    this._baseUrl = url;
  }

  set defaultHeaders(headers: Record<string, string>) {
    this._defaultHeaders = headers;
  }
}
