import { Readable, Writable, get } from '../lib/stores';
import { bind } from '../lib/decorators';
import BaseService from './base';
import { Account } from './realms';
import RouterService from './router';

export interface UserLoginOptions {
  username: string,
  password: string
}

export interface RefreshTokenOptions {
  refresh_token: string
}

export type CreateTokenOptions = Partial<
  UserLoginOptions & RefreshTokenOptions & {
    grant_type: 'password' | 'refresh_token' | 'guest',
    customerScoped: boolean
  }
>;

export interface AuthToken {
  access_token  : string;
  refresh_token : string;
  expire_time   : number;
}

export type AuthRole = 'admin' | 'developer' | 'tester' | void

export class AuthService extends BaseService {
  public readonly token     : Writable<Partial<AuthToken>> = this.persisted(`auth.token.${RouterService.getOrgId()}`);
  public readonly username  : Writable<string>    = this.writable('auth.username');
  public readonly password  : Writable<string>    = this.writable('auth.password');
  public readonly code      : Writable<string>    = this.writable('auth.code');
  public readonly error     : Writable<string>    = this.writable('auth.error');
  public readonly isLoading : Writable<boolean>   = this.writable('auth.isLoading', false);
  private readonly isInitialized: Writable<boolean> = this.writable('auth.initialized', false);

  public readonly isLoggedIn : Readable<boolean> = this.derived(
    this.token, this.computeIsLoggedIn, false
  );

  public readonly account: Readable<Account|void> = this.derived(
    this.isLoggedIn,
    (isLoggedIn: boolean, set: (role: Account|void) => void) => {
      this.fetchMe(isLoggedIn).then(set);
    },
    undefined
  )

  public readonly currentRole : Readable<AuthRole> = this.derived(
    this.account,
    (account: Account, set: (role: AuthRole) => void) => {
      if (account){
        const roleString = account.roleString;
        if (roleString !== (roleString as AuthRole) ){
          console.error('role string is not valid', roleString);
        }
        set(roleString as AuthRole);
      } else {
        set(undefined);
      }
    },
    undefined
  )

  public readonly canLogIn : Readable<boolean> = this.derived('org.id', async (orgId: string, set: any) => {
    this.isLoading.set(true);
    if (!orgId) return; // wait for an ord id;

    try {
      await this.app.http.request('/basic/realms/is-customer', void 0, 'get', false);

      set(true);
    } catch(err) {
      set(false);
    }

    this.isLoading.set(false);
  });

  private readonly loginOptions : Readable<CreateTokenOptions> = this.derived(
    [ this.token, this.username, this.password ], this.computeLoginOptions
  );

  public isInit() : boolean {
    return get(this.isInitialized) == true;
  }

  public async onInit() : Promise<void> {
    return new Promise<void>(resolve => {
      // if we have already initialized, immediately resolve.
      if (get(this.isInitialized) == true){
        resolve();
        return;
      }

      // wait for the initialization to finish.
      let unsub: any;
      unsub = this.isInitialized.subscribe(next => {
        if (next === true){
          unsub();
          resolve();
        }
      });
    });
  }

  async init() {
    let refresh_token = new URLSearchParams(location.search).get('refresh_token');
    if (refresh_token == null) {
      refresh_token = get(this.token)?.refresh_token
    }

    if (refresh_token) {
      this.isLoading.set(true);
      try {
        this.token.set(await this.createToken({ grant_type: 'refresh_token', refresh_token }));

      } catch(err) {
        console.error(err)
      } finally {
        this.isLoading.set(false);
      }
    }
    this.token.subscribe(token => {
      if (token){
        this.isInitialized.set(true);
      }
    })
    this.currentRole.subscribe(_ => {}); // keep a subscription open make it more likely other callers don't have to wait.
  }

  @bind public async logout() {
    this.isInitialized.set(false);
    this.token.set();
  }

  @bind public async login() {

    this.isLoading.set(true);

    try {
      this.token.set(await this.createToken());
    } catch(error) {
      this.logout();
      console.log(error);
    }

    this.isLoading.set(false);
  }

  @bind public async forgotPassword() {
    const email = encodeURIComponent(get(this.username));
    try {
      await this.app.http.request(`/basic/accounts/password-update/init?email=${email}&codeType=pin`, void 0, 'post', false);
      this.password.set('');
    } catch (ex){
      this.error.set(ex.message);
      throw ex;
    }
  }

  @bind public async finishForgotPassword() {
    const password = encodeURIComponent(get(this.password));
    const code = encodeURIComponent(get(this.code));
    const email = encodeURIComponent(get(this.username));
    try {
      await this.app.http.request(`/basic/accounts/password-update/confirm?code=${code}&newPassword=${password}&email=${email}`, void 0, 'post', false);
      await this.login();
    } catch (ex){
      this.error.set(ex.message);
      throw ex;
    }
  }

  public async checkAccess(roles: Array<AuthRole> = ['admin']) : Promise<boolean> {
    let unsub;
    const promise = new Promise<boolean>(resolve => {
        unsub = this.currentRole.subscribe(value => {
            // XXX: A simple get() won't work here, because the value of the store is itself async.
            //      if there is not a value, just wait for the next time there is a value.
            if (value){
              const hasAccess = roles.indexOf(value) > -1;
              resolve(hasAccess);
            }
        });
    });
    promise.finally(unsub);
    return promise;
  }

  @bind private computeIsLoggedIn(token: AuthToken): boolean {
    if (!token) return false;
    if (token.expire_time && token.expire_time < Date.now()) {
      this.login();
      return false;
    }
    return true;
  }

  private async fetchMe(isLoggedIn: boolean): Promise<Account|void> {
    if (!isLoggedIn) return undefined;

    const accountView = await this.app.http.request(`/basic/accounts/admin/me`, void 0, 'get');
    return accountView.data as Account;
  }


  @bind private clearLoginInfo(): void {
    this.username.set('');
    this.password.set('');
    this.code.set('');
    this.error.set('');
  }

  @bind private computeLoginOptions(
    [ token, username, password ]: [ AuthToken, string, string ]
  ): CreateTokenOptions {
    if (username && password) return {
      grant_type: 'password', username, password
    };

    if (token && token.refresh_token) return {
      grant_type: 'refresh_token', refresh_token: token.refresh_token
    };

    return { grant_type: 'guest' };
  }

  private async createToken(
    options: CreateTokenOptions = get(this.loginOptions)
  ): Promise<AuthToken> {
    const request_time = Date.now();
    try {
      // get a customer scoped token here.
      options.customerScoped = true;
      const { data } = await this.app.http.request('/basic/auth/token', options, 'post', false);

      this.clearLoginInfo();
      if (data.expires_in) {
        data.expire_time = request_time + data.expires_in;
      }

      return data;
    } catch (ex){
      this.error.set(ex.message);
      throw ex;
    }

  }
}

export default AuthService;
