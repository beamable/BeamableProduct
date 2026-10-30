# The virtual offer federation, end to end

A reading guide to the **offer** system: what it is, which repo owns which piece, how a grant and a
purchase actually flow, and where to look when something breaks.

> **This is the *virtual* offer federation** — offers a player buys with soft currency, or is simply
> given. `IFederatedVirtualOffer`, providers `beamable_virtual_store`, `beamable_reward` and
> `beamable_badge`. A price on this contract is a currency symbol and an amount; there is no real-money
> price, no platform product id and no payment settlement callback.
>
> **Real money is a separate federation, and it does not exist yet.** It needs platform product ids, a
> native purchase flow, receipt verification and a settlement callback from outside Beamable. If you are
> looking for the real-money path, it is not here.

This is the *system* view. Neighbours that are not repeated here:

- **`agentic-portal/guides/creating-offer-providers.md`** — how to ship your own provider.
- **`beam-offer-federation`** (MCP skill, `cli/Docs/SkillTemplates/beam-offer-federation.md.scriban`)
  — the contract, routes, limits and defaults, for an agent implementing a provider.
- **`BeamableProduct/rfc/004_offer_federation_client_sdks.md`** — what Unity and Unreal need to reach
  the player routes.

---

## 1. The one idea

A campaign lane can attach **offers** to the message it sends. The operator authors them in the Portal,
Beamable **grants** each one to each recipient as the send goes out, and the player redeems it in-game.
An operator can also grant an offer by hand.

Three sentences carry almost the whole design:

> **Beamable is the authority on every grant.** The offer registry (`offer_grants`, in the shared
> `global` database) decides whether a grant exists, is visible, was redeemed or ended. A provider needs
> no storage for that, but may keep state of its own under each `grantId`, up to managing the whole
> grant in its microstorage.

> **"Which provider" is a federation, not a feature.** `beamable_virtual_store` (a commerce listing,
> bought on redeem), `beamable_reward` (free currency and items) and `beamable_badge` (a free badge,
> written as a stat) are the providers Beamable ships. A game with its own virtual economy implements the
> same interface under its own id and is treated identically by the gateway, the registry, the campaign
> runtime and the Portal.

> **A catalog is not a federation concern.** Listing and describing offers has one consumer — the
> provider's own Portal extension — so a catalog is an ordinary microservice, and a provider without one
> authors its offers inline in its extension.

Two rules follow: **nothing outside a provider branches on the federation id**, and **the offer id is
opaque** — only the provider that minted it interprets it. Do not parse it, show it as a name, or key
anything durable on its shape.

### What a grant is

A grant is a registry row: "this player may take this offer, from this provider, until this time". It
is not the goods; redeeming it is what delivers them.

```
Pending ─▶ Granted ─▶ Claiming ─Settled─▶ Redeemed
   │          │  ▲         │
   │          │  └─Refused─┘     (Ambiguous: stays Claiming; asked again on the redelivery curve)
   │          ├─ operator revoke / forfeit ─▶ Revoked
   │          └─ expiresAt or its origin closed ─▶ reads Expired
   └─ provider refused / exclusive group incomplete / send gave up / version ended ─▶ Failed
```

- **Pending is invisible.** A grant is written Pending before its provider is asked and becomes Granted
  only once whatever tells the player about it is durable — for a campaign, the staged message. A
  grant whose message never went out is never shown.
- **Expiry is folded in on read.** A Granted grant past its `expiresAt`, or whose origin was closed,
  reads `Expired` the first time anyone looks. Never cache the list across a session.
- **Nothing is deleted.** Redeemed, revoked and expired grants stay; the list shows only usable ones by
  default and the rest on request.
- **A grant id is deterministic**: `hash(cid, pid, origin, originKey, entryId)`. A retried hand-out
  lands on the same grants, and the provider answers them as the same grants.

---

## 2. Where everything lives

| Repo | Owns |
|---|---|
| **BeamableProduct** | The **contract** (the DTOs every side shares), the web/JS SDK, the React Native sample |
| **BeamableAPI** | The **offer registry** and **gateway** routes, the **campaign runtime** (grants on send), and a mirror of the contract |
| **agentic-portal** | The three Beamable providers, the shared provider code, the catalog service, the Portal authoring extensions |
| **BeamableBackend** | The Scala platform: `commerce`, `inventory`, `stats`, `content`. Knows nothing about offers |

### The files that matter

**Contract**

- `BeamableProduct/cli/beamable.common/Runtime/Federation/FederatedVirtualOffer.cs` —
  `IFederatedVirtualOffer<T>`: `GrantOffers(grants)`, `RedeemOffer(grant, params)`,
  `RevokeOffer(revokes)`, `GetOffers(grants)`, routed at `{federationId}/<Method>`. DTOs: `OfferItem`,
  `OfferAmount`, `OfferReason`, `OfferGrantItem`, `OfferGrantRef`, `OfferGrantBatchResponse` /
  `OfferGrantResponse`, `OfferRedeemResponse` (`OfferRedeemOutcome` by name), `OfferRevokeBatchResponse` /
  `OfferRevokeResponse`, `OfferAvailability`;
  `OfferContract` holds every wire string. How the registry treats a provider (availability rules,
  revoke lead, redelivery, batch sizes) is realm config `offers|{federationId}.<setting>`, not part of
  the contract: the backend reads it into its own `OfferProviderSettings` on each use.
- `BeamableAPI/BeamableShared/Federation/Offer/OfferMessages.cs` — the gateway's mirror.
  `BeamableShared.Tests/Tests/Federation/OfferWireShapeTest.cs` asserts the exact JSON, so a field added
  on one side only breaks it on purpose.

**Registry and gateway — BeamableAPI**

- `BeamableShared/Services/Offer/OfferRegistry.cs`, `OfferService.cs` — grant states, `GrantMany`,
  `Confirm`, `Fail`, `Claim` (compare-and-set, with an exclusive group's anchor), `Settle`, `Release`,
  `Revoke`, `ExpireOrigin`, the paged `GetOffers`, `Redeem`, `Availability`.
- `BeamableShared/Federation/Offer/OfferFederation.cs` — provider discovery (kept
  `Federation:OfferDiscoveryCacheMs`, 5 s, per realm) and calls.
- `BeamableShared/Services/Offer/OfferGate.cs` — the campaign's condition, evaluated by the gateway.
- `BeamableGateway/Controllers/OfferController.cs` — the routes (§3).

**Campaign runtime — BeamableAPI**

- `BeamableShared/Services/Campaigns/Runtime/CampaignOfferStep.cs` — a send's hand-out: checks the
  version still runs, grants through the registry, confirms after the message is staged (re-checking the
  version), fails the grants of a send that gives up.
- `BeamableShared/Services/Campaigns/Runtime/CampaignDrainMonitor.cs` — closes a version's offers
  (`ExpireOrigin`) before it goes Inactive.

**Providers — agentic-portal `bundles/features/player-engagement-offers/`**

- `libs/OfferProviders/OfferProvider.cs` — shared code (a common library): resolve once per distinct
  offer, answer a redeem. Providers keep no data.
- `microservices/PlayerEngagementCampaignOfferService/` — `beamable_virtual_store`.
- `microservices/PlayerEngagementRewardOfferService/` — `beamable_reward`.
- `microservices/PlayerEngagementBadgeOfferService/` — `beamable_badge`.
- `microservices/PlayerEngagementCampaignOfferCatalogService/` — the store's catalog, an ordinary
  microservice (`CommerceCatalog.cs` reads the Mongo catalog, `ContentCatalog.cs` published content).

**Web SDK — `BeamableProduct/web/`**

- `src/services/OffersService.ts` — `beam.offers`: `getOffers` and `redeem`.
- `src/__generated__/apis/OfferApi.ts` and the `Offer*` / `PlayerOffer*` schemas, generated from the
  gateway's OpenAPI.

**Sample — `BeamableProduct/beam-native-mobile/Samples/ReactNative/`** (§6)

---

## 3. The gateway surface

`/api/offers`. A player can reach two routes.

| Route | Client? | Guard |
|---|---|---|
| **`GET /`** | **yes** | `ForbiddenForPlayer`, per-player read limit |
| **`POST /redeem`** | **yes** | `ForbiddenForPlayer`, per-player redeem limit |
| `POST /grant` | no | `CampaignOffer:Write` |
| `POST /revoke` | no | `CampaignOffer:Write` |
| `POST /revoke-origin` | no | `CampaignOffer:Write` |
| `POST /report-redeemed` | no | `CampaignOffer:Write` |
| `GET /awaiting-operator` | no | `CampaignOffer:Write` |
| `POST /resolve` | no | `CampaignOffer:Write` |

The client routes are deliberately not permission-scoped: a player holds no operator permissions, so a
resource permission cannot express "may act on themselves". `ForbiddenForPlayer` lets a player act on
their own `playerId`, and privileged (non-`Guest`) accounts act on anyone. The permission matrix is a
fail-closed dictionary in `BeamableGateway/Security/Authorization/PermissionAuthorizationHandler.cs`.

The operator's `grant` takes a required `idempotencyKey` (a repeat after the grant ended answers
`grant-ended` without asking the provider), an optional `originId` (a batch, default `manual`) and an
optional `expiresAtUnixSeconds`, which must be in the future;
`revoke-origin` expires that batch's open grants in the request's realm. `report-redeemed` is a
provider reporting a redeem made in its own service (`federationId`, `playerId`, `grantId`; only its own
grants; 409 while the grant is still pending). `awaiting-operator` and `resolve` serve the grants on
hold (§5).

---

## 4. What crosses the wire

`GET /api/offers?playerId=…` returns the offer **inline**, so one call renders a store screen:

```jsonc
{ "offers": [ {
    "grantId": "4f1c…", "federationId": "beamable_virtual_store",
    "offerId": "stores.main/listings.starter", "state": "Granted",
    "grantedAtUnixSeconds": …, "expiresAtUnixSeconds": …,

    "offer": {
      "offerId": "stores.main/listings.starter",
      "title": "Starter Pack", "priceLabel": "350 Coins",
      "cost":    [ { "type": "currency", "symbol": "currency.coins", "amount": 350 } ],
      "rewards": [ { "type": "currency", "symbol": "currency.gems",  "amount": 500, "title": "gems" } ],
      "available": true, "unavailableReasons": [], "localizations": {}, "tags": [], "properties": {}
    },

    "available": true,
    "unavailableReasons": []
} ],
  "next": "…" }
```

- **One call, every provider.** `federationId` is an optional filter. `states` defaults to `Granted`
  and `Claiming`; pass `Redeemed`, `Revoked` or `Expired` for history. `next` pages, newest first.
- **`offer` is the snapshot the provider resolved at grant time.** Editing a catalog afterwards does not
  change what a player was given.
- **`available` is not `state`.** A `Granted` grant can be unavailable because its provider's rules say
  so (asked on every read, for the page's grants) or because the campaign's gate is closed (§5b).
  `unavailableReasons` is non-empty whenever it is false. A provider that does not answer in time
  leaves its grants listed without its rules; a redeem still meets them. A provider whose realm config
  sets `offers|{federationId}.hasAvailabilityRules` to `false` is not asked: its grants are available
  unless the gate is closed.
- **`rewards` is disclosure, not a receipt.** `type` is an open string; render what you know and fall
  back to `title`/`symbol`. An empty list means "this provider cannot enumerate its payout".
- **`cost` and `rewards` are the same shape**, and `amount` is always a real quantity. `priceLabel` is a
  convenience, never the only representation of a price.
- **There is no real-money price here.** A SKU-priced listing is refused by the store provider rather
  than described.

The store's catalog service reports withheld listings on its own response `properties`
(`withheldCount`, `withheldReason`), so the Portal can say "1 listing is priced in real money" instead
of showing an empty picker.

---

## 5. How a redeem works

`POST /api/offers/redeem { playerId, grantId, params? }`:

```
1. claim      the registry moves the grant Granted → Claiming (compare-and-set; for an exclusive
              group, also takes the group's anchor, so two alternatives cannot both be redeemed)
2. gate       the campaign's condition is checked on fresh stats; closed → released, refused with a reason
3. deliver    RedeemOffer(grant, params) on the grant's provider
4. record     Settled → Redeemed, forfeits the rest of an exclusive group and any named entries
              Refused → back to Granted
              Ambiguous, or a failed call → stays Claiming; the expiry worker asks again 20 s later,
              doubling up to 1 h, with the redeem's params (or the player does, after the claim lease)
```

The provider delivers idempotently by grant id: a redeem asked again is the same delivery. When that only
holds for a while, the realm says so in its config for the provider, and the registry, not the provider,
decides whether to ask again:

- `offers|{federationId}.ambiguousRedeliverySeconds`: for how long after the first unanswered delivery
  the registry may ask for it again (unset = the platform default (12 h); 0 = never asked again).
  Beamable's providers take the default: reward's inventory transaction dedup covers it. A delivery that
  cannot be repeated safely is set to `Hold` below.
- `offers|{federationId}.ambiguousOutcome`: what the delivery is taken to have been once that window
  passes, by name, ignoring case.
  `NotDelivered` (default) gives the grant back, so the next redeem is a new delivery; `Delivered`
  records it as redeemed and forfeits its group; `Hold` leaves it in progress until someone checks the
  provider's service and calls `offers/resolve`, listed meanwhile by `offers/awaiting-operator`.

Both are read from the realm config at grant time and fixed on the grant: a changed value holds for the
grants written after it. A value that does not read as its type takes the default and logs a warning,
at most once a minute per realm, provider and setting. Within the window the expiry worker asks for the
delivery again by itself (`Offer:Service:RedeliveryRetryBaseSeconds` 20 s, doubling up to
`RedeliveryRetryMaxIntervalSeconds` 1 h), and `NotDelivered` and `Delivered` are applied by it as soon as
the window and the claim lease have passed, so no grant waits for its player to come back.

### The store: redeeming buys

For `beamable_virtual_store` the redeem **is** the purchase. The provider calls
`POST /object/commerce/{playerId}/purchase` as the player (`AssumeNewUser`, `requireAdminUser: false` —
the gateway already authorized the caller), with `purchaseId = "{listing}:{store}"` recovered from the
offer id it minted. That route is soft-currency only by construction:

```scala
// BeamableBackend/tools/commerce/.../CommerceObjectService.scala
private def verifyExecutableListing(executable: ExecutableListing): Unit = {
  if (executable.price.`type`.toLowerCase != "currency")
    throw ServiceError(BAD_REQUEST, "", "UnexpectedPriceType", executable.price.`type`)
}
```

In one call it enforces the listing's active period, schedule, purchase limits and requirements,
debits the price and credits the payout in one inventory transaction, and records the purchase in
`offer_status`. Commerce's refusal message is passed through: it names which of the listing's own gates
failed.

**Why the provider buys instead of checking that the client did.** Redeem is client-callable, and
settling forfeits the other alternatives. For a virtual offer no read tells the provider whether a client
already bought: the platform exposes only `purchasesRemain`, which is null unless the listing has a
purchase limit. Performing the purchase removes the question.

**Why an unanswered purchase is never repeated.** The purchase route takes only a `purchaseId`, so it
cannot be retried idempotently, and nothing in the store makes a repeat safe. After a timeout or 5xx the
registry settles the redeem by the ambiguous outcome: not delivered by default (a purchase that did
happen may then be made again), or what the realm chose.

The response carries no deltas (commerce calls the plain `updateInventory`), so what moved is read back
from the inventory by whoever needs it.

---

## 5b. Conditions: a gate, not a filter

A campaign can gate an offer. **It does not decide whether to grant.** Every recipient is granted, and
the offer reports itself unavailable until the player qualifies — someone who receives a push for an
offer they cannot yet take keeps it and watches it unlock.

- **The gateway evaluates; the provider never sees it.** The condition (a segmentation rule over the
  player's stats, plus an optional `lockedMessage`) is stored on the grant and evaluated by `OfferGate`
  with the segments engine's own evaluator. The list and the redeem use the same method, so a locked
  offer and a refused redeem cannot disagree. A provider reports its own rules on `available` /
  `unavailableReasons`.
- **Snapshotted at grant.** Editing a campaign afterwards does not change the rule a player was granted
  under.
- **Ordering operators need numbers.** `>`, `>=`, `<`, `<=` and `between` compare numerically; an
  operand that can never be ordered is refused at publish.
- **Failing closed.** A rule that cannot be parsed, a stats read that failed — all resolve to *not
  satisfied*.
- **The reason** carries `attribute`, `target` and `current` (`OfferContract.Reason*Key`), so a client
  can render "Level 3 / 10". `current` is omitted unless the stat's namespace is public, and only a leaf
  or an `and` names a requirement.

---

## 6. The React Native sample

`BeamableProduct/beam-native-mobile/Samples/ReactNative` — Expo + expo-router. The **Offers** tab is the
player half end to end.

```
app/(tabs)/offers.tsx          the screen: provider filter · Wallet · Campaign Offers · From the last push
src/beam/campaignOffers.ts     list + claim over beam.offers, the normalised CampaignOffer model, badge read-back
src/beam/inventory.ts          currency balances and the wallet diff
src/ui/OfferCard.tsx           one grant, rendered from its offer
src/ui/BalanceRow.tsx          a currency with an optional signed change chip
```

- **One list, every provider**, with an optional filter (All · Store · Reward · Badge · Custom id). A
  claim needs only the grant id, including from a push's `beam_offer_grants` deep link.
- **The card is drawn from the offer**, never from the federation id: a cost means Buy, an empty cost a
  free Claim, a `badge` reward is read back from stats after the claim.
- **"Received" is a wallet diff** around the redeem, not the response. Money stays `bigint` in
  `inventory.ts`, formatted by hand (Hermes ships a partial `Intl`).
- `status: 'pending'` shows "still confirming"; pressing again retries the same delivery.

---

## 7. Running it locally

The stack config lives at `agentic-portal/.beamable/local-stack.json`; the providers and the catalog
run in the `group: player-engagement` step.

- **Scala services**: `commerce` (store), `inventory` (store, reward) and `stats` (badge, gates) must be
  running.
- **Provider registration**: without a running provider the registry fails the grant retriably and logs
  `Unable to find an IFederatedVirtualOffer registration for '<id>'`; the campaign's publish refuses an
  offer whose federation is not registered (`OfferFederationNotRegistered`).
- **Realm setup for the store**: published `currency.*` content (one the offer pays, one it is priced
  in), a listing priced in `currency.*` with a real payout in an active store, some of the price currency
  on the player, and a published campaign whose lane offers it.
- **Changing the contract** means rebuilding the SDK the providers consume
  (`cd BeamableProduct && ./dev.sh --skip-unreal --skip-sams-sandbox`) and regenerating the web client
  from the gateway's OpenAPI.

| Symptom | Look at |
|---|---|
| Grant retried forever, `Unable to find an IFederatedVirtualOffer registration` | the provider is not running or registered in another realm |
| A listing is missing from the store picker | it is priced in a SKU; the catalog logs the skip |
| Redeem refused, `UnexpectedPriceType` | the listing was repriced to real money after the grant |
| Redeem refused, insufficient funds | the player does not hold enough of the price currency |
| Redeem `pending` forever | the provider's (or the realm's) ambiguous outcome is `Hold`; see `offers/awaiting-operator` |
| Offer listed but `available: false` | its gate (`reason`) or the provider's own rule |

The providers log every purchase, payout or badge write, and every refusal with its reason.

---

## 8. Non-production paths

### 8.1 The two catalog paths are not both tested by one realm

`commerce|useComet = true` keeps commerce in **content**, so `GET /basic/commerce/catalog` answers empty
and `ContentCatalog` is the live path; a Mongo-backed realm exercises `CommerceCatalog`. They have
separate reward-projection and listing-filter code, including the real-money skip.

### 8.2 `api|restrictTimeOverride`

| Key | Default | If wrong |
|---|---|---|
| `api\|restrictTimeOverride` | `true` | a client can pass `time` and move the clock, defeating listing schedules |

The purchase itself ignores a client `time`; listing schedules are what it would expose.

### 8.3 The Portal's demo backend

`agentic-portal/src/beam/demo/` serves commerce data client-side when the realm pid starts with
**`DEMO_`** (not `DE_`) or the game's root pid is the demo project.

### Before promoting a realm

- [ ] `api|restrictTimeOverride` is **true**
- [ ] Fixture content is gone or replaced
- [ ] Every listing sold as a virtual offer is priced in a published `currency.*`
- [ ] Test grants and guest players cleaned up

---

## 9. Known gaps

- **The real-money federation does not exist.** Platform product ids, a native purchase flow in each
  client SDK, receipt verification and a settlement callback are its design, not this one's.
- **Unity and Unreal cannot reach `/api/*`.** Their codegen consumes only the `/basic/*` Scala OpenAPI
  documents; see RFC 004.
- **What a grant created in a third party's service stays redeemable there until its revoke lands.**
  The platform calls `RevokeOffer` for every grant that ends unredeemed, at the pace the provider's
  answers ask for (`throttled`, `maxInFlight`, `retryAfterMs`), and not while the customer is sending, so
  between the expiry and that call the third
  party may still honour it. A provider closes the gap by passing `expiresAt` on to its service, or its
  realm sets `offers|{federationId}.revokeLeadSeconds`.
- **An unanswered delivery is settled by assumption.** Past the provider's window the registry cannot
  know what happened: `NotDelivered` may deliver twice, `Delivered` may deliver nothing, `Hold` needs a
  person. Which risk to take is per provider and per realm.
- **The in-game rail cannot carry `beam_offer_grants`.** Beamable mail has no extras field, so the deep
  link works on push only.
- **The Portal picker shows only `priceLabel`.** It cannot warn that a listing is priced in a currency
  the realm does not publish; withheld listings are reported as a count and a sentence.
