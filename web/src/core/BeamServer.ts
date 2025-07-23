import { BeamServerConfig } from '@/configs/BeamServerConfig';
import { BaseRequester } from '@/network/http/BaseRequester';
import { BeamRequester } from '@/network/http/BeamRequester';
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
  constructor(config: BeamServerConfig) {
    super(config);
    this.addOptionalDefaultHeader(HEADERS.UA, config.engine);
    this.addOptionalDefaultHeader(HEADERS.UA_VERSION, config.engineVersion);
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
  /** High-level account helper built on top of accounts api endpoints. */
  account: (userId: string) => AccountService;
  /** High-level announcement helper built on top of announcements api endpoints. */
  announcements: (userId: string) => AnnouncementsService;
  /** High-level auth helper built on top of auth api endpoints. */
  auth: (userId: string) => AuthService;
  /** High-level leaderboards helper built on top of leaderboards api endpoints. */
  leaderboards: (userId: string) => LeaderboardsService;
  /** High-level stats helper built on top of stats api endpoints. */
  stats: (userId: string) => StatsService;
}
