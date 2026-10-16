/**
 * Where a grant is in its lifecycle. Closed: the platform owns this state machine, not a store.
 *
 * Distinct from `available` — a grant can be `Granted` and still not actionable, which is exactly
 * what an offer gated on a condition the player has not met yet looks like.
 */
export type CampaignOfferState = "Granted" | "Redeemed" | "Revoked" | "Expired";
