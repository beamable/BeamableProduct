import Stores, { Store, Readable } from '../lib/stores';
import Services from 'services';

export class BaseService {
  protected readonly app: Services;

  constructor(protected readonly services: Services, ...args: any[]) {
    this.app = services;
    Stores.use(this.app, 'com.disruptorbeam.portal');
    setTimeout(()=> this.init(...args), 0);
  }

  protected init(...args: any[]) {}

  public writable(key: string, value?: any, start?: any) {
    return Stores.writable(this.app, key, value, start);
  }

  public persisted(key: string, value?: any, start?: any) {
    return Stores.persisted(this.app, key, value, start);
  }

  public derived(key: string, fn: any, value?: any): Readable;
  public derived(key: Store, fn: any, value?: any): Readable;
  public derived(keys: (string | Store)[], fn: any, value?: any): Readable;
  public derived(keys: any, fn: any, value?: any) {
    return Stores.derived(this.app, keys, fn, value);
  }
}

export default BaseService;
