import { PlayerStatusUpdate } from './PlayerStatusUpdate';

export type UpdatePlayerStatusRequest = { 
  playerId: bigint | string; 
  tournamentId: string; 
  update: PlayerStatusUpdate; 
};
