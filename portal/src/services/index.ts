import * as svelte from 'svelte';

import HttpService from './http';
import AuthService from './auth';
import RealmsService from './realms';
import RouterService from './router';
import PlayersService from './players';
import { ContentService } from './content';
import { InventoryService } from './inventory';
import { AnnouncementsService } from './announcements';
import { LeaderboardService } from './leaderboards';
import { CloudSavingService } from './cloudsaving';
import { PaymentsService } from './payments';
import { TournamentService } from './tournaments';
import { Microservices } from './microservices';

// Essentially creating a namespace around shared services
export class Services {
  readonly router = new RouterService(this);
  readonly http = new HttpService(this);
  readonly auth = new AuthService(this);
  readonly realms = new RealmsService(this);
  readonly players = new PlayersService(this);
  readonly content = new ContentService(this);
  readonly inventory = new InventoryService(this);
  readonly announcements = new AnnouncementsService(this);
  readonly leaderboards = new LeaderboardService(this);
  readonly cloudsaving = new CloudSavingService(this);
  readonly payments = new PaymentsService(this);
  readonly tournaments = new TournamentService(this);
  readonly microservices = new Microservices(this);

  init() {
    (<any>window).services = this;
  }

  public static get(): Services {
    let context = svelte.getContext(Services);

    if (!context) {
      context = new Services();
      svelte.setContext(Services, context);
    }

    return context;
  }
}

export const { get: getServices } = Services;
export default Services;
