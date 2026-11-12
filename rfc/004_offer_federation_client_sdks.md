# 004 — Offer Federation: handoff for the Unity & Unreal client SDKs

**Status:** the offer registry, the gateway routes, the three Beamable providers, the Portal authoring
extensions, the web SDK (`beam.offers`) and the React Native sample are built. Unity and Unreal have no
client surface, and cannot reach the gateway at all (§3).

**Purpose:** the input to a planning session for the Unity and Unreal half. It records what a game
client needs, what exists to copy, and the one structural blocker that has to be decided first.

`docs/offer-federation.md` is the system view; the `beam-offer-federation` MCP skill holds the contract,
routes and limits.

---

## 1. What the feature is

A campaign lane can attach **offers** to the message it sends; an operator can also grant one by hand.
Beamable's offer registry records each grant, and the player redeems it in-game. Which provider delivers
an offer is a **federation** — `IFederatedVirtualOffer<T>`, an extension point, not a Beamable feature:

```
Portal (author) → campaign graph → campaign runtime → offer registry ──GrantOffers──▶ provider microservice
                                                          ▲
game client ── GET /api/offers, POST /api/offers/redeem ──┘ ──RedeemOffer──▶ provider microservice
```

Beamable ships three providers: `beamable_virtual_store` (a commerce listing, bought with soft currency on
redeem), `beamable_reward` (free currency and items) and `beamable_badge` (a free badge, written as a
public stat). A game with its own virtual economy registers its own federation id and is treated
identically. Real-money offers are a different federation, which does not exist yet.

---

## 2. What a client needs

Two routes, both client-callable (`BeamableGateway/Controllers/OfferController.cs`, under `/api`):

```jsonc
// GET /api/offers?playerId=1234[&states=Granted&states=Redeemed][&federationId=…][&cursor=…]
// → { "offers": [ {
//      grantId, federationId, offerId, state,            // OfferGrantState
//      grantedAtUnixSeconds, expiresAtUnixSeconds,
//      offer: { offerId, title, description, imageUrl, priceLabel,
//               cost: [ { type, symbol, amount, title?, imageUrl?, properties } ],
//               rewards: [ … same shape … ], localizations, tags, properties },
//      available, unavailableReasons: [ { code, message, detail?, properties } ] } ],
//    "next": "…" }

// POST /api/offers/redeem
{ "playerId": "1234", "grantId": "4f1c…", "params": {} }
// → { grantId, success, status?, message?, state?, reason? }
```

- **One list, every provider.** `federationId` is an optional filter. `states` defaults to `Granted` and
  `Claiming`; history (`Redeemed`, `Revoked`, `Expired`) is asked for explicitly. Pages are newest first;
  `next` is the cursor.
- **A redeem needs only the grant id.** The registry knows the provider. It is idempotent: a repeat is
  the same delivery, and a redeemed grant answers `success: true` again.
- **A refusal is a 200** with `success: false` and `status` `unknown-grant`, `unavailable` (with
  `reason` when the campaign's gate is closed) or `pending` (the delivery is unresolved or in progress —
  retry later).
- **Self-only for a player**: another player's id is 403 unless the token is privileged. Per-player rate
  limits answer 429.

The web SDK is the reference shape (`web/src/services/OfferService.ts`, `beam.offers`):

```ts
const { offers, next } = await beam.offers.getOffers({ states, federationId, cursor });
const res = await beam.offers.redeem(grantId, params);
```

---

## 3. ⚠️ The structural blocker — decide this first

**The gateway's `/api/*` routes do not exist in the Unity or Unreal generated APIs.**

- Unity's `Common/Runtime/OpenApi/Models.gs.cs` and Unreal's
  `Plugins/BeamableCore/Source/BeamableCore/Public/AutoGen/SubSystems/*` are generated from the
  **`/basic/*` OpenAPI documents** (`cli/cli/openapi/*.oapi.json`, downloaded via `SwaggerService`).
- `offers`, `message-rail` and `campaigns` are **C# gateway controllers**. They appear only in the web
  SDK's generated API (`web/src/__generated__/apis/`).

A plan must pick one of:

1. **Feed the gateway's OpenAPI into Unity/Unreal codegen.** Reusable — it unblocks the message rail,
   campaigns and every future gateway controller — but it is a codegen-pipeline change and the larger
   piece of work.
2. **Hand-write the two calls** in each SDK against the raw requester. Fast, but sets a precedent of
   hand-rolled gateway calls that will need unpicking.

The same gap blocks the message rail: neither SDK can opt a player in to push, email or in-game today.
Option 1 fixes both; option 2 fixes offers only.

---

## 4. The player flow

1. A campaign sends a message through a rail. The recipient's data carries the reserved key
   **`beam_offer_grants`** (`OfferContract.GrantsKey`): every grant id of that send, comma-separated. A
   client reads it to deep-link straight to what the player was given. The push rail passes it through;
   the in-game rail (Beamable mail) cannot carry it.
2. The client redeems a grant id from the message, or lists the player's offers and redeems from there.
3. **Redeeming delivers.** For the store it is the purchase (commerce debits the price and credits the
   payout in one transaction); for a reward it is one inventory update; for a badge a stat write. The
   response carries none of what moved, so a client that needs to know reads the inventory or stat back.

---

## 5. Rules the client SDKs inherit

1. **Never branch on the federation id**, and never hardcode one. It is at most a filter.
2. **The offer id is opaque.** Do not parse it, show it as a name, or key anything durable on it.
3. **`beam_offer_grants` is reserved.** A client reads it; a rail never emits it.
4. **Never cache the list across a session.** Expiry and the campaign's gate are evaluated on every
   read; a locked offer unlocks on a later read with no new message.
5. **`available` is not `state`.** A `Granted` offer can be unavailable; `unavailableReasons` says why.
   Prefer a reason's `properties` (`attribute`, `target`, `current`) over its `message` for a gate.
6. **`rewards` is disclosure, not a receipt**, and its `type` is an open string: render what you know,
   fall back to `title`/`symbol`.
7. **Surface the server's `message` on a refusal.** The likeliest failures — insufficient funds, a
   listing outside its schedule, unpublished payout content — are named only there.

---

## 6. Known gaps

- **No offer or message-rail client in Unity or Unreal** (§3).
- **The in-game rail cannot carry `beam_offer_grants`.** An in-game recipient finds the grant through the
  list.
- **Real money is out of scope.** Unity's IAP flow (`UnityBeamablePurchaser`, `PaymentService`) belongs to
  the real-money federation, not this one.
- **The permission matrix is a hardcoded, fail-closed dictionary**
  (`BeamableGateway/Security/Authorization/PermissionAuthorizationHandler.cs`). The operator routes use
  the `CampaignOffer` resource; a new gateway route needs its own entry.

---

## 7. Suggested shape for the next plan

1. **Decide §3** — codegen pipeline vs. hand-written calls.
2. Add an `Offers` service to each SDK with `GetOffers(states?, federationId?, cursor?)` and
   `Redeem(grantId, params?)`, modelled on `web/src/services/OfferService.ts`.
3. A helper that reads `beam_offer_grants` from a received push and redeems each grant id.
4. A sample scene / map: list offers → redeem → see the inventory change.
5. Decide whether the message rail's opt-in rides along, since §3 option 1 unblocks it too and a player
   who cannot opt in never receives the message carrying the offer.
