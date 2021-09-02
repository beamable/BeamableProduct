import * as svelteStores from 'svelte/store';
import storage from './web-storage';

export type Readable<T = any> = svelteStores.Readable<T | void>;
export type Writable<T = any> = svelteStores.Writable<T | void>;
export type Store<T = any>    = Writable<T> | Readable<T>;
export type StoreMap          = Map<string, Store>;

const STORES    : unique symbol  = Symbol('__STORES__');
const PERSISTED : WeakSet<Store> = new WeakSet();

export class Stores {
  private readonly stores : StoreMap = new Map();
  private readonly prefix : string   = '';

  constructor(prefix?: string) {
    if (prefix) {
      this.prefix = `${prefix}.`;
    }
  }

  private get(key: string) {
    return this.stores.get(`${this.prefix}${key}`);
  }

  private set(key: string, store: Store) {
    this.stores.set(`${this.prefix}${key}`, store);
    return store;
  }

  private load(key: string, value?: any) {
    return storage.load(`${this.prefix}${key}`, value);
  }

  private save(key: string, value?: any) {
    return storage.save(`${this.prefix}${key}`, value);
  }

  public writable(key: string, value?: any, start?: any) {
    return this.get(key) || this.set(key, svelteStores.writable(value, start));
  }

  public persisted(key: string, value?: any, start?: any) {
    const store = <Writable> this.writable(key, value, start);

    if (!PERSISTED.has(store)) {
      PERSISTED.add(store);
      store.set(this.load(key, value));
      store.subscribe(this.save.bind(this, key));
    }

    return store;
  }

  public derived(key: string | Store, fn: any, value?: any): Store;
  public derived(keys: (string | Store)[], fn: any, value?: any): Store;
  public derived(keys: any, fn: any, value?: any) {
    if (!Array.isArray(keys)) {
      keys = typeof keys === 'string' ? this.writable(keys) : keys;
    } else {
      const [ head, ...rest ] = keys.map(key => (
        typeof key === 'string' ? this.writable(key) : key
      ));

      keys = [ head, ...rest ];
    }

    return svelteStores.derived(keys, fn, value);
  }

  /* Static members */

  private static get(target: any, ...args: any[]) {
    let { [STORES]: stores } = target;

    if (!stores) {
      stores = (target[STORES] = new Stores(...args));
    }

    return stores;
  }

  public static use(target: any, ...args: any[]) {
    Stores.get(target, ...args);
    return target;
  }

  public static writable(target: any, key: string, value?: any, start?: any) {
    return Stores.get(target).writable(key, value, start);
  }

  public static persisted(target: any, key: string, value?: any, start?: any) {
    return Stores.get(target).persisted(key, value, start);
  }

  public static derived(target: any, keys: (string | Store)[], fn: any, value?: any) {
    return Stores.get(target).derived(keys, fn, value);
  }
}

export const { use, writable, persisted, derived } = Stores;

export function UseStores(...args: any[]) {
  return (target: any) => {
    use(target.prototype, ...args);
    return target;
  };
}

export { get } from 'svelte/store';

export default Stores;
