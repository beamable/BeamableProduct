# The virtual offer federation, end to end

A reading guide to the campaign **offer** system: what it is, which repo owns which piece, how a
purchase actually flows, and where to look when something breaks.

> **This is the *virtual* offer federation** — offers a player buys with soft currency.
> `IFederatedCampaignVirtualOffer`, default provider id `beamable_virtual_store`. A price on this
> contract is a currency symbol and an amount; there is no real-money price, no platform product id
> and no payment settlement callback.
>
> **Real money is a separate federation, and it does not exist yet.** It needs platform product ids,
> a native purchase flow, receipt verification and a settlement callback from outside Beamable —
> substantial backend work. Carrying both on one interface meant every virtual provider inherited
> fields it could not fill and a payment-verification path it had no use for, so the two were split.
> If you are looking for the real-money path, it is not here.

This is the *system* view. Two neighbours cover other ground and are not repeated here:

- **`agentic-portal/guides/creating-offer-providers.md`** — how to ship your own store provider.
  Read that when you are writing a federation, not when you are trying to understand one.
- **`BeamableProduct/rfc/004_offer_federation_client_sdks.md`** — the decision record: what was
  built, what is still missing, and why Unity/Unreal are blocked.

---

## 1. The one idea

A campaign lane can attach an **offer** to the message it sends. The operator authors it in the
Portal, the campaign runtime **grants** it to each recipient as the send goes out, and the player
acts on it in-game.

Two sentences carry almost the whole design:

> **An offer is a storefront listing.** It is not a bag of currency the campaign invented. It is a
> listing a designer already built in a store, with its own currency price, its own contents and its
> own eligibility rules — addressed by an offer id of the form `{store}/{listing}`.

> **"Which store" is a federation, not a feature.** `beamable_virtual_store` is the default provider
> Beamable ships. A game with its own virtual economy implements the same interface under its own id
> and is treated identically by the gateway, the campaign runtime and the Portal.

A third sentence was added when the contract was simplified:

> **A catalog is not a federation concern.** Listing and describing offers had exactly one consumer —
> the provider's own Portal extension — and a federation method whose only caller is the provider's
> own UI federates nothing. The message rail, which this federation is modelled on, has no catalog
> methods either. So a catalog is an ordinary microservice, and a game with its own economy writes
> its own, or ships none and authors offers inline in its extension.

Everything awkward about the code follows from these, and the second one has a hard rule
attached: **nothing outside a provider may branch on which federation id it is talking to.** If you
find yourself writing `if (federationId == "beamable_virtual_store")`, the extension point is broken.

A third rule falls out of the first: **the offer id is opaque.** Only the store that minted it can
interpret it, which is why the federation id travels with it everywhere. Do not parse it, show it as
a name, or key anything durable on its shape.

### What a grant is, and what it is not

A grant is a **ledger row**: "this store owes this player the chance to buy this listing, until this
time." It is not the goods, and claiming it is not delivery.

```
granted ──buy the listing──▶ (goods delivered by commerce/payments)
   │                                      │
   │                                      ▼
   └──────────────────────────────▶ redeemed   ← the ledger closes
   │
   ├── revoked   (an alternative was taken, or an operator pulled it)
   └── expired   (evaluated on READ, never swept)
```

Two consequences that catch people:

- **Expiry is folded in on read.** A grant past its expiry reports `expired` the first time anyone
  looks, even though nothing touched it. So an entitlement list must never be cached across a
  session.
- **`state` is the source of truth, not the presence of a row.** `redeemed` and `revoked` grants
  stay in the list forever.

---

## 2. Where everything lives

Four repos. This is the map worth memorising.

| Repo | Owns |
|---|---|
| **BeamableProduct** | The **contract** (the DTOs every side shares), the web/JS SDK, the React Native sample |
| **BeamableAPI** | The **C# gateway** (public routes), the **campaign runtime** (grants on send), and a mirror of the contract |
| **agentic-portal** | The **default provider** `beamable_virtual_store`, and the Portal authoring extensions |
| **BeamableBackend** | The Scala platform: `commerce`, `inventory`, `content`. **Knows nothing about offer federation** — zero references to it |

### The files that matter

**Contract — the source of truth for every DTO**

- `BeamableProduct/cli/beamable.common/Runtime/Federation/FederatedCampaignVirtualOffer.cs`
  - `IFederatedCampaignVirtualOffer<T>` — the six methods
  - `CampaignOfferItem` — an offer as a store describes it
  - `CampaignOfferPrice` — a soft-currency `symbol` + `amount`, plus a display `label`
  - `CampaignOfferReward` — what the offer *contains*
  - `CampaignOfferContract` — every wire string: states, statuses, reason codes, reward types, payload keys
- `BeamableProduct/client/Packages/com.beamable/Common/Runtime/Federation/FederatedCampaignVirtualOffer.cs`
  — the Unity mirror. **Do not edit.** It is read-only, untracked, and regenerated from the file
  above by `dotnet run --project cli/ -- unity release-shared-code` (which `dev.sh` runs). See
  `docs/developer-help.md`.
- `BeamableAPI/BeamableShared/Federation/CampaignOffer/CampaignOfferMessages.cs` — the gateway's
  mirror, in `record` style. **A field added on one side and not the other is exactly what
  `BeamableShared.Tests/Tests/Federation/CampaignOfferWireShapeTest.cs` exists to catch** — it
  asserts exact JSON strings, so adding a property to a serialized record breaks it on purpose.

**Gateway — `BeamableAPI/BeamableGateway/Controllers/CampaignOfferController.cs`**

**Campaign runtime — `BeamableAPI/BeamableShared/Services/Campaigns/Runtime/CampaignSendDispatcher.cs`**
- `TryGrantOffers` (`:248`) → `GrantOne` (`:458`). Grants happen **before** the message reaches the
  rail, so the grant id can ride the same payload.
- Three deliberately distinct outcomes: granted → id in the payload; *retriable* failure → the send
  stays pending and nothing ships; *terminal* failure → **the message ships anyway with no offer**. A
  sold-out offer degrades the outreach, it does not swallow it.

**Default provider — `agentic-portal/services/BeamableCampaignOfferService/`**
- Four federation methods and no catalog. Listing offers left the contract entirely — see §1.
- `BeamableCampaignOfferService.cs` — `Grant`, `Redeem`, `BuyListing`, `Entitlements`,
  `ToEntitlement`, `Forfeit`
**Default catalog — `agentic-portal/services/BeamableCampaignOfferCatalogService/`**
- An ordinary microservice, **not a federation**: `ListOffers` / `GetOffer` as `[ServerCallable]`
- `CommerceCatalog.cs` — reads the **Mongo** catalog (`/basic/commerce/catalog`), delegates to
  `ContentCatalog` when that is empty
- `ContentCatalog.cs` — reads **published content**, which is where a `commerce|useComet` realm keeps it
- `OfferGrantStore.cs` — the grant ledger. Stored as a JSON blob in a **private per-player stat**,
  `beam_offer_grants` (`:26`), not a storage object.
- `Models.cs` — `OfferGrant`, `GrantedListing`, `AuthoredOffer`

**Web SDK — `BeamableProduct/web/`**
- `src/services/CampaignOfferService.ts` — `beam.campaignOffer`, the only two client-callable routes
- `src/__generated__/apis/CampaignOfferApi.ts` and eleven campaign-offer schemas (ten
  `schemas/CampaignOffer*.ts` plus `RedeemCampaignOfferRequest.ts`)
  — **hand-written**, named exactly as the generator would spell them so a future `pnpm codegen`
  replaces rather than collides. See RFC 004 §7 before regenerating.

**Sample — `BeamableProduct/beam-native-mobile/Samples/ReactNative/`** (section 6)

---

## 3. The gateway surface

`/api/campaign-offer/*`. Four routes; a player can reach exactly two.

The route itself is **shared vocabulary, not this federation's**: it dispatches by `federationId`, the
way one `/api/message-rail/*` route serves push, email and ingame. A future real-money offer federation
is reached through these same paths, and only the interface behind them differs. That is also why the
DTOs are still named `CampaignOffer*` rather than `CampaignVirtualOffer*`.

| Route | Client? | Guard |
|---|---|---|
| `POST /grant` | no | `CampaignOffer:Write` — the campaign runtime |
| `POST /revoke` | no | `CampaignOffer:Write` |
| **`POST /redeem`** | **yes** | `ForbiddenForPlayer` |
| **`GET /campaign-offers`** | **yes** | `ForbiddenForPlayer` |

The two catalog routes are gone. A picker reads its own provider's catalog service directly, so
`CampaignOffer:Read` no longer gates anything — **including for the Tester role**, which used to
reach the picker through it. That is an accepted regression: a Tester can still publish a campaign,
they just cannot browse the catalog.

The two client routes are **deliberately not permission-scoped.** A player holds no operator
permissions at all, so a resource permission cannot express "may act on themselves". Instead they are
guarded per-request by `ForbiddenForPlayer`: you may act on your own `playerId`, and privileged
(non-`Guest`) accounts may act on anyone.

> That is also why `Redeem` performs the purchase itself rather than trusting that the client made
> one — see §5. The permission system cannot express "has paid"; and for a virtual offer, nothing the
> provider can read tells it either.

The permission matrix is a hardcoded, fail-closed dictionary in
`BeamableGateway/Security/Authorization/PermissionAuthorizationHandler.cs`. A resource string missing
from it returns 403 for everyone below SuperAdmin, silently.

---

## 4. What crosses the wire

`GET /api/campaign-offer/campaign-offers` returns the offer **inline**, so one call renders a store
screen instead of a lookup per row:

```jsonc
{ "playerId": "…", "contractVersion": 1, "offers": [ {
    "grantId": "bsg_…", "offerId": "stores.main/listings.starter",
    "state": "Granted", "grantedAtUnixSeconds": …, "expiresAtUnixSeconds": 0,

    "offer": {
      "offerId": "stores.main/listings.starter",
      "title": "Starter Pack", "description": …, "imageUrl": …, "priceLabel": "350 Coins",

      // WHAT IT COSTS and WHAT YOU GET — the same shape, at the same level.
      "cost":    [ { "type": "currency", "symbol": "currency.coins", "amount": 350 } ],
      "rewards": [ { "type": "currency", "symbol": "currency.gems",  "amount": 500,
                     "title": "gems", "properties": {} } ],

      "available": true, "unavailableReasons": [],
      "localizations": {}, "tags": ["stores.main"], "properties": {}
    },

    "available": true,
    "unavailableReasons": []
} ] }
```

Things to know about this payload:

- **Every field below `expiresAtUnixSeconds` is optional.** A provider may send no `offer` at all;
  the client falls back to the opaque `offerId`. Write the fallback, do not guard the whole block.
- **`available` is not `state`.** A grant can be `granted` and still unavailable because a
  requirement on its listing is unmet. `unavailableReasons` is non-empty whenever it is false, and
  `message` is written to be shown.
- **`rewards` is disclosure, not a receipt.** It says what the offer promises. It is never reconciled
  against what actually landed — a loot roll or a store-side promotion can legitimately differ.
  `type` is an **open string**; render what you know and fall back to `title`/`symbol`. An empty list
  means "this store cannot enumerate its payout", *not* "this offer gives nothing". `amount: 0` means
  "varies" (a loot roll), not "none".
- **The price is numbers, not just a label.** `cost[].symbol` and `cost[].amount` are what let a
  client compare against a balance, disable a row the player cannot afford, or show "350 / 200
  Coins". `priceLabel` is a convenience on top, neither localizable nor comparable — never the only
  representation you read.
- **`cost` and `rewards` are the same shape.** A cost is a reward in the other direction, and a
  client renders both the same way. The price used to hang off a storefront-listing wrapper, which
  meant a provider without a storefront had nowhere to say what its offer cost at all.
- **`amount` is always a real quantity.** A loot roll is one roll, not zero of a prize. A store that
  cannot enumerate its payout leaves `rewards` empty instead.
- **`available` is re-evaluated on every read.** A campaign can gate an offer on a requirement, and
  the gate is checked when the list is read — so an offer a player could not claim yesterday unlocks
  by itself, with no new send. Never cache this list across a session.
- **There is no real-money price here.** No `realPriceCents`, no `currencyCode`, no `productIds`: this
  is the virtual federation, and a SKU-priced listing is refused rather than described. If you are
  looking for the handles a native purchase flow needs, they belong to the real-money federation,
  which does not exist yet.
- **`properties` bags** exist on the item, each listing, the price, each reward, each reason, and on
  `CampaignOfferCatalogResponse` itself. They are the escape hatch that lets a store carry its own
  fields without a contract version bump.

**One catalog-level use of that bag is load-bearing.** `POST /api/campaign-offer/offers` returning
`offers: []` is ambiguous — "this realm has authored nothing yet" and "this store has listings but none
it can offer here" look identical, and only the store knows which. Since this federation withholds
real-money listings, the second case is now common, so the provider reports it:

```jsonc
{ "offers": [], "nextCursor": "",
  "properties": {
    "withheldCount":  "1",
    "withheldReason": "1 listing in this realm is priced in real money, so it cannot be sold as a virtual offer." } }
```

`CampaignOfferContract.CatalogWithheldCountKey` / `.CatalogWithheldReasonKey` are the keys. The reason
is a sentence the store writes and the Portal renders verbatim, so a third-party provider withholding
for its own reason needs no client change. Absence means nothing was withheld, not "unknown".

---

## 5. How a purchase actually works

The claim *is* the purchase. That is the one thing to internalise, because the two-step shape this
feature used to have — buy through payments, then claim to close the ledger — is gone.

### The route

`POST /object/commerce/{playerId}/purchase` is what buys a virtual listing, and it is **soft-currency
only** by construction:

```scala
// BeamableBackend/tools/commerce/.../CommerceObjectService.scala
private def verifyExecutableListing(executable: ExecutableListing): Unit = {
  if (executable.price.`type`.toLowerCase != "currency")
    throw ServiceError(BAD_REQUEST, "", "UnexpectedPriceType", executable.price.`type`)
}
```

For this federation that check is a feature, not an obstacle: a SKU-priced listing is not something a
virtual offer can be, so both catalog readers skip it and `Grant` refuses it. The listing never
reaches a player, and the operator gets a log line saying why.

What the route does, in one call (`CommerceObjectService.onPurchase`):

- `requireSelf` — a player token can only buy for itself
- `getExecutableListing` — resolves the listing and enforces its active period, schedule, purchase
  limits, and stat / cohort / entitlement / offer requirements
- **debits the price and credits `obtainCurrency` + `obtainItems` in ONE inventory transaction**
- applies entitlement generators
- `markOfferPurchased` — records the purchase in `offer_status`

`PUT /object/commerce/{id}/purchase` is not related — it needs scope `commerce-write` (server-only)
and only bumps purchase counters. It grants nothing.

`purchaseId` is `"{listing}:{store}"`; the platform splits on `:` and searches all stores when the
store half is omitted (`CommerceService.getExecutableListing:935`).

### The flow

```
1.  read the offer     GET /api/campaign-offer/entitlements
                       → listings[0].price.symbol, .amount  (a currency and how much of it)

2.  claim              POST /api/campaign-offer/redeem
                       → the provider commits the claim
                       → then buys the listing AS THE PLAYER, via
                         POST /object/commerce/{playerId}/purchase
                       → then forfeits the alternatives

3.  show what moved    GET /object/inventory/{playerId}/?scope=currency
                       → the wallet, diffed around step 2
```

One client call, not two. Step 3 is needed because `InventoryUpdateResponse.deltas` is not populated
on this route — commerce calls the plain `updateInventory`, and only `updateInventoryWithDeltas` sets
`includeDeltas` — so the response confirms the purchase but not what moved.

### Why the provider buys, instead of checking that the client did

`Redeem` is client-callable and unpermissioned, and it calls `Forfeit`. If it settled a grant on the
caller's word, any player could POST an invented `transactionId`, mark their own grant redeemed **for
free**, and destroy the sibling grants they were meant to choose between — while the ledger recorded a
conversion that never happened.

So the gate has to be something the store knows rather than something the client asserts. For a
virtual offer, **no such read exists.** The platform does record every purchase — `markOfferPurchased`
appends a timestamp to `InteractionInfo.purchases: Seq[Date]` in Mongo `offer_status` — but the only
per-player purchase figure it exposes on a route a microservice can call is
`PlayerListingView.purchasesRemain`, and that is

```scala
purchaseLimit.map(limit => limit - status.numberOfListingPurchases(symbol))   // CommerceModel.scala:544
```

— `None` whenever the listing has no purchase limit. (`PortalOfferView.purchases`, which *is* an
absolute count, comes only from `getActiveOffersForPortal` at `/portal/commercev2/:dbid` on the Play
REST edge, which a microservice cannot reach.)

Performing the purchase removes the question instead of answering it. `BuyListing` calls
`AssumeNewUser(playerId)` — the platform's supported way to act for a player — with
`requireAdminUser: false`, because authorization already happened upstream in the gateway's
`ForbiddenForPlayer`, and a federation endpoint is reachable only by the platform, never by a player
token directly.

### The ordering, and which failure it chooses

`Redeem` commits the claim **before** spending, and rolls it back if commerce refuses:

```
state = redeemed; save     →     buy     →     forfeit; save
                                  ↓ refused
                     state = granted; save; return the refusal
```

That order is chosen for the failure it leaves behind. A crash between the commit and the spend costs
the player this offer. Spending first and crashing before the write would leave the grant claimable
and charge them a second time on retry. Losing an offer is recoverable; taking someone's currency
twice is not.

Commerce's refusal message is passed through rather than replaced — it names which of the listing's
own gates failed, and anything written provider-side would be a guess about a decision made elsewhere.

### The transaction id

`OfferGrant.transactionId` is the **client's** invented idempotency key, and its only job is telling a
double-tap (same key → succeeds, "Already redeemed.") from a double-claim (different key → refused).
The web SDK mints one per `federationId:grantId` and reuses it, which is what makes a retry safe.
Nothing overwrites it — doing so would turn every legitimate retry into a refusal.

### Exclusive groups

A lane can hold several offers that are *alternatives*. The campaign resolves that vocabulary down to
a flat `invalidatesOfferIds` list before the store ever sees it — a store is told **which offers to
revoke**, never how the campaign decided. `Forfeit` is where it happens, reachable from `Redeem` only,
and it runs only after the purchase has actually succeeded.

---

## 5b. Conditions: a gate, not a filter

A campaign can gate an offer. **It does not decide whether to grant.** Every recipient is granted, and
the offer reports itself unavailable until the player qualifies — so someone who receives a push for
an offer they cannot yet take keeps it and watches it unlock. Deciding once, at send time, is what
used to make an offer unreachable forever for anyone who happened not to qualify at that instant.

Three pieces already existed for this and are simply being used properly now:
`CampaignOffer.available` is documented as distinct from `state`, `ReasonStatRequirement` was already
in the vocabulary, and expiry was already folded in on read. The campaign's gate joins the same lazy
model the store's own rules use.

**Who evaluates.** `CampaignConditionRef.FederationId` is an id, not a flag:

| | |
|---|---|
| Empty | the **platform** evaluates a segmentation rule over the player's stats — the default |
| Set | that **federation** answers it, via `VerifyConditions` |

The indirection is what keeps "the campaign owns conditions" true: tying the gate to the offer's own
store would mean a game using Beamable's commerce could never bring its own eligibility language
without forking the default provider.

**The gateway evaluates, never the store.** A store answers for its own rules — its schedule, its
purchase limits — and knows nothing about campaign conditions. The gateway overlays its verdict on
the way out, and `unavailableReasons` is a list so both can be heard.

**How the gate survives to read time.** It rides in `CampaignOfferGrantContext.conditionToken`, the
store persists it opaquely, and hands it back on `CampaignOffer.conditionToken`. The gateway strips it
before a client sees it. The store is the only thing that persists per grant, and the gateway is
stateless for offers — so the token travels the way a cookie does. It is **snapshotted at grant**:
editing a campaign afterwards must not change the rule a player was granted under.

**Failing closed.** A rule that cannot be parsed, a federation that cannot be reached, a check absent
from a verdict — all resolve to *not satisfied*. Opening a gate nobody answered is the worse of the
two failures.

**Two rules for the reason.** A condition was authored for an *operator*, so its prose is rarely fit
to show a player — that is what `lockedMessage` is for. And `properties` must never carry a value the
client could not otherwise read; a player's own client-visible stat is fine, a private one is not.

---

## 6. The React Native sample

`BeamableProduct/beam-native-mobile/Samples/ReactNative` — Expo + expo-router. The **Offers** tab is
the player half end to end.

### Files

```
app/(tabs)/offers.tsx          the screen: Store · Wallet · Entitlements · From the last push · How buying works
src/beam/campaignOffers.ts     the federation bindings + the normalised Entitlement model
src/beam/inventory.ts          currency balances and the wallet diff
src/beam/commerce.ts           buying a listing (one call)
src/ui/OfferCard.tsx           one entitlement, rendered
src/ui/BalanceRow.tsx          a currency with an optional signed change chip
src/ui/RefreshButton.tsx       the ↻ accessory (shared with the Segments tab)
```

### The house pattern

Every `src/beam/*.ts` module looks the same, and it is worth copying:

- a long comment explaining **why**, not what;
- a local `requireBeam()` throwing one shared `NOT_CONNECTED` string;
- raw generated bindings from `@beamable/sdk/api` called with `beam.requester`, because the SDK has
  no high-level service for inventory or commerce (`segments.ts` and `ingameMessages.ts` set this
  precedent);
- **normalisation at the module boundary**, so `bigint | string` never reaches a component.

That last one has a wrinkle worth knowing. Timestamps and stat values go through `Number(...)`. **Money
does not** — `inventory.ts` keeps balances as `bigint`, because a balance is money and a receipt is a
subtraction of two balances. It also formats by hand rather than with `toLocaleString`, since Hermes
ships a partial `Intl` and `BigInt.prototype.toLocaleString` is not safe to rely on.

### What the screen does

**Wallet** sits *above* the offers so a purchase reads before → action → after in one glance. It
merges the player's balances with every currency the realm publishes
(`beam.content.getByType({ type: 'currency' })`), so a currency you hold none of still has a `0` row
for a purchase to move. Currency content carries **no display name** — only `id`, `version`, `uri`,
`tags`, `properties` — so labels are the id's last segment.

**Each entitlement** renders as an `OfferCard`: title, price, a **"You get"** line from `rewards`,
availability reasons, and the ids. With no `offer`, it degrades to the opaque-id row it always was.

**Buy** is one press, **one call**, one receipt. It reads the wallet, calls `claimGrant` — which is
where the provider buys and settles — and reads the wallet again. The sample does not post to commerce
itself; it used to, and that charged the player twice.

**"Received"** is a wallet diff around the purchase, *not* the response.
`InventoryUpdateResponse.deltas` is not populated on this path (`onPurchase` calls the plain
`updateInventory`; only `updateInventoryWithDeltas` sets `includeDeltas`). The diff also captures the
price, so cost and gain show together in one line: `-350 coins · +500 gems`.

**This is a real purchase.** There is no `test` payment handler in the loop and no realm flag standing
in for a store: commerce debits real currency and credits the real payout. That is the practical payoff
of splitting the federations — the virtual path is exercisable end to end without any of the
non-production machinery real money needs.

**Never branch on the federation id.** The federation id is a screen input with the default as its
placeholder, never a hardcoded constant, and nothing in `src/beam/*` reads its value. `isPurchasable`
is the subtle version: it checks only that the grant is claimable and has a listing, and lets commerce
be the authority on whether the purchase is allowed — a client-side whitelist of price types would
reject a provider using its own vocabulary for no reason.

---

## 7. Running it locally

The stack config lives at `agentic-portal/.beamable/local-stack.json`.

**Scala services required.** `DefaultScalaServices` in
`BeamableProduct/cli/cli/Services/LocalStack/LocalStackTemplate.cs:44` is a curated set that **omits
`commerce` and `inventory`**. Both are needed here; add them as `scala:` steps. `payments` is **not**
needed at all any more — that was the real-money path.

**The provider must be running.** It is not a Scala service — it is a microservice in
`agentic-portal/services/`:

```bash
beam project run --ids BeamableCampaignOfferService \
  --host http://localhost:8080 --portal-url http://localhost:4950 --force --detach
```

Look for `Federation components registered.` in its output. Without it the gateway answers **503
`Unable to find any service registrations for
IFederatedCampaignVirtualOffer/beamable_virtual_store`** — the single most common failure.

**Realm setup**

1. Publish `currency.*` content — **at least two**: one the offer pays out, and one it is priced in.
   An unpublished currency surfaces as `UnknownCurrencyException` at payout.
2. An offer with a real payout (`obtainCurrency` / `obtainItems`), on a listing **priced in
   `currency.*`**, in an active store. A SKU-priced listing is skipped by the catalog readers and
   refused by `Grant`, so it will not appear in the picker.
3. Give the player some of the price currency, or every claim is refused for insufficient funds.
4. A campaign whose send lane points at the listing via `beamable_virtual_store`, published, player
   enrolled.

No SKU, and no `payments|testHandlerActive` — neither is part of this path.

**Changing the contract** means rebuilding the SDK package the provider consumes:

```bash
cd BeamableProduct && ./dev.sh --skip-unreal --skip-sams-sandbox
```

That bumps `build-number.txt`, packs to `BeamableNugetSource`, regenerates the Unity mirror, deletes
the previous version and installs the CLI globally. Downstream projects pinned to the old version
need a restore afterwards.

### Debugging, in the order that usually pays off

| Symptom | Look at |
|---|---|
| 503 `no service registrations` | the provider is not running |
| `No binding found for inventory.object` | the Scala `inventory` service is not running |
| Wallet empty, no error | it reports its own failure — read the red line |
| A listing you published is missing from the picker | it is priced in a SKU. The provider logs `Skipping listing … priced in 'skus', which is real money` |
| Claim refused, `UnexpectedPriceType` | a grant made before the listing was repriced to real money. Reprice it, or revoke the grant |
| Claim refused, insufficient funds | correct — the player does not hold enough of the price currency |
| Claim refused, `Unavailable` | the listing is outside its active period or schedule, or its purchase limit is spent |
| No "You get" line | the listing's offer has no `obtainCurrency`/`obtainItems` |

The provider's own logs are the best tool by far:
`agentic-portal/.beamable/temp/serviceLogs/BeamableCampaignOfferService-*.txt`. It logs every
federation call it handles and the full JSON it responds with, plus a line per purchase it makes and
per listing it skips.

---

## 8. Mock data and non-production paths

**Most of this section used to be about the `test` payment handler, and it is gone.** Splitting real
money into its own federation removed the whole non-production apparatus from this path: no `test`
provider that mints goods for free, no `payments|testHandlerActive` realm flag that survives shipping,
no fixture SKU with a fake App Store product id, no `PROVIDER` constant to remember to change in a
shipped client. A virtual purchase debits real currency and credits the real payout, in dev and in
production, through the same one call.

What is left is short.

### 8.1 The two catalog paths are not both tested by one realm

`commerce|useComet = true` means commerce lives in **content**, so `GET /basic/commerce/catalog`
answers empty and `ContentCatalog` is the live path. A Mongo-backed realm exercises `CommerceCatalog`
instead. They have separate reward-projection and listing-filter code, so **testing one does not test
the other** — including the real-money skip, which is implemented in both.

### 8.2 `api|restrictTimeOverride`

| Key | Default | If wrong |
|---|---|---|
| `api\|restrictTimeOverride` | `true` | a client can pass `time` and move the clock, defeating listing schedules and expiry |

`CommerceObjectService.onPurchase` calls `currentTime(None, rc)` — it does not read a client `time` at
all, so the purchase itself is not exposed. `onGetOffers` sanitises `time` away unless `DEBUG_MODE`.
Worth auditing anyway, because listing schedules and grant expiry are both clock-dependent.

### 8.3 The Portal's demo backend

`agentic-portal/src/beam/demo/` (`index.ts` + `data.json`) serves `DEMO_COMMERCE_CATALOG` and
`DEMO_PLAYER_OFFERS` entirely client-side, intercepting `/basic/commerce/*` before it reaches the
network. It activates only when the realm pid starts with **`DEMO_`** or the game's root pid is the demo
project.

> Read that prefix carefully: a normal realm id like `DE_96847458205697026` starts with `DE_`, **not**
> `DEMO_`, and does not trigger demo mode. The two are one character apart.

### 8.4 Fixture content and test data in the local realm

The dev realm carries fixtures — `stores.main`, `listings.starter`, `currency.gems` — and grants held
by guest players from verifying the flow. Harmless locally, worth clearing before that realm is used
for anything measured. Note the old `skus.sku.gem_10` fixture is now inert here: a SKU-priced listing is
skipped by the catalog readers, so it cannot reach a player through this federation.

### Before promoting a realm

- [ ] `api|restrictTimeOverride` is **true**
- [ ] Fixture content (`stores.main`, `listings.starter`, `currency.gems`) is gone or replaced
- [ ] Every listing you intend to sell as a virtual offer is priced in `currency.*`, and that currency
      is published
- [ ] Test grants and guest players cleaned up

---

## 9. Known gaps

Carried here so they are not rediscovered. RFC 004 §4/§7 is the authority.

- **The real-money federation does not exist.** It is the large remaining piece of work, and it is
  deliberately deferred rather than half-built: platform product ids on the price, a native purchase
  flow in each client SDK, receipt verification, and a settlement callback so a purchase completed
  outside Beamable can close a grant. `OnPurchaseCompleted` and `POST /api/campaign-offer/purchase-completed`
  were removed with the rest of the real-money surface — that route was never called in production and
  `PaymentAuditEntry` carried no `grantId` to correlate on, so it was a shape without an
  implementation. Reintroducing it is part of that federation's design, not a regression here.
- **Unity and Unreal cannot reach `/api/*` at all.** Their codegen consumes only the `/basic/*` Scala
  OpenAPI documents, so the gateway's controllers are invisible to them. The web SDK has the gateway,
  which is why React Native is the only client that can do this flow today. See RFC 004 §4 for the
  choice between feeding the gateway OpenAPI into their codegen and hand-writing the two calls.
- ~~**`CampaignOfferItem.listings` is a multi-slot nothing fills.**~~ **Fixed.** The listing wrapper
  is gone: cost sits on the offer beside rewards, and the store and listing symbols live inside the
  offer id the provider minted rather than as contract fields.
- **A claim cannot be verified after the fact, only performed.** The provider buys during `Redeem`
  precisely because nothing it can read tells it whether a client already did — the platform records
  every purchase in `offer_status` but exposes only `purchasesRemain`, which is `null` without a
  purchase limit, and the absolute count lives behind the Play REST edge at `/portal/commercev2/:dbid`.
  Exposing a per-player purchase read on a `/basic` or `/object` route would open up the
  verify-instead-of-perform option; see §5.
- **`Redeem` is not atomic across the spend.** The claim is committed, then the purchase runs, then
  forfeiture is written. A crash mid-sequence costs the player the offer rather than double-charging
  them, which is the deliberate choice, but it is not a transaction.
- **The in-game rail cannot carry `beam_offer_grant`.** `InGameMessageRailService.ParsePayload` drops
  unknown keys and Beamable mail has no extras field. The deep link works on push only.
- **The Portal picker drops everything but `priceLabel`.** (Now its own service's problem rather than
  the federation's.) It cannot show the operator the price
  currency or amount, so it cannot warn that a listing is priced in a currency the realm does not
  publish. It *can* now say when listings were withheld, via the catalog `properties` bag in §4 — but
  that is a count and a sentence, not per-listing detail: the operator is told one listing was
  withheld, never which one. `CampaignOfferItem` has no availability field (only `GetOffer` does), so
  showing a withheld listing greyed out with its own reason would need a contract addition.
- **No test project for any `agentic-portal` service.** `BeamableCampaignOfferService`,
  `CommerceCatalog`, `ContentCatalog`, `OfferGrantStore` and `AuthoredOfferCodec` are untested, while
  `AuthoredOfferCodec`'s Portal-side mirror `offerContent.ts` has 37 tests. The real-money skip and
  the redeem ordering are the first things worth covering.
- **Generated files are hand-edited** (`Models.gs.cs` ×2, two web enums, `CampaignOfferApi.ts` and
  eleven schemas). A regeneration drops them — including the `IFederatedCampaignVirtualOffer` enum
  entries — unless the source OpenAPI documents are updated first.
