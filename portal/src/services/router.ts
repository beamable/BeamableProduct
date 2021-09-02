import { Readable, Writable, get } from '../lib/stores';
import { bind } from '../lib/decorators';
import BaseService from './base';

export type RouterData = string | void;

// Workaround for circular dependency in svelte-filerouter. TODO: replace library with routify
declare global {
  interface Window { router: any; }
}

export interface UrlParameters {
  [key: string]: any
}

export interface TypedUrlParameters {
  readonly playerQuery: string | void;
  readonly refreshToken: string | void;
}

export class RouterService extends BaseService {

  public static getOrgId(): string {
    return location.pathname.split('/').filter(x => x)[0]
  }

  lastUrlParameter: UrlParameters = {};
  lastRoutePath: string='';

  public readonly route   : Writable<any> = window.router.route;
  public readonly urlParameters: Readable<UrlParameters> = this.derived(this.route, (route: any, set:any) => {
    const nextParams = this.getRawParams();
    if (JSON.stringify(nextParams) !== JSON.stringify(this.lastUrlParameter)){
      this.lastUrlParameter = nextParams;
      set(nextParams);
    }
  });

  public readonly typedUrlParameters: Readable<TypedUrlParameters> = this.derived(this.urlParameters, (params: UrlParameters) => {
    return this.getTypedParams(params);
  });

  readonly orgId   : Writable<RouterData> = this.writable('org.id', RouterService.getOrgId());
  readonly realmId : Writable<RouterData> = this.writable('realm.id');
  readonly gameId: Writable<RouterData> = this.writable('game.id')

  init() {
    this.route.subscribe(this.onRouteChange);
  }

  public getRealmId(): string {
    return get(this.realmId);
  }

  @bind private onRouteChange(route: any = {}) {
    const { orgId, realmId, gameId, serviceId } = route.params || {};
    this.orgId.set(orgId);
    this.realmId.set(realmId);
    this.gameId.set(gameId);

    if (this.lastRoutePath == route.url){
      // dont reload state. ReplaceState triggers a route store edit.
      return;
    }
    this.lastRoutePath = route.url;

    // prefer any options set at route time. Otherwise, use the last option set.
    const currentParams = this.getTypedParams();
    if (!this.anyParams(currentParams) && this.lastUrlParameter){
      const serialized = this.serializeUrlParams(this.getTypedParams(this.lastUrlParameter));
      if (serialized != window.location.search) {
        history.replaceState(serialized, '', serialized);
      }
    }
  }

  getRawParams() : UrlParameters {
    const params = new URLSearchParams(window.location.search); // TODO: can refactor this to just be URLSearchParams
    const urlParams: UrlParameters = {};
    params.forEach( (value, key) => {
      urlParams[key] = value;
    });
    return urlParams;
  }

  getTypedParams(params: UrlParameters | void = undefined) : TypedUrlParameters {
    if (!params){
      params = this.getRawParams();
    }
    const typedParams: TypedUrlParameters = {
      playerQuery: params ? params['playerQuery'] : undefined,
      refreshToken: params ? params['refresh_token'] : undefined
    };
    return typedParams;
  }

  anyParams(params: TypedUrlParameters) {
    return Object.values(params).filter(value => value !== undefined).length > 0;
  }

  serializeUrlParams(params: TypedUrlParameters) : string {
    const playerDbid = params.playerQuery ? `playerQuery=${encodeURIComponent(params.playerQuery)}` : '';
    const searchString = `?${playerDbid}`;
    return searchString;
  }

  public writeUrlParams(paramGenerator: (currentParams: TypedUrlParameters) => TypedUrlParameters) {
    const currentParams = this.getTypedParams();
    const params = paramGenerator(currentParams);

    if (JSON.stringify(currentParams) == JSON.stringify(params)){
      return;
    }
    const searchString = this.serializeUrlParams(params);

    if (searchString == window.location.search) {
      return;
    }
    history.pushState(searchString, '', searchString);
  }
}

export default RouterService;
