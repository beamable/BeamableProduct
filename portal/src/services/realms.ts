import { Readable, Writable, get } from '../lib/stores';
import { bind } from '../lib/decorators';
import BaseService from './base';
import { AuthRole } from './auth';

export interface Project {
  readonly created: number;
  readonly displayName: string;
  readonly name: string;
  readonly parent?: string;
  readonly children?: Array<string>;
  // TODO there are more keys in here... config, plan, secret, etc...
}
export interface Customer {
  readonly contact: string;
  readonly created: number;
  readonly cid: number;
  readonly name: string;
  readonly realms: Array<Project>;
}

export interface Organization {
  readonly realms: Array<Project>;
  readonly cid: number;
}

export interface Account {
  readonly email: string;
  readonly id: number;
  readonly roleString: string;
  readonly scopes: Array<string>;
  readonly availableRoles: Array<string>;
  readonly thirdPartyAssociations: Array<string>;
}

export interface RealmPromoteResponse {
  readonly pullPid: string;
  readonly scopes: Array<RealmPromoteScope>
}

export interface RealmPromoteScope {
  readonly name: string;
  readonly promotions: Array<RealmPromotable>
  readonly isPromotionAvailable: boolean; // Computed Property. PullChecksum != CurrentChecksum
}

export interface Promotable {
  readonly checksum: string;
  readonly createdAt: number;
  readonly createdAtDisplay: string;  // Computed Property
}

export interface RealmPromotable {
  readonly name: string;
  readonly id: string;
  readonly source: Promotable;
  readonly destination: Promotable;
  readonly isPromotionAvailable: boolean; // Computed Property. PullChecksum != CurrentChecksum
}

export interface ProjectView {
  projectName: string,
  pid: string,
  cid?: number,
  sharded?: boolean,
  parent?: string,
  children?: Array<string>,
  archived?: boolean
}

export type PromotableFeature = 'content' | 'microservices'

export class RealmsService extends BaseService {
  public readonly org: Writable<Organization> = this.writable('org', void 0, (set: any) => {
    this.fetchCustomer().then(set);
  });

  public readonly realms : Readable<any> = this.derived(this.org, (org: any)=> {
    return org?.realms;
  });

  public readonly cid : Readable<any> = this.derived(this.org, (org: any)=> {
    return org?.cid;
  });

  public readonly gameRealms: Readable<Array<ProjectView>> = this.derived([this.org, 'game.id'], (args: [any, string], set: any) => {
    if (args[0] && args[1]){
      set(undefined) // immediately invalidate any old realms.
      this.fetchGame(args[1]).then(set);
    }
  });

  private readonly membersRefreshStore: Writable<() => void> = this.writable('members.forcerefresh', () => {});
  public readonly members: Readable<Array<Account>> = this.derived(
    [this.org, this.app.auth.currentRole, this.membersRefreshStore],
    (args: [Organization, AuthRole, () => void], set:(next:Array<Account>) => void) => {
      const [org, role, refreshCallback] = args;
      if (role == 'admin' && org) {
        this.fetchMembers().then(set).then(refreshCallback);
      } else {
        set([]); // no members available.
        refreshCallback();
      }
  });

  public readonly realm : Readable<any> = this.derived([this.realms, 'realm.id'], ([ realms, realmId ]: any[]) => {
    return !realmId ? void 0 : realms?.find(
      (realm: any) => realm.name === realmId
    );
  });

  @bind public async setRole(id: string, role: string | null) {
    if (role === null) {
      await this.app.http.request(`/object/accounts/${id}/role`, null, 'delete');
    } else {
      await this.app.http.request(`/object/accounts/${id}/role`, { role }, 'put');
    }
    await this.forceRefreshMembers();
  }

  public async addUser(email: string) {
    const encodedEmail = encodeURIComponent(email);
    await this.app.http.request(`/basic/accounts/admin/admin-user?email=${encodedEmail}`, void 0, 'post');
    await this.forceRefreshMembers();
  }

  private convertToProject(projectView: ProjectView): Project {
    // readonly created: number;
    // readonly displayName: string;
    // readonly name: string;
    // readonly parent?: string;
    // readonly children?: Array<string>;
    return {
      name: projectView.pid,
      displayName: projectView.projectName,
      parent: projectView.parent,
      children: projectView.children,
      created: 0
    };
  }

  private async fetchCustomer() : Promise<Organization> {
    const { http } = this.app;
    const response = await http.request('/basic/realms/customer', void 0, 'get');
    const org: Organization = {
      realms: response.data.customer.projects.map(this.convertToProject),
      cid: response.data.customer.cid
    };
    return org;
  }

  public async fetchGame(pid: string): Promise<Array<ProjectView>> {
    const { http } = this.app;
    let resp = await http.request(`/basic/realms/game?rootPID=${pid}`, null, 'get');
    return resp.data.projects;
  }

  public async createRealm(cid: string, name: string, parent?: string) : Promise<void> {
    const { http } = this.app;
    const body:any = {
      cid: cid,
      name: name
    }
    if (parent){
      body.parent = parent;
    }
    await http.request('/basic/realms/project/beamable', body, 'post');
    this.fetchCustomer().then(data => this.org.set(data));
  }

  private timeSince(date: Date): string {

    var nowDate = new Date()
    var seconds = Math.floor((nowDate.getTime() - date.getTime()) / 1000);
  
    var interval = seconds / 31536000;
    
    if (interval > 1) {
      return Math.floor(interval) + " years";
    }
    interval = seconds / 2592000;
    if (interval > 1) {
      return Math.floor(interval) + " months";
    }
    interval = seconds / 86400;
    if (interval > 1) {
      return Math.floor(interval) + " days";
    }
    interval = seconds / 3600;
    if (interval > 1) {
      return Math.floor(interval) + " hours";
    }
    interval = seconds / 60;
    if (interval > 1) {
      return Math.floor(interval) + " minutes";
    }
    return Math.floor(seconds) + " seconds";
  }

  private upgradePromotableResponse(promotableResponse: RealmPromoteResponse): RealmPromoteResponse {

    const upgradePromotable: (_: Promotable)=> Promotable = promotable => {

      const date = new Date(promotable.createdAt)
      return {
        ...promotable,
        createdAtDisplay: promotable.createdAt < 1
          ? ''
          : `${date.toUTCString()} (${this.timeSince(date)} ago)`
      }
    }

    const upgradeScope: (_: RealmPromoteScope) => RealmPromoteScope = scope => {
      
      let upgrades = scope.promotions.map(promotion => ({
        ...promotion,
        name: promotion.id.length > 0 ? promotion.id : scope.name,
        source: upgradePromotable(promotion.source),
        destination: upgradePromotable(promotion.destination),
        isPromotionAvailable: promotion.source.checksum != promotion.destination.checksum
      }));
      if (upgrades.length == 0){
        upgrades.push({
          id: scope.name,
          name: scope.name,
          source: upgradePromotable({
            checksum: '',
            createdAt: 0,
            createdAtDisplay: ''
          }),
          destination: upgradePromotable({
            checksum: '',
            createdAt: 0,
            createdAtDisplay: ''
          }),
          isPromotionAvailable: false
        })
      }
      return {
        ...scope,
        promotions: upgrades,
        isPromotionAvailable: upgrades.filter(x => x.isPromotionAvailable).length > 0
      }
    }

    return {
      ...promotableResponse,
      scopes: promotableResponse.scopes.map(upgradeScope)
    };
  }

  public async getRealmPromotables(sourcePid: string, destinationPid: string): Promise<RealmPromoteResponse> {
    const { http } = this.app;
    const response = await http.request(`/basic/realms/promotion?sourcePid=${sourcePid}`, undefined, 'get', true, destinationPid);
    const data = response.data as RealmPromoteResponse
    return this.upgradePromotableResponse(data);
  }

  public async performRealmPromotion(sourcePid: string, destinationPid: string, promotions: Array<PromotableFeature>): Promise<RealmPromoteResponse> {
    const { http } = this.app;
    const body = {
      sourcePid: sourcePid,
      promotions,
    }
    const response = await http.request('/basic/realms/promotion', body, 'post', true, destinationPid);
    const data = response.data as RealmPromoteResponse
    return this.upgradePromotableResponse(data);
  }

  public async setProjectHierarchy(gameId: String, projects: Array<ProjectView>) : Promise<void> {
    const { http } = this.app;
    const body = {
      rootPID: gameId,
      projects: projects
    }
    await http.request('/basic/realms/game', body, 'put');
    this.fetchCustomer().then(data => this.org.set(data));
  }

  public async archiveProject(cid: String, pid: String): Promise<void> {
    const { http } = this.app;
    const body = {
      cid: Number(cid),
      pid,
    }
    await http.request('/basic/realms/project', body, 'delete');
    this.fetchCustomer().then(data => this.org.set(data));
  }

  public async createGame(gameName: String): Promise<void> {
    const { http } = this.app;
    const body = {
      gameName: gameName
    }
    await http.request('/basic/realms/game', body, 'post');
    this.fetchCustomer().then(data => this.org.set(data));
  }

  private async fetchMembers() : Promise<Array<Account>> {
    const { http } = this.app;
    const response = await http.request('/basic/accounts/admin/admin-users', void 0, 'get');
    const accounts: Array<Promise<Account>> = response.data.accounts.map(async (a:Account) => {
      const rolesResp = await http.request(`/object/accounts/${a.id}/available-roles`, void 0, 'get');
      const account: Account = {
        ...a,
        availableRoles: rolesResp.data.roles || []
      };
      return account;
    });

    return await Promise.all(accounts);
  }

  private async forceRefreshMembers(): Promise<void> {
    return new Promise(resolve => {
      this.membersRefreshStore.set(resolve)
    });
  }

}

export default RealmsService;
