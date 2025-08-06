import { SERVICE_KEYS } from '../types';
import { BeamBase } from '@/core/BeamBase';
import { BeamBaseConfig } from '@/configs/BeamBaseConfig';

/** A mixin that adds client-side services (e.g. `beam.account`) to the Beam client SDK. */
export function ClientServicesMixin(Base: typeof BeamBase) {
  abstract class ClientServiceContainer extends Base {
    protected constructor(config: BeamBaseConfig) {
      super(config);
      for (const key of SERVICE_KEYS) {
        Object.defineProperty(this, key, {
          get: () => {
            if (!this.clientServices[key]) this.throwServiceUnavailable(key);
            return this.clientServices[key];
          },
        });
      }
    }
  }

  return ClientServiceContainer as typeof BeamBase;
}

/** A mixin that adds server-side services (e.g. `beamServer.account(userId)`) to the Beam server SDK. */
export function ServerServicesMixin(Base: typeof BeamBase) {
  abstract class ServerServiceContainer extends Base {
    protected constructor(config: BeamBaseConfig) {
      super(config);
      for (const key of SERVICE_KEYS) {
        Object.defineProperty(this, key, {
          value: (userId: string) => {
            if (!this.serverServices[key])
              this.throwServiceUnavailable(key, true);
            return this.serverServices[key](userId);
          },
        });
      }
    }
  }

  return ServerServiceContainer as typeof BeamBase;
}
