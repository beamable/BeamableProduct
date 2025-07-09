import { BeamApi } from '@/core/BeamApi';
import { PlayerService } from '@/services/PlayerService';

export interface ApiServiceProps {
  api: BeamApi;
  player?: PlayerService;
  userId?: string;
}

export abstract class ApiService {
  protected readonly api: BeamApi;
  protected readonly player?: PlayerService;
  protected readonly userId: string;

  protected constructor(props: ApiServiceProps) {
    this.api = props.api;
    this.player = props.player;
    this.userId = props.userId ?? '';
  }

  /**
   * Gets the account ID for the current player or admin.
   * @remarks This is used to identify the player or admin in API requests.
   */
  protected get accountId(): string {
    if (this.player?.id) {
      return this.player.id;
    }
    return this.userId;
  }
}
