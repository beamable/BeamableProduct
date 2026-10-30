# C# microservice socket reliability

Design for beamable/BeamableProduct#4834: outbound requests that never get a reply, connections that stop carrying
traffic in one direction, and realm-wide work pinned to the first connection.

Code references are to this branch. Gateway references are to BeamableBackend `main` @ `cc51da0e9`.
dotnet/runtime references are to tags v8.0.6, v9.0.2 and v10.0.0.

## 1. Request lifecycle

Every outbound platform request (`MicroserviceRequester.BeamableRequest`) owns one listener in its connection
context's pending table.

```
              AddListener                    send ok (stamp SentOn, SentAt)
  (new) ───────────────────► REGISTERED ───────────────────────────────► SENT
                                 │                                        │
                                 │ terminal send failure                  │ reply │ deadline │ connection lost │ shutdown
                                 ▼                                        ▼
                               ENDED ◄───────────────────────────────── ENDED
```

**Invariants**

1. **Exactly one ending.** Every path that ends a request first *claims* its listener with an atomic
   `TryRemove` (`SocketRequesterContext.TryClaim`). Only the claimant completes the promise. The claiming paths are
   a reply, deadline expiry, terminal send failure, a dropped connection, and shutdown. A path that loses the claim
   does nothing.
2. **Balanced accounting.** The auth daemon counts each attempt once when it is sent, and once more when its listener
   ends, however it ends. A gateway-403 retry is a new attempt with its own count.
3. **No sending after the end.** Before each send attempt, `SendMessageSafely` checks that the listener is still
   pending. An expired request is never sent, or retried, afterwards.
4. **Deadlines cover the whole wait.** The caller awaits the listener, not the send. Waiting for a socket, for auth,
   or behind a blocked send cannot hide expiry. A 403 re-auth retry keeps the original absolute deadline.
5. **Only requests sent on a connection die with it.** When a connection drops, only listeners whose `SentOn` is that
   connection fail. Requests that have not been sent yet go out on the replacement.
6. **Late replies are expected.** Ids of requests that ended without a reply are kept in a bounded record (4096 ids).
   A reply for one of them logs at Debug. A reply for an id never seen still logs at Error.

**Replies to inbound requests** are bound to the connection the request arrived on and do not wait for auth:

- The gateway matches a reply to the session that sent the request (`ThoriumSocket.scala` `providerCallbacks` is per
  session, keyed by a per-session counter). The same id on another session can complete a *different* request, so a
  reply is never moved to a replacement connection.
- The gateway does not check session auth for replies (`handleRawMessageInternal`: `Right(rsp) => handleResponse`). A
  reply that waited for a stalled re-auth lost the player's request for nothing.
- A reply whose connection closed is dropped with a Warning. The player's request was lost with the connection, and
  the gateway times it out.

## 2. Connection lifecycle (`EasyWebSocket`)

```
 CONNECTING ──► OPEN ──► TORN DOWN (exactly once)
                  │         ▲
                  └─────────┘  any detector: receive error, peer Close frame, send failure,
                               send blocked past WS_SEND_TIMEOUT_SECONDS, Abort(reason) from the service, shutdown
```

`Teardown` runs once per connection (an Interlocked guard), in this order:

1. Complete the send queue and fail every queued send, so nothing waits on a dead connection.
2. Fire `OnDisconnected`, so recovery (a reconnect) starts before any close handshake.
3. Abort the socket when it is presumed dead. Otherwise attempt a graceful close, bounded at 5 s, then abort.

Why this order: `ClientWebSocket.CloseAsync` sends its Close frame through the same send mutex as data frames
(`ManagedWebSocket.cs` v8 `:1267-1274`, `:413-423`). With a blocked send, an unbounded close never finishes, and on
`origin/main` that kept `OnDisconnected` from firing at all.

Send completions run their continuations asynchronously (`TaskCompletionSource` with
`RunContinuationsAsynchronously`), so caller code never runs on the send loop.

## 3. Detecting a broken connection

| Detector | Catches | Default |
|---|---|---|
| Receive error or reset | the peer or the network closed the connection | always on |
| Send failure (send loop supervised) | a write that throws | always on |
| **Send blocked** past `WS_SEND_TIMEOUT_SECONDS` | a peer that stopped reading, or a full TCP window | 30 s, enforced only in enforce mode |
| **Reply stall** past `WS_STALL_TIMEOUT_SECONDS`: the oldest *sent* outbound request has had no reply, and no reply of any kind has arrived on that connection since it was sent | the incident shape: outbound frames leave but nothing comes back for them | 60 s, enforced only in enforce mode |
| **Gateway 403 right after a fresh auth** | a session the gateway's breaker ended: it sets `valid=false` and never resets it | always detected; the connection is replaced only in enforce mode |
| `KeepAliveTimeout` (net9+) | dead peer on an **idle** connection only | off (`WS_KEEPALIVE_TIMEOUT_SECONDS=0`) |

`KeepAliveTimeout` is not a primary detector. .NET only sends a ping after `KeepAliveInterval` with no inbound data
(`ManagedWebSocket.KeepAlive.cs` `OnDataReceived`), so a connection that keeps receiving traffic is never probed.
`ClientWebSocketKeepAliveCharacterizationTests` pins this down.

The reply-stall detector needs outbound traffic on the connection. An active round-trip probe would cover idle
connections, but the gateway has no side-effect-free endpoint: `gateway/nonce` rotates the session nonce and would race
re-authentication. That needs a backend decision (section 8).

## 4. Shadow and enforce modes

`BEAM_SOCKET_RELIABILITY_ENFORCE` (default `false`) selects how *new policies* behave.

| | shadow (default) | enforce |
|---|---|---|
| Outbound request past its deadline | Warning log plus `beam.ws.request.deadline_exceeded` metric (`enforced=false`); the call keeps waiting | the call fails with `RequestTimeoutException` (504) |
| Send blocked past the send timeout | Warning plus metric | connection torn down and replaced |
| Reply stall, or 403 after fresh auth | Warning plus metric | connection torn down and replaced |

The defects behind the incidents are fixed in **both** modes, because they have no legitimate behaviour to preserve:

- accounting
- ending each request exactly once
- terminal send failure
- disconnect and shutdown cleanup
- replies without the auth wait
- bounded close
- the supervised send loop
- async send continuations
- the auth daemon drain
- unobserved-task logging
- root-connection failover
- content catch-up

Auth requests (`gateway/nonce`, `gateway/auth`) always have an enforced deadline of `min(timeout, 15 s)`, or 15 s when
the timeout is disabled. The daemon's retry loop exists for failed attempts, and a hung one wedged it.

## 5. Timeouts across layers

| Layer | Setting | Value | Unit / source |
|---|---|---|---|
| Gateway HTTP relay (player → gateway) | akka `request-timeout` | 60 s in `disruptorbeam.conf:78`; incidents show 120 s in prod | seconds, gateway config |
| Gateway → service request | `socketSession.responseTimeoutSecs` | 10 s (code default), 30 s (`prod-rest.json.erb`) | seconds |
| Gateway → service, per request | max of `X-BEAM-TIMEOUT` / `X-DE-TIMEOUT` / `X-KS-TIMEOUT` | header | **seconds** (`ThoriumSocket.scala:693-705`); the Unity Editor sends 30000 (meant as ms) |
| Gateway breaker | `socketSession.maxConsecutiveTimeouts`, `consecutiveTimeoutClosureEnabled` | 10, true (code); **false** in `prod-rest.json.erb` | count |
| Gateway session auth | `thor_socket.sessionLengthMs` | 1 h | ms |
| Gateway keep-alive | `periodic-keep-alive-mode=pong`, `max-idle` | 20 s | unsolicited pong |
| Service inbound handling | `REQUEST_TIMEOUT_SECONDS` | 10 s | cooperative cancellation only |
| **Service outbound reply deadline** | `BEAM_OUTBOUND_REQUEST_TIMEOUT_SECONDS` | **30 s**, 0 = none, max 3600 | seconds |
| Service auth request deadline | derived | `min(outbound, 15 s)` | always enforced |
| Service auth wait per send attempt | `WaitForAuthorization` | 10 s × 10 attempts | requests only, not replies |
| Service startup/reconnect auth wait | `SetupWebsocket` | 2 min, then exit 12 | |
| **Service send blocked** | `WS_SEND_TIMEOUT_SECONDS` | **30 s**, 0 = off | |
| **Service reply stall** | `WS_STALL_TIMEOUT_SECONDS` | **60 s**, 0 = off | |
| Service graceful close | fixed | 5 s, then abort | |
| Service keep-alive | `KeepAliveInterval` fixed 20 s; `WS_KEEPALIVE_TIMEOUT_SECONDS` | 0 = unsolicited pong only | net9+ only |
| Service reconnect backoff | `_retryIntervalsInSeconds` | 5, 5, 10, 10, 15, 45, 60 s | |

**Ordering.** The outbound deadline (30 s) is below the reply stall (60 s), so one slow request expires before the
connection is condemned. The stall is below the 120 s relay timeout seen in production, so a broken connection is
replaced before players see more than one relay timeout per request. The 30 s default is a starting point for the
shadow-mode measurements (section 7), not a ratified value.

**Long-running calls.** Wrap them in `using (MicroserviceRequester.OverrideReplyTimeout(TimeSpan.FromMinutes(5)))`.
`TimeSpan.Zero` disables the deadline for calls made inside the scope.

## 6. Realm-wide work and the first connection

Root singletons (`ContentService`, `RealmConfigService`, `LoggingContextService`, `StorageObjectConnectionProvider`,
`BeamScheduler`) used to send and subscribe only through instance 0's connection.

- **Sending.** Every DI-resolved `MicroserviceRequester` picks its connection per request
  (`SocketContextSelector.PreferHealthy`):
  - its own scope's connection while that one is healthy (open, authenticated, not reported broken);
  - otherwise the first healthy instance's;
  - otherwise its own.

  At the root, its own is instance 0's, so realm-wide singletons fail over. Any authenticated connection of the
  service can carry a platform request. The instance's private requester (registration, auth, acks) stays on its
  own connection.
- **Events.** Every connection receives every realm event: each socket's `toAll` binding gets its own queue
  (`EventServiceProvider.scala:336-338`). This covers only the platform's realm-wide events
  (`MicroserviceRequester.DefaultEventNames`: content, realm config, logging context), whose handlers subscribe on
  instance 0's context alone.
  - When the gateway's `msgId` header is present (`MESSAGE_ID_KEY = "msgId"`, `ActiveMQMessageBus.scala:60`), the
    first instance to receive the event runs those handlers, and instance 0's context skips repeats, using a bounded
    record (1024 ids).
  - Without the header, only the primary (first healthy) instance runs them.
  - Other events are unchanged. Instance 0's context also holds that instance's own `toAll=false` subscriptions, so
    running it from other instances would duplicate those.
- **Catch-up.** After instance 0 reconnects, the content manifests already loaded are refreshed, as realm config
  already was. Events published while it was away are not lost.

## 7. Validation and rollout

**Automated, in this repository** (`microservice/microserviceTests`):

- `RequestLifecycleTests`: deadline, shadow, stuck send, terminal send failure, late reply, the reply/expiry race,
  the gateway zombie session, and a lost nonce reply
- `SocketRecoveryTests`: replies during a stalled re-auth, disconnect cleanup, zombie-session replacement, root
  failover, and event dispatch on another connection
- `EasyWebSocketTeardownTests`: a blocked send timing out, a Close frame during a blocked send, sending after a reset,
  a bounded close, and caller continuations off the send loop. These use a real `ClientWebSocket` against
  `RawWebSocketServer`.
- `SocketHealthTests`: the reply-stall detector (and that it leaves a slow request on a working connection alone),
  connection selection, event dedupe, and the reply-timeout override
- `ClientWebSocketKeepAliveCharacterizationTests`: the .NET keep-alive behaviour the design relies on

Every test in the first three groups except `ReplyRacingExpiry_CompletesEachRequestOnce` (a regression guard for the
claim race) fails on `origin/main` @ `794af4e69`. Each one fails for its intended reason: a request still pending
after its deadline, a leaked listener, unbalanced accounting, no reconnect, a reply waiting for auth, or a blocked
send or close.

**Not covered here:** exhaustive interleaving of the claim race. `ReplyRacingExpiry` samples it with 200 requests
racing their deadlines. Ending a request exactly once rests on `ConcurrentDictionary.TryRemove` being atomic, so only
one path can win the claim. Systematic exploration with Microsoft Coyote would need its binary rewriting step added
to the test build. That is a reasonable follow-up, not part of this change.

**Rollout**

1. **Release with shadow defaults.** Watch these for two weeks:
   - `beam.ws.request.deadline_exceeded{enforced=false}` (with the path, it gives the reply-time tail used to ratify
     the 30 s default)
   - `beam.ws.connection.unhealthy{reason}`
   - `beam.ws.request.connection_lost`
2. **Canary.** Set `BEAM_SOCKET_RELIABILITY_ENFORCE=true` on the affected customer's service. Success means:
   - gateway `Request timeout encountered` for that service near zero outside deploys
   - no restart needed to recover
   - each `beam.ws.connection.unhealthy` followed by a reconnect within `WS_STALL_TIMEOUT_SECONDS + 15 s`
   - no rise in caller 504s versus the shadow baseline
3. **Statistical bar.** Six restart-only incidents in 23 days is about 0.26 a day. If the rate were unchanged, 12
   incident-free days would happen by chance under 5% of the time, and 30 days about 0.04%. This assumes incidents are
   independent and steady; check that against traffic.
4. **Make enforce the default** in a later runtime release, with a changelog entry.

## 8. Open questions for the backend

1. Which deployment terminates C# microservice websockets in production (`rest` with breaker closure disabled, or
   `gateway` with code defaults)?
2. Can a `toStrict(5.seconds)` failure in `RpcHelper.webSocketInSink` leave a streamed message undrained and stall the
   inbound side of that connection? The service fragments messages over 64 KiB.
3. Is `PlatSingleThread`, one thread for every socket session on a node, observed to saturate, and do relay timeouts
   cluster by gateway host?
4. Should `SocketSession.end()` close the websocket (#947) and leave `valid` alone, so the service sees a disconnect
   instead of a 403 storm?
5. Can the gateway offer a side-effect-free round-trip endpoint (`gateway/ping`) for an idle-connection probe?

### Test specs for the gateway (BeamableBackend)

These reproduce the gateway-side candidates. They belong in an akka-http test against `RpcHelper.websocket`.

1. **Streamed inbound message stall.**
   - Open a websocket, and send a 200 KiB text message as four 64 KiB frames, with 6 s between the last two frames
     (past `toStrict(5.seconds)`).
   - Then send a small `basic/` request.
   - Expect: the second request is answered, and the session is not wedged. If it never is, inbound processing stalls
     after a `toStrict` timeout.
2. **Node-wide `PlatSingleThread` stall.**
   - With two sessions on one gateway, block `BackgroundTask.singleThreadContext` for 15 s, for example with a
     `Future` that sleeps on it.
   - Expect: provider requests on *both* sessions hit `scheduleRequestTimeout`. This shows that one slow handler times
     out unrelated services.
3. **Breaker zombie (#947).**
   - With `consecutiveTimeoutClosureEnabled=true`, let 11 provider requests time out.
   - Expect: the websocket closes and the client receives a Close frame, `end()` runs once, and pending provider
     callbacks fail immediately.
   - Also expect that after `end()` a `basic/` request is not answered with 403 forever. Today `valid` stays `false`.
