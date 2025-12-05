import { ContentBase } from '@/contents/types/ContentBase';

export type GameTypeContent = ContentBase<{
  teams: {
    data: {
      name: string;
      maxPlayers: number;
      minPlayers?: number;
    }[];
  };
  numericRules: {
    data: {
      property: string;
      maxDelta: number;
      Default?: number;
    }[];
  };
  stringRules: {
    data: {
      property: string;
      value: string;
    }[];
  };
  waitAfterMinReachedSecs?: {
    data: number;
  };
  maxWaitDurationSecs?: {
    data: number;
  };
  matchingIntervalSecs?: {
    data: number;
  };
  newFederatedGameServerNamespace?: {
    data: {
      name: string;
    };
  };
  federatedGameServerNamespace?: {
    data: string;
  };
  leaderboardUpdates: {
    data: {
      leaderboard: string; // leaderboard reference
      scoringAlgorithm: {
        algorithm: string;
        options: {
          key: string;
          value: string;
        }[];
      };
    }[];
  };
  rewards: {
    data: {
      startRank: number;
      endRank: number;
      rewards: {
        type: string;
        name: string; // currency reference
        amount: number;
      }[];
    }[];
  };
  isFirstTime?: {
    data: boolean;
  };
}>;
