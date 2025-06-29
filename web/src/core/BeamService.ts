import { Beam } from '@/core/Beam';
import { AuthService } from '@/services/AuthService';
import { AccountService } from '@/services/AccountService';
import { AnnouncementsService } from '@/services/AnnouncementsService';

/**
 * Container for all Beamable services.
 * Provides higher-level abstractions and business logic on top of the `BeamApi` clients.
 * Access each service via `beam.<serviceName>.<method>`.
 */
export class BeamService {
  /** Attaches all services to a Beam instance. */
  static attachServices(beam: Beam) {
    beam.account = new AccountService({ api: beam.api });
    beam.announcements = new AnnouncementsService({
      api: beam.api,
      player: beam.player,
    });
    beam.auth = new AuthService({ api: beam.api });
  }
}
