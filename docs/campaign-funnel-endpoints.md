# Campaign funnel: what changed in the endpoints

The platform reworked how campaign funnels are counted. Funnel stages are no longer a separate,
name-routed code path — they are ordinary watched-goal rows parked by the send node, counted from the
same records everything else is counted from.

This guide is for the SDK and CLI side: what a client must send for an open or a click to be
counted, and what it gets back when it reads a funnel.

**Short version:** every stage a client reports is now spelled in the reserved `beam_` namespace
(`beam_opened`, not `Opened`), and the `category` field is gone. An old spelling is accepted with a
`202` and counts nothing. The funnel **read** endpoint also changed in two breaking ways (§2).

---

## 1. Sending stages — the names changed

`POST /analytics/events` keeps its route, envelope, headers and validation. The body is a JSON array
and the response is `202` unconditionally, so per-event failures are invisible to the caller — they
always were.

```json
[{ "op": "g.core", "e": "beam_opened",
   "p": { "outreachId": "<the beam_outreach from the push>",
          "trackId": "campaign:c:1:send",
          "funnelType": "Opened" } }]
```

`playerId` / `accountId` come from the token, never from the body.

### Stage names on the wire

| what happened | was | is now |
|---|---|---|
| rail handed the message to its provider (rail only) | `Sent` | `beam_sent` |
| push arrived / rail delivery ack | `Received` (handset), `Delivered` (rail) | `beam_delivered` |
| notification opened / mail read / email pixel | `Opened` | `beam_opened` |
| tapped through / email call-to-action | `Clicked` | `beam_clicked` |
| rail ops signals (BI only, never counted) | `Error`, `Expired` | `beam_rail_error`, `beam_rail_expired` |

`Received` no longer exists: the handset's arrival receipt and the rail's delivery ack were one fact
with two words, and are one word now. There is **no fallback** for the old spellings — the server-side
table that mapped several names onto one stage was deleted. A build emitting `Opened` counts nothing
from the moment the server deploys, so the rename is lockstep with the server.

The same names apply to the rail's out-of-band endpoint, `POST /message-rail/funnel` (`Stage` must be
`beam_delivered`, `beam_opened` or `beam_clicked`, otherwise `400 InvalidFunnelStage`), and to the
email tracking link, whose click URL is `…?s=beam_clicked`. An email link minted with `?s=Clicked` now
just returns the pixel and records nothing.

There is deliberately no wire name for a conversion. A conversion is something the runtime concludes
when a player meets an objective, so a device-reported one would count players who met nothing.

### `beam_` is reserved

An authored event may not start with `beam_`. `CampaignGraphValidator` refuses to publish a graph
whose entry triggers, inaction triggers or goal predicates name one (`ReservedEventName`), which is
what guarantees a `beam_` event on the stream came from the platform. The rule is enforced on the
**graph**, not the event: a client that fires `beam_opened` by hand still gets `202` and quietly
miscounts a live funnel. Never let user-typed event names through with that prefix.

### What a client must get right

| field | status | why |
|---|---|---|
| `e` | **required, exact** | the `beam_*` stage name. Anything else is never watched. |
| `p.outreachId` | **required, exact** | must equal the push's / mail's `beam_outreach`. It is the decisive match key; missing means nothing counts. |
| `p.trackId` | optional | the campaign path no longer reads it — the campaign and node come off the parked row the `outreachId` matched. Send it when you have it; it is useful for BI. |
| `p.funnelType` | optional, display only | handsets send a readable label (`Received` / `Opened` / `Clicked`), which the Portal's Athena-backed detail table shows as its "Step" column. The server never reads it. The rails use the same param for the *channel* (`email` / `ingame` / `notification`); that disagreement predates this change. |
| `c` / `category` | **gone** | the gateway no longer binds it and nothing reads it. It used to stop a player's own opens from enrolling them into a campaign triggered by them; the reserved prefix does that now. |

The failure mode throughout is **silence**. A wrong name or a missing `outreachId` is not rejected —
it simply never matches a parked row, and the funnel reads zero while every request returns `202`.

### Four behavioural changes worth knowing

1. **A stage is no longer admitted on its name alone.** There is no longer a list of consumed stage
   names; `beam_opened` is an ordinary watched event. The realm needs at least one live published
   campaign with a send node, or the event is dropped at the header gate before it is even decoded.
2. **Stages count on nodes that route nothing on them.** A node counts every stage regardless of which
   outcomes it declares edges for. Funnels that used to be structurally zero now populate.
3. **Out-of-order now works.** A `beam_opened` that arrives before its delivery receipt counts the
   open *and* backfills the delivery. A `beam_clicked` before delivery does not backfill — a click can
   reach a player by a path the funnel never observed, so it is not evidence of delivery. Either way,
   SDKs no longer need to order or buffer their echoes.
4. **Rejection tags changed.** Gone: `funnel_no_track_ref`, `funnel_bad_track_ref`,
   `funnel_no_outreach`, `funnel_no_account`, `funnel_stage_not_consumed`. New: `funnel_unwatched`,
   plus match tags `pid_mismatch` / `bound_failed` / `outreach_mismatch` / `already_signalled`.

Routing **outcome** keys on graph edges (`Sent`, `Delivered`, `Opened`, `Clicked`, `NoGoal`,
`beam_met_<label>`) are a different vocabulary and did not change.

---

## 2. Reading a funnel — two breaking changes

```
GET /campaigns/{campaignId}/{version}/funnel?live=true
```

`live` is new and **defaults to true**. `live=false` answers from the sealed counters alone: one query
instead of two, with `pending` reported as zero. Worth asking for on any view that is not watching a
campaign in flight, because the pending half is the one that scans the collection the event firehose
is writing to.

### 2a. Stage values are objects, not numbers

| | before | now |
|---|---|---|
| a stage's value | `long` | `{ "sealed": n, "pending": n, "lagging": bool }` |

The total is `sealed + pending`. `sealed` is settled; `pending` is still being rolled up, and a reader
may want to show it as provisional.

`lagging: true` means the pending slice was **not measured at all**. A `0` under a lagging stage means
"unknown", not "none" — do not render it as zero. It is campaign-wide and stamped on every stage.

### 2b. Stage keys were renamed

| before | now |
|---|---|
| `Sent` | `beam_sent` |
| `Delivered` | `beam_delivered` |
| `Opened` | `beam_opened` |
| `Clicked` | `beam_clicked` |
| `Goal:<label>` | `beam_goal_<label>` |
| `Converted:<label>` | `beam_met_<label>` |

There is also `beam_queued` — the campaign's own handed-to-the-rail top of funnel, above `beam_sent`.

The stage keys and the wire event names are now one vocabulary: what a device sends is what the funnel
counts under.

Two of these count different things and should be labelled differently wherever they surface:

- `beam_goal_<label>` counts players who fired at least one **observed event** of that goal. For the
  common single-event goal that is the same as meeting it; for an all-of / any-of goal it is not. Call
  it "goal events observed", not "conversions".
- `beam_met_<label>` counts players who actually **met** the goal.

### 2c. A write then an immediate read can come back stale

Results are cached per (realm, campaign version, `live`) for a short TTL, and the pending slice depends
on a background rollup pass. A CLI or test that fires an event and polls straight away should poll with
`live=true` and allow for the TTL before believing a zero.

---

## 3. Where this repo emits stages

Every emitter below now sends `beam_*` names and no category.

| emitter | file | stages |
|---|---|---|
| iOS native | `NativeSources/iOS/.../Models.swift` (`FunnelType` raw values), `Analytics/BeamableAnalytics.swift` | `beam_delivered`, `beam_opened`, `beam_clicked` |
| Android native | `NativeSources/Android/.../push/BeamableAnalytics.kt` (`FunnelType.wire`) | same |
| Unity push | `beam-native-mobile/Unity/Runtime/BeamableNotifications.cs` (`TrackOfferClicked`) | `beam_clicked` |
| Unity / C# mail | `Common/Runtime/Api/Analytics/Models/MailOpenedFunnelEvent.cs` (cli + client copies) | `beam_opened` |
| web mail | `web/src/services/MailService.ts` (`FunnelStage.Opened`) | `beam_opened` |

React Native's TypeScript layer puts nothing on the wire itself; it calls the native SDKs.

Both native enums keep the readable label for the `funnelType` param (`FunnelType.label`), so the
Portal "Step" column is unchanged. Events an older build queued for offline replay stored the old
label; both native SDKs resolve either spelling on load and send the new name.

### The mail paths require only `outreachId`

The web `MailService` and the C# `AbsMailApi` used to refuse to attribute a mail that carried an
`outreachId` but no `trackId`. The server no longer needs `trackId`, so both now report the open on
`outreachId` alone and include `trackId` only when the rail stamped one.

### The funnel read lands via codegen, not by hand

The only funnel reader here is generated: `web/src/__generated__/apis/CampaignApi.ts`. There is no CLI
campaign command. `pnpm codegen` pulls the spec from a live host, so the new shape and the `beam_*`
keys appear only once a host has the change deployed. Nothing to hand-edit — sequence it after
deployment and re-run codegen.

### Prebuilt binaries

The Android `.aar` is rebuilt and restaged by `./dev-native.sh`. The iOS xcframeworks under
`Unity/Plugins/iOS` and `Unity.Web/Plugins/iOS` are built on macOS; their `.swiftinterface` /
`.abi.json` files still carry the old literals until they are rebuilt, and are never hand-edited.

---

## 4. Testing it with the React Native sample

The sample at `beam-native-mobile/Samples/ReactNative` is the harness. Point it at a stack that has the
change — see that sample's `README.md` for pointing at a local stack, and check
`.beamable/config.beam.json` before anything else. Its committed value points at the shared dev
environment, where these changes do not exist; the funnel then reads zeros, and it looks exactly like a
broken client.

After editing Kotlin, run `./dev-native.sh` from the repo root first (the sample build aborts if the
vendored `.aar` is older than its sources). Then regenerate the build from scratch with:

```bash
npm run android:local:release -- --clean
```

Then:

1. Send a campaign to the device — `beam_sent` and `beam_delivered` move.
2. Open the notification — `beam_opened` moves, and `beam_delivered` is non-zero even if its receipt
   arrived late (that is the backfill).
3. Tap through — `beam_clicked` moves.
4. Read a campaign mail through the web SDK — `beam_opened` moves for the in-game rail.
5. Read the funnel and check for the new key names and the object-valued stages. Allow for the cache
   TTL before believing a zero, and check `lagging` before believing it is real.

The sample's objective-event screen (`src/beam/objectiveEvents.ts`) refuses names starting with
`beam_`, for the reason in §1: the server accepts such an event and it would miscount the funnel.

**Do not treat a green build as evidence.** An old-spelling event is never watched, so the funnel reads
zero while every request still returns `202`. Check that a count moved.
