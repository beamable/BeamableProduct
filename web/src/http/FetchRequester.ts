import type { HttpRequest } from './types/HttpRequest';
import type { HttpResponse } from './types/HttpResponse';
import type { HttpRequester } from './types/HttpRequester';
import type { FetchRequesterConfig } from '@/configs/FetchRequesterConfig';
import { HttpMethod } from './types/HttpMethod';

export class FetchRequester implements HttpRequester {
  private readonly timeout: number;
  private readonly baseUrl?: string;
  private readonly defaultHeaders: Record<string, string>;
  private readonly withCredentials: boolean;
  private readonly fetchImpl: typeof fetch;
  private readonly authProvider?: () => Promise<string> | string;

  constructor(config: FetchRequesterConfig = {}) {
    this.timeout = config.timeout ?? 10_000; // defaults to 10 seconds
    this.baseUrl = config.baseUrl;
    this.defaultHeaders = config.defaultHeaders ?? {};
    this.withCredentials = config.withCredentials ?? false;
    this.fetchImpl = config.customFetch ?? fetch;
    this.authProvider = config.authProvider;

    if (typeof this.fetchImpl !== 'function') {
      throw new Error(
        'Fetch is not available. Provide a customFetch or use Node 18+ / browser.',
      );
    }
  }

  async request<TRes = any, TReq = any>(
    req: HttpRequest<TReq>,
  ): Promise<HttpResponse<TRes>> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    const finalUrl = this.baseUrl
      ? new URL(req.url, this.baseUrl).toString()
      : req.url;

    const requestHeaders = {
      ...this.defaultHeaders,
      ...req.headers,
    };

    if (req.withAuth) await this.AddAuthHeader(requestHeaders);

    try {
      const response = await this.fetchImpl(finalUrl, {
        method: req.method ?? HttpMethod.GET,
        headers: requestHeaders,
        body: req.body as BodyInit | null | undefined,
        signal: controller.signal,
        credentials: this.withCredentials ? 'include' : 'same-origin',
      });

      const contentType = response.headers.get('content-type');
      const responseBody = contentType?.includes('application/json')
        ? await response.json()
        : await response.text();

      const responseHeaders: Record<string, string> = {};
      response.headers.forEach((v, k) => (responseHeaders[k] = v));

      return {
        status: response.status,
        headers: responseHeaders,
        body: responseBody,
      } as HttpResponse<TRes>;
    } catch (err: any) {
      if (err.name === 'AbortError') {
        throw new Error(
          `Request to ${finalUrl} timed out after ${this.timeout}ms`,
        );
      }
      throw err;
    } finally {
      clearTimeout(timeoutId);
    }
  }

  private async AddAuthHeader(
    requestHeaders: Record<string, string>,
  ): Promise<void> {
    const token = await this.authProvider?.();
    if (!token) return;

    requestHeaders['Authorization'] = `Bearer ${token}`;
  }
}
