import { Beam } from '@/core/Beam';
import { AuthService } from '@/services/AuthService';
import { AccountService } from '@/services/AccountService';

/**
 * Container for all Beamable services.
 * Provides higher-level abstractions and business logic on top of the `BeamApi` clients.
 * Access each service via `beam.<serviceName>.<method>`.
 */
export class BeamService {
  /**
   * Attaches all services to a Beam instance.
   * @param {Beam} beam - The Beam instance to attach services to.
   */
  static attachServices(beam: Beam) {
    beam.account = new AccountService(beam.api);
    beam.auth = new AuthService(beam.api);
  }
}
