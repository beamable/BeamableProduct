export type CampaignOfferAmount = {
  /** How many. Always a real quantity — a loot roll is one roll, not zero of a prize. */
  amount?: bigint | string;
  imageUrl?: string | null;
  properties?: Record<string, string>;
  /** The store's own reference. Opaque: only the store that issued it can interpret it. */
  symbol: string;
  title?: string | null;
  /** Open string. Render what you know and fall back to `title` — never switch exhaustively. */
  type: string;
};
