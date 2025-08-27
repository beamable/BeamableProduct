import { PlayerService } from '@/services/PlayerService';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { BeamBase } from '@/core/BeamBase';

export interface ApiServiceProps {
  beam: BeamBase;
  getPlayer?: () => PlayerService;
}

export type ApiServiceCtor<T> = new (props: ApiServiceProps) => T;

export abstract class ApiService {
  protected readonly requester: HttpRequester;
  protected readonly beam: BeamBase;
  private readonly _getPlayer?: () => PlayerService;
  private _userId: string | undefined;

  protected constructor(props: ApiServiceProps) {
    this.beam = props.beam;
    this.requester = props.beam.requester;
    this._getPlayer = props.getPlayer;
  }

  /**
   * Retrieves the player service instance.
   * @remarks This is only available in the client SDK.
   */
  protected get player(): PlayerService | undefined {
    return this._getPlayer?.();
  }

  /**
   * @internal
   * Sets the user ID for the current player or admin used in server context.
   */
  set userId(id: string) {
    this._userId = id;
  }

  /**
   * @internal
   * Gets the user ID for the current player or admin used in server context.
   */
  get userId(): string {
    return this._userId ?? '';
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

  /** @internal */
  abstract get serviceName(): string;
}
