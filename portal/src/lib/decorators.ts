import { AuthRole } from '../services/auth';
import { DataUnavailable, DataUnavailableValue } from '../services/http';


const {
  defineProperty
} = Object;

export function lazy(target: any, propertyName: string | symbol, {
  get, set, value: init = get,
  configurable, enumerable, writable
}: TypedPropertyDescriptor<any>) {
  if (typeof init !== 'function' && typeof set !== 'function') {
    throw new Error('@lazy: Decorator may only be applied to accessor or method class members.');
  }

  function accessor(this: any) {
    const value = init?.apply(this, arguments);

    defineProperty(this, propertyName, {
      value, configurable: true, writable: true
    });

    if (set && !init) {
      (<any> set).apply(this, arguments);
    }

    defineProperty(this, propertyName, {
      configurable, writable
    });

    return value;
  };

  return {
    enumerable,
    configurable: true,
    get: get ? accessor : void 0,
    set: set ? accessor : void 0
  };
}

export function bind(target: any, propertyName: string | symbol, {
  value: method, ...desc
}: TypedPropertyDescriptor<any>) {
  if (typeof method !== 'function') {
    throw new Error('@bind: Decorator may only be applied to method class members.');
  }

  return lazy(target, propertyName, { ...desc, get() {
    return method.bind(this);
  }});
}

export function roleGuard(roles: Array<AuthRole>) {
  return function (target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    const method = descriptor.value; // references the method being decorated
    descriptor.value = async function(...args: any[]):Promise<any> {
      const { auth } = (<any>this).app;
      if (!auth) 
        throw new Error('@roleGuard: Decorator may only be applied to methods of services');

      const hasAccess: boolean = await auth.checkAccess(roles);
      if (hasAccess) {
        // run the function...
        return method.apply(this, args);
      } else {
        console.error('user has insufficient permissions. Required', roles)
        return Promise.reject({
          error: 'invalidRole',
          requiredRoles: roles,
        }).catch(_ => {
          // XXX: the default catch() impl will throw an error in the log. We don't want that.
        });
      }
    };
  }
}

export function networkFallback<T>(
  fallbackData: T|DataUnavailable=DataUnavailableValue, 
  shouldFallback:(status:number)=>boolean = (status => status >= 500)) {
  return function (target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    const method = descriptor.value; 
    descriptor.value = async function(...args: any[]):Promise<T|DataUnavailable> {
      try {
        return await method.apply(this, args);
      } catch (error){
        if (error && shouldFallback(error.status) ){
          return fallbackData;
        } else {
          throw error;
        }
      }
    }
  }
}
