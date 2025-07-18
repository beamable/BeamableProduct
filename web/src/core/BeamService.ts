import { Beam } from '@/core/Beam';
import { AuthService } from '@/services/AuthService';
import { AccountService } from '@/services/AccountService';
import { AnnouncementsService } from '@/services/AnnouncementsService';
import { BeamServer } from '@/core/BeamServer';
import { StatsService } from '@/services/StatsService';
import { LeaderboardsService } from '@/services/LeaderboardsService';

/**
 * Container for all Beamable services.
 * Provides higher-level abstractions and business logic on top of the beamable api endpoints.
 * Access each service via `beam.<serviceName>.<method>`.
 */
export class BeamService {
  /** Attaches all services to a Beam Client instance. */
  static attachServices(beam: Beam) {
    beam.account = new AccountService({
      requester: beam.requester,
      player: beam.player,
    });
    beam.announcements = new AnnouncementsService({
      requester: beam.requester,
      player: beam.player,
    });
    beam.auth = new AuthService({
      requester: beam.requester,
      player: beam.player,
    });
    beam.leaderboards = new LeaderboardsService({
      requester: beam.requester,
      player: beam.player,
    });
    beam.stats = new StatsService({
      requester: beam.requester,
      player: beam.player,
    });
  }

  /** Attaches all services to a Beam Server instance. */
  static attachServicesToServer(beam: BeamServer) {
    beam.account = (userId) =>
      new AccountService({ requester: beam.requester, userId });
    beam.announcements = (userId) =>
      new AnnouncementsService({ requester: beam.requester, userId });
    beam.auth = (userId) =>
      new AuthService({ requester: beam.requester, userId });
    beam.leaderboards = (userId) =>
      new LeaderboardsService({ requester: beam.requester, userId });
    beam.stats = (userId) =>
      new StatsService({ requester: beam.requester, userId });
  }
}
