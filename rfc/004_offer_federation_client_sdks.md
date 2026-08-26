# 004 — Offer Federation: handoff for the Unity & Unreal client SDKs

**Status:** server, gateway and Portal are built and green. **The web SDK and the React Native
sample now ship the player half** (see §9). Unity and Unreal still have nothing.
**Purpose:** this is the input to a planning session for the client SDK half. It records what exists,
what a game client actually needs, and the one structural blocker that has to be decided before any
of it can be written.

---

## 1. What the feature is

A **campaign lane can attach an offer to the message it sends.** The operator authors the offer in the
Portal, the campaign runtime grants it to each recipient as the send goes out, and the player later
claims it in-game. "Which store the offer comes from" is a **federation** — an extension point, not a
Beamable feature:

```
Portal (author)          →  campaign graph  →  campaign runtime  →  IFederatedStoreOffer<T>
beamable-store-offer                            GrantOffer()         BeamableStoreOfferService
  (or a game's own                                                     (or a game's own
   offer extension)                                                     microservice)
```

`IFederatedStoreOffer<T>` is the extension point. `BeamableStoreOfferService` (federation id
`beamable_store`) is only the default implementation, shipped so the feature works out of the box. A
game selling through Steam, a console store, or its own web shop implements the same interface under its
own `[FederationId]` and is treated identically by the gateway, the campaign runtime, and the Portal.

**Nothing outside a given provider may branch on which federation id it is talking to.** That rule
applies to the client SDKs too — see §6.

---

## 2. What already exists

### BeamableProduct (SDK)

| Path | What |
|---|---|
| `cli/beamable.common/Runtime/Federation/FederatedStoreOffer.cs` | `IFederatedStoreOffer<T>` + every DTO + `StoreOfferContract` (payload keys, statuses, entitlement states) |
| `client/Packages/com.beamable/Common/Runtime/Federation/FederatedStoreOffer.cs` | the Unity mirror (build-time copy, read-only) |
| `cli/beamable.common/Runtime/OpenApi/Models.gs.cs` | `FederationType.IFederatedStoreOffer` / `BeamoV2FederationType.IFederatedStoreOffer` (+ Unity mirror) |
| `microservice/beamable.tooling.common/Microservice/FederatedComponentGenerator.cs` | maps the interface name to the enum — a miss throws at service startup |
| `microservice/beamable.tooling.common/Microservice/ServiceMethodHelper.cs` | routes all seven methods at `{federationId}/{MethodName}` |
| `web/src/__generated__/schemas/enums/*FederationType.ts` | the two web enums (also added the long-missing `IFederatedMessageRail`) |

The interface, in full:

```csharp
public interface IFederatedStoreOffer<in T> : IFederation where T : IFederationId, new()
{
    Promise<OfferCatalogResponse>      ListOffers(OfferQuery query);
    Promise<OfferDetailsResponse>      GetOffer(string offerId);
    Promise<OfferGrantResponse>        GrantOffer(string playerId, string offerId, OfferGrantContext context);
    Promise<OfferGrantResponse>        RevokeOffer(string playerId, string grantId);
    Promise<OfferRedeemResponse>       RedeemOffer(string playerId, string grantId, OfferRedeemRequest request);
    Promise<OfferEntitlementsResponse> GetPlayerEntitlements(string playerId);
    Promise<OfferPurchaseAck>          OnPurchaseCompleted(OfferPurchaseNotification notification);
}
```

> A federation id must match `[A-Za-z][A-Za-z0-9_]*` — the source generator rejects anything else
> (`BEAM_FED_0004`), because the id becomes a route segment and a generated client member. Hence
> `beamable_store`, not `beamable-store`.

### BeamableAPI (gateway + campaign runtime)

- `BeamableShared/Federation/StoreOffer/` — backend contract, wire DTOs, and the dispatch client that
  discovers the microservice registered for a federation id and POSTs to
  `basic/{ServiceNameWithoutType}/{federationId}/{Method}`. Modelled on `MessageRailFederation`.
- `BeamableGateway/Controllers/StoreOfferController.cs` — the seven public routes (§3).
- `BeamableShared/Services/Campaigns/Model/Nodes.cs` — `SendNodeBody.OfferFederationId` (additive,
  init-only). The offer ref alone is not routable: only the store that minted an id can interpret it, so
  the id and its federation travel together.
- `BeamableShared/Services/Campaigns/Runtime/CampaignSendDispatcher.cs` — grants **before** handing off
  to the message rail, so the grant id can ride the same payload. Three outcomes, deliberately distinct:
  granted → id in the payload; *retriable* failure → `SendPending` survives and nothing reaches the rail;
  *terminal* failure → the message ships anyway with no offer. A sold-out offer degrades the outreach,
  it does not swallow it.

### agentic-portal

- `services/BeamableStoreOfferService/` — the default provider. **Authoring, not catalog**: it mints an
  offer per campaign, parses the authored definition off the grant (`AuthoredOfferCodec`), stores it on
  the grant row, and fulfils from that row at redeem via `Services.Inventory.Update`.
- `bundles/extensions-libs/offer-contract/` — the Portal parent/child contract (`OfferDraft`,
  `OfferSiteData`, `validateOfferDraft`, `OFFER_KEY_PREFIX`).
- `bundles/extensions/hubs/player-engagement/beamable-store-offer/` — the authoring form.
- `bundles/extensions/hubs/player-engagement/campaigns/` — the lane's `offer` extension site.
- `guides/creating-offer-providers.md` — how a game ships its own provider.

**Test state:** 47 federation + 356 campaign + 18 message-rail (C#), 128 campaigns + 32 offer-codec +
75 rail (portal). All passing.

---

## 3. The gateway surface a client can reach

`BeamableGateway/Controllers/StoreOfferController.cs`, served under the `/api` path base.

| Route | Client-callable? | Notes |
|---|---|---|
| `GET  /api/store-offer/entitlements?federationId&playerId` | **YES** | what this player holds, in any state |
| `POST /api/store-offer/redeem` | **YES** | claim a grant |
| `POST /api/store-offer/offers` | no — `StoreOffer:Read` | catalog list; operator/Portal |
| `GET  /api/store-offer/offers/{offerId}?federationId` | no — `StoreOffer:Read` | |
| `POST /api/store-offer/grant` | no — `StoreOffer:Write` | server-to-server |
| `POST /api/store-offer/revoke` | no — `StoreOffer:Write` | |
| `POST /api/store-offer/purchase-completed` | no — `StoreOffer:Write` | payment webhook fan-in |

**The two client routes are deliberately not permission-scoped.** A player holds no operator permissions
at all, so a resource permission cannot express "may act on themselves". Instead they are guarded
per-request by `ForbiddenForPlayer`: you may act on your own `playerId`, and privileged (non-`Guest`)
accounts may act on anyone. This mirrors `MessageRailController`'s `register`/`unregister`.

Request/response shapes:

```jsonc
// POST /api/store-offer/redeem
{ "federationId": "beamable_store", "playerId": "1234", "grantId": "bsg_…",
  "request": { "transactionId": "…", "params": {} } }
// → { grantId, success, status?, message? }

// GET /api/store-offer/entitlements?federationId=beamable_store&playerId=1234
// → { playerId, entitlements: [ { grantId, offerId, state, grantedAtUnixSeconds, expiresAtUnixSeconds } ] }
```

`state` is one of `StoreOfferContract`'s: `granted` (claimable), `redeemed`, `revoked`, `expired`.

---

## 4. ⚠️ The structural blocker — decide this first

**The gateway's `/api/*` routes do not exist in the Unity or Unreal generated APIs.**

- Unity's `Common/Runtime/OpenApi/Models.gs.cs` and Unreal's
  `Plugins/BeamableCore/Source/BeamableCore/Public/AutoGen/SubSystems/*` are generated from the
  **`/basic/*` OpenAPI documents** (`cli/cli/openapi/*.oapi.json`, downloaded via `SwaggerService`).
- `store-offer`, `message-rail` and `campaigns` are **C# gateway controllers**, not Scala `/basic`
  services. They appear only in the **web SDK's** generated API
  (`web/src/__generated__/apis/`).
- Confirmed by grep: zero hits for `message-rail` or `store-offer` anywhere under
  `client/Packages/com.beamable/` or `UnrealSDK/Plugins/`.

So a plan must pick one of:

1. **Feed the gateway's OpenAPI into Unity/Unreal codegen.** Correct and reusable — it unblocks
   message-rail, campaigns and every future gateway controller at once — but it is a codegen-pipeline
   change, not a feature change, and it is the larger piece of work.
2. **Hand-write the two client calls** in each SDK against the raw requester, the way the Portal
   extensions do (`beam.requester.request({ url: '/api/store-offer/…' })`). Fast, unblocks the player
   flow, but sets a precedent of hand-rolled gateway calls in Unity/Unreal that will need unpicking.

Whichever is chosen, note that **the same gap blocks the message rail**: neither Unity nor Unreal can
opt a player in to push/email/ingame today either. Option 1 fixes both; option 2 fixes offers only.

---

## 5. What the player flow actually is

There is no existing client precedent to copy — the **web SDK's `MessageRailService`**
(`web/src/services/MessageRailService.ts`) is the only client-side federation service that exists, and it
is a good shape to mirror: a small typed service on the `beam` handle, with a
`type FederationId = 'known' | 'ids' | (string & {})` union that documents the built-ins without closing
the set.

The end-to-end flow a game client participates in:

1. A campaign sends a message through a rail (push / email / in-game). The payload carries
   `StoreOfferContract.GrantKey` — the literal key **`beam_offer_grant`** — holding the grant id, written
   by `CampaignSendPayload.Build`. This is the deep-link: the message says "you got something", and this
   is the handle to it.
2. The client reads that key out of the message it received and calls **redeem** with the grant id.
3. Or, independently of any message, the client lists **entitlements** to show a "rewards waiting"
   badge, and redeems from there.
4. Redeem moves the offer's currencies and items into the player's inventory in one idempotent
   transaction (keyed on the grant id), then closes the grant out.

Proposed client surface, per SDK:

```csharp
// Unity — sketch, not a commitment
var offers = await beamContext.StoreOffer.GetEntitlements("beamable_store");
await beamContext.StoreOffer.Redeem("beamable_store", grantId);
```

Things the client surface must get right:

- **Idempotency.** `RedeemOfferRequest.transactionId` is the client's key. A retried redeem with the same
  transaction id returns the first result; a *different* transaction id on a spent grant is refused as a
  double-claim. The SDK should generate and persist one per redeem attempt rather than leaving it to the
  game.
- **Expiry is evaluated on read.** A grant past `expiresAtUnixSeconds` reports `expired` even if nothing
  has swept it. Do not cache entitlement state across a session boundary.
- **`state` is the source of truth**, not the presence of a row — a `revoked` or `redeemed` grant still
  appears in the list.

---

## 6. Rules the client SDKs inherit

These are not stylistic; each one is load-bearing somewhere upstream.

1. **Never hardcode `beamable_store`.** The federation id is a parameter, always. A game with its own
   store passes its own id to exactly the same calls. If any client code branches on the id, the
   extension point is broken.
2. **The offer id is opaque.** Only the store that minted it can interpret it, which is why
   `federationId` travels with it everywhere. Do not parse it, display it as a name, or key anything
   durable on its shape. A catalog provider puts a catalog reference there; the default provider puts a
   per-campaign identity there.
3. **`beam_offer_grant` is a reserved payload key.** It is in `CampaignSendPayload.ReservedKeys` and in
   both Portal contracts' `RESERVED_KEYS`. A client **reads** it; a rail must never emit it.
4. **Redeem is self-only for a player.** The client can only ever redeem its own grants; passing another
   player id yields 403 unless the token is privileged.
5. **A failed redeem is not always the player's fault.** The most likely production failure is a payout
   id that is not in the realm's published content manifest — the platform's inventory service throws
   `UnknownCurrencyException` / `UnknownItemException`, and that surfaces *here*, at redeem. Surface the
   server's message rather than a generic "could not claim".

---

## 7. Known gaps and things deliberately not done

- **No message-rail client in Unity or Unreal.** Players cannot opt in to push/email/ingame from either
  SDK today. Same root cause as §4.
- **No test project for any `agentic-portal` service**, including all three rails — so
  `AuthoredOfferCodec` has no unit tests. Its Portal-side mirror (`offerContent.ts`) has 32. If the
  client SDK work adds a service test project, that codec is the first thing to cover.
- **`ListOffers` / `GetOffer` are catalog-provider methods.** The default provider returns empty and
  "not resolvable on its own" respectively — truthful answers for a provider that authors rather than
  serves a catalog, not stubs. A client should not assume a catalog exists.
- **No payment surface.** Offers are granted and claimed; nothing is priced or purchased.
  `OnPurchaseCompleted` exists for a store that settles payment elsewhere, and is server-to-server.
- **Generated files were hand-edited** (`Models.gs.cs` ×2, the two web enums, `SendBodyDto.ts`, and now
  the web SDK's `StoreOfferApi.ts` + five store-offer schemas) because the OpenAPI snapshots in-tree are
  stale — `IFederatedMessageRail` was added the same way before this work. A regeneration will drop them
  unless the source documents are updated first. Relevant to §4 option 1.

  The web SDK's are the ones with a concrete path out: `web/`'s `pnpm codegen` runs
  `beam oapi generate --engine web --host https://dev.api.beamable.com`, so once the store-offer routes
  are deployed to that host (or `--host` is pointed at a local gateway), regenerating emits them for
  real. The hand-written functions are named exactly as the generator would spell them
  (`storeOfferGetEntitlements`, `storeOfferPostRedeem`) so that regeneration replaces the file rather
  than colliding with it. **Check first that the run does not drop the two `FederationType` enum
  entries**, which are hand-added in the same tree.
- **The permission matrix is a hardcoded, fail-closed dictionary** —
  `BeamableGateway/Security/Authorization/PermissionAuthorizationHandler.cs`. A resource string that is
  not listed there returns 403 for everyone below `SuperAdmin`, silently, with no startup validation.
  `StoreOffer` is registered now (Admin/Developer read+write, Tester read-only). Any new gateway route
  needs the same. Worth a separate RFC to make that set an enum or assert it at boot.

---

## 8. Suggested shape for the next plan

1. **Decide §4** — codegen pipeline vs. hand-written calls. Everything else depends on it.
2. Land the two client-callable routes in whichever SDK surface that decision implies.
3. Add a `StoreOffer` service to each SDK with `GetEntitlements` / `Redeem`, federation-id parameterised,
   modelled on `web/src/services/MessageRailService.ts`.
4. Wire the `beam_offer_grant` deep-link: a helper that reads the key out of a received message payload
   and redeems in one call, since that is the flow every game will write otherwise.
5. Sample scene / sample map exercising: list entitlements → redeem → see inventory change.
6. Consider whether the message rail's `optIn`/`optOut` rides along, since §4 option 1 unblocks it for
   free and a player who cannot opt in never receives the message carrying the offer in the first place.

---

## 9. What React Native has now (added after this RFC was written)

RN is not blocked by §4: it runs on the web SDK, whose generated API already covers the gateway's
`/api/*` routes. So the player half exists there, and it is the reference shape for §8 step 3.

**`web/` — `StoreOfferService`**, modelled line-for-line on `MessageRailService`:

```ts
beam.use([StoreOfferService]);
const held = await beam.storeOffer.getEntitlements('beamable_store');   // OfferEntitlement[]
const res  = await beam.storeOffer.redeem('beamable_store', grantId);   // OfferRedeemResponse
```

- `StoreOfferFederationId` is the same open union the rail uses (`'beamable_store' | (string & {})`) —
  it names the default without closing the set.
- **Only the two client-callable routes are exposed.** The other five are permission-scoped or
  server-to-server; a client SDK offering them would only invite a 403.
- **The service owns `transactionId`**, keyed `federationId:grantId` for the life of the instance. This
  is not a convenience: the provider treats the same id on a spent grant as a success
  ("Already redeemed.") and a *different* id as a double-claim, so minting a fresh id per attempt turns
  a double-tap into a failure. §5's "the SDK should generate and persist one" — generated, and stable
  within a session.
- **`redeem` resolves for a refused claim.** It returns the body; `success` is on it. Same convention as
  `MessageRailService`, so the caller decides what a refusal means. 8 unit tests in
  `web/tests/services/StoreOfferService.test.ts`.

**`beam-native-mobile/Samples/ReactNative` — the Offers tab** (`app/(tabs)/offers.tsx` +
`src/beam/storeOffers.ts`): a store-federation-id input (never hardcoded), the entitlement list with
per-row claim, and the `beam_offer_grant` deep-link claimed straight off the received push, read in
`src/state/notificationContext.tsx`.

**One gap this surfaced.** The **in-game rail cannot carry `beam_offer_grant` at all.**
`InGameMessageRailService.ParsePayload` builds a `MailContent` from `subject`/`body`/`category`/
`rewards`/`expiresInSeconds` and drops everything else, and Beamable mail has no extras field the key
could ride in. The push rail passes unreserved keys straight through (`ReservedExtraKeys` does not list
it), so the deep-link works there and only there. An in-game recipient has to find the grant through
the entitlements list. That is a hole in the feature, not in the client — worth its own decision.
