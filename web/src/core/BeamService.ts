import { Beam } from '@/core/Beam';
import { AuthService } from '@/services/AuthService';
import { AccountService } from '@/services/AccountService';
import { AnnouncementsService } from '@/services/AnnouncementsService';
import { BeamServer } from '@/core/BeamServer';
import { StatsService } from '@/services/StatsService';

/**
 * Container for all Beamable services.
 * Provides higher-level abstractions and business logic on top of the `BeamApi` clients.
 * Access each service via `beam.<serviceName>.<method>`.
 */
export class BeamService {
  /** Attaches all services to a Beam Client instance. */
  static attachServices(beam: Beam) {
    beam.account = new AccountService({ api: beam.api, player: beam.player });
    beam.announcements = new AnnouncementsService({
      api: beam.api,
      player: beam.player,
    });
    beam.auth = new AuthService({ api: beam.api, player: beam.player });
    beam.stats = new StatsService({ api: beam.api, player: beam.player });
  }

  /** Attaches all services to a Beam Server instance. */
  static attachServicesToServer(beam: BeamServer) {
    beam.account = (playerId: string) =>
      new AccountService({ api: beam.api, playerId });
    beam.announcements = (playerId: string) =>
      new AnnouncementsService({
        api: beam.api,
        playerId,
      });
    beam.auth = (playerId: string) =>
      new AuthService({ api: beam.api, playerId });
    beam.stats = (playerId: string) =>
      new StatsService({ api: beam.api, playerId });
  }
}
