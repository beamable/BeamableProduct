import { BeamApi } from '@/core/BeamApi';
import { PlayerService } from '@/services/PlayerService';
import { BeamError } from '@/constants/Errors';

export interface ApiServiceProps {
  api: BeamApi;
  player?: PlayerService;
  playerId?: string;
}

export abstract class ApiService {
  protected readonly api: BeamApi;
  protected readonly player?: PlayerService;
  protected readonly playerId?: string;

  protected constructor(props: ApiServiceProps) {
    this.api = props.api;
    this.player = props.player;
    this.playerId = props.playerId;
  }

  protected get playerIdOrThrow(): string {
    if (this.player?.id) {
      return this.player.id;
    } else if (this.playerId) {
      return this.playerId;
    }
    throw new BeamError(
      'Player ID is not set. Please provide an instance of PlayerService or playerId string.',
    );
  }
}
