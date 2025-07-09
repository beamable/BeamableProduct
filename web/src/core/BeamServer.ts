import { BeamServerConfig } from '@/configs/BeamServerConfig';
import { BaseRequester } from '@/network/http/BaseRequester';
import { BeamRequester } from '@/network/http/BeamRequester';
import { BeamApi } from '@/core/BeamApi';
import { BeamBase } from '@/core/BeamBase';
import { HEADERS } from '@/constants';
import { AccountService } from '@/services/AccountService';
import { AnnouncementsService } from '@/services/AnnouncementsService';
import { AuthService } from '@/services/AuthService';
import { StatsService } from '@/services/StatsService';
import { BeamService } from '@/core/BeamService';
import { LeaderboardsService } from '@/services/LeaderboardsService';

/** The main class for interacting with the Beam Server SDK. */
export class BeamServer extends BeamBase {
  public readonly api: BeamApi;

  constructor(config: BeamServerConfig) {
    super(config);
    this.addOptionalDefaultHeader(HEADERS.UA, config.engine);
    this.addOptionalDefaultHeader(HEADERS.UA_VERSION, config.engineVersion);
    this.api = new BeamApi(this.requester);
    BeamService.attachServicesToServer(this);
  }

  protected createBeamRequester(config: BeamServerConfig): BeamRequester {
    const baseRequester = config.requester ?? new BaseRequester();
    return new BeamRequester({
      inner: baseRequester,
      useSignedRequest: true,
      pid: this.pid,
    });
  }
}

export interface BeamServer {
  /** High-level account helper built on top of `beam.api.accounts.*` endpoints. */
  account: (userId: string) => AccountService;
  /** High-level announcement helper built on top of `beam.api.announcements.*` endpoints. */
  announcements: (userId: string) => AnnouncementsService;
  /** High-level auth helper built on top of `beam.api.auth.*` endpoints. */
  auth: (userId: string) => AuthService;
  /** High-level leaderboards helper built on top of `beam.api.leaderboards.*` endpoints. */
  leaderboards: (userId: string) => LeaderboardsService;
  /** High-level stats helper built on top of `beam.api.stats.*` endpoints. */
  stats: (userId: string) => StatsService;
}
