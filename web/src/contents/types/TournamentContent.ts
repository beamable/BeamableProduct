import { ContentBase } from '@/contents/types/ContentBase';

export type TournamentContent = ContentBase<{
  name: {
    data: string;
  };
  anchorTimeUTC: {
    data: string;
  };
  cycleDuration: {
    data: string;
  };
  playerLimit: {
    data: number;
  };
  tiers: {
    data: {
      name: string;
      color: {
        r: number;
        g: number;
        b: number;
        a: number;
      };
    }[];
  };
  stagesPerTier: {
    data: number;
  };
  passiveDecayStages?: {
    data: number;
  };
  DefaultEntryColor: {
    data: {
      r: number;
      g: number;
      b: number;
      a: number;
    };
  };
  ChampionColor: {
    data: {
      r: number;
      g: number;
      b: number;
      a: number;
    };
  };
  stageChanges: {
    data: {
      minRank: number;
      maxRank: number;
      delta: number;
      color: {
        r: number;
        g: number;
        b: number;
        a: number;
      };
    }[];
  };
  rankRewards: {
    data: {
      name: string;
      tier: number;
      stageMin?: number;
      stageMax?: number;
      minRank?: number;
      maxRank?: number;
      currencyRewards: {
        amount: number;
        symbol: string; // currency reference
      }[];
    }[];
  };
  scoreRewards: {
    data: {
      name: string;
      tier: number;
      minScore: number;
      maxScore?: number;
      stageMin?: number;
      stageMax?: number;
      currencyRewards: {
        amount: number;
        symbol: string; // currency reference
      }[];
    }[];
  };
  groupRewards: {
    data: {
      rankRewards: {
        name: string;
        tier: number;
        stageMin?: number;
        stageMax?: number;
        minRank?: number;
        maxRank?: number;
        currencyRewards: {
          amount: number;
          symbol: string; // currency reference
        }[];
      }[];
    };
  };
}>;
