import * as AxiosTypes from 'axios';
import axios from 'axios';

import { Readable, get } from '../lib/stores';
import BaseService from './base';
import RouterService from './router';
import { AuthToken } from './auth';

export type HttpAdapter       = AxiosTypes.AxiosInstance;
export type HttpRequestConfig = AxiosTypes.AxiosRequestConfig;
export type HttpResponse      = AxiosTypes.AxiosResponse;
export type HttpMethod        = AxiosTypes.Method;

export enum HttpHeaderNames {
  scope = 'X-DE-SCOPE',
  auth  = 'Authorization'
}

export interface HttpHeaders {
  [HttpHeaderNames.scope]? : string;
  [HttpHeaderNames.auth]? : string;
}

export const DataUnavailableValue = null;
export type DataUnavailable = null;

export class HttpService extends BaseService {
  // XXX: This should probably move to a config file when we have multiple builds.
  public readonly baseURL: string = window.config.host;
  public readonly scope: Readable<string> = this.derived(['org.id', 'realm.id'], this.computeScope);
  public readonly bearer: Readable<string> = this.derived(`auth.token.${RouterService.getOrgId()}`, this.computeAuth);

  public isResponseUnavailable<T>(data: T|DataUnavailable): boolean {
    return data === DataUnavailableValue;
  }

  public async request(url: string, data?: any, method: HttpMethod = 'post', includeAuth: boolean = true, pid: string|undefined = undefined) {
    let bearer;
    if (includeAuth){
      await this.app.auth.onInit();
      bearer = get(this.bearer);
    }

    let scope = pid 
      ? this.computeScope([RouterService.getOrgId(), pid])
      : get(this.scope)

    let config: HttpRequestConfig = {
      method: method,
      baseURL: this.baseURL,
      url: url,
      data: data,
      headers: this.computeHeaders(scope, bearer)
    };

    return axios(config).catch((err:any) => {
      throw err.response.data;
    });
  }

  public async requestWorld(url: string, method: HttpMethod = 'get') {
    let config: HttpRequestConfig = {
      method: method,
      url: url
    };

    return axios(config).catch((err:any) => {
      throw err.response.data;
    });
  }

  private computeAuth(token: AuthToken): string | undefined {
    return !token ? undefined : `Bearer ${token.access_token}`;
  }

  private computeScope([orgId, realmId]: string[]): string | undefined {
    return !orgId ? undefined : (
      realmId ? `${orgId}.${realmId}` : orgId
    );
  }

  private computeHeaders(scope: string | undefined, bearer: string | undefined): HttpHeaders {
    const headers: HttpHeaders = {};

    if (scope) headers[HttpHeaderNames.scope] = scope;
    if (bearer) headers[HttpHeaderNames.auth] = bearer;

    return headers;
  }
}

export default HttpService;
