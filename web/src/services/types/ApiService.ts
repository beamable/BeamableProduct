import { PlayerService } from '@/services/PlayerService';
import { HttpRequester } from '@/network/http/types/HttpRequester';

export interface ApiServiceProps {
  requester: HttpRequester;
  player?: PlayerService;
  userId?: string;
}

export abstract class ApiService {
  protected readonly requester: HttpRequester;
  protected readonly player?: PlayerService;
  protected readonly userId: string;

  protected constructor(props: ApiServiceProps) {
    this.requester = props.requester;
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
