# C# microservice process health (#4946)

This adds process-health observation to the socket reliability work in #4945. It does not enable process replacement or self-exit. The existing `/health` status and response are preserved, including its first-instance checks. A process-unhealthy observation therefore does **not** make ECS replace a task in this implementation.

## Runtime contract

The existing `ProcessHealthMonitor` remains the only periodic scheduler. Once a second its dedicated thread captures every configured instance and queues a small pool heartbeat. At most one heartbeat is outstanding. A late callback acknowledges only its own probe; it cannot revive a stopped monitor.

Startup remains `Starting` until every configured instance has initialized. Subsequent states are `Healthy`, `Degraded`, `Unhealthy`, and terminal `Stopping`. Snapshots contain stable reason codes, monotonic transition timestamps, pool queue/completion/thread counters, and per-instance connection generation, authentication, pending request count, oldest pending age, reply age, unanswered age, and recovery age. They contain no request payloads or player identifiers. Last successful send is not available from the upstream contract and is not inferred from request creation time.

If socket sampling throws, heartbeat evaluation continues. The verdict becomes `Degraded` with `monitor_sample_failed`, unless an existing unhealthy incident must be retained. Socket metadata and pool counters then come from the prior successful sample and may be stale; an existing unhealthy reason is preserved rather than reporting recovery.

Observed generations follow actual `IConnection` identity across samples, retaining identity through disconnected gaps. Intermediate connections created and lost entirely between samples are not individually counted. Authentication requires a successful `gateway/auth` response whose pending request was sent on that actual connection, plus no auth currently pending. An empty auth counter left by the previous session cannot certify a replacement.

Existing `SocketRequesterContext.CheckForStall` policy is retained. Its logger and metric listeners can execute synchronously, so the monitor hands checks to one dedicated worker with a single coalesced pending signal. Another dedicated worker delivers new process diagnostics. Neither worker is a second periodic detector. A blocked process-diagnostic sink cannot prevent socket checks or heartbeat sampling. A provider blocking inside an upstream socket check can still delay subsequent socket checks; the watchdog and process diagnostics continue independently. Exceptions increment `SocketCheckFailures` and retain `LastSocketCheckErrorType` (at most 128 characters), without exporting exception messages or calling the failed logger. These appear in the next cached snapshot and subsequent transition logs; a failure does not independently trigger a log. The worker continues on later handoffs. Shutdown wakes all workers and waits at most 1.5 seconds in total; an externally blocked worker is left as one background thread, never replaced by additional workers.

## Initial observation parameters

| Signal | Value | Behavior |
| --- | --- | --- |
| Unacknowledged pool heartbeat | 30 seconds | `Unhealthy`, `thread_pool_heartbeat_missed` |
| Recovery after heartbeat acknowledgement | 5 seconds with an acknowledgement at every sample | A sample without acknowledgement restarts the recovery interval |
| Continuous socket recovery episode | 120 seconds | Candidate escalation only with recent usable sibling progress |
| Recent sibling reply | At most 30 seconds old | Corroborates an isolated recovery failure |
| No instance with a live connection | 30 seconds, then every 30 seconds while it lasts | Error record; stays `Degraded`, `no_live_connections` |

These internal observation values are not approved production enforcement defaults. Queue length alone does not determine health. Request age alone does not determine socket failure; the upstream stall policy must report it. Recovery age survives unauthenticated reconnects. A reply during the episode or a successfully authenticated replacement ends that episode. A later unanswered request on the recovered socket must cross the stall threshold again. If only one socket exists, all sockets fail together, or sibling replies are stale, socket failure remains `Degraded` (`socket_recovery_pending`). When no instance has a live connection, the reason is `no_live_connections` instead (#4958). A live connection is one whose instance has initialized and that is open and authenticated. A connection that is open but has stopped answering still counts as live here; reply stalls keep the handling above. `no_live_connections` also stays `Degraded` and opens no incident: one process can't tell its own failure from a platform outage, where replacing every process at once would make things worse. The process keeps reconnecting, and an error record repeats every 30 seconds while it lasts, starting 30 seconds in, so that a short loss of every connection, such as a gateway deploy, is not reported. Only one of these records waits in the diagnostics queue at a time; repeats behind a slow sink are counted as dropped, so they can't crowd out the record of an incident. Before every instance has initialized, the state is `Starting` and nothing is reported; a service that can't start still exits. A failed socket with recent usable sibling traffic can become `Unhealthy` (`isolated_socket_recovery_exhausted`). Sibling traffic does not prove route-specific dependencies healthy. No socket observation enables process replacement.

## HTTP and incident evidence

- `/health`: same HTTP 200/409 behavior and legacy JSON string body (`"Healthy: true"` / `"Healthy: false"`). Adds `X-Beam-Process-Health`, `X-Beam-Process-Health-Reason`, and `X-Beam-Process-Health-Observation-Only: true`.
- `/health/process`: JSON containing the cached process observation and all configured instances. Always observation-only; it does not sample sockets or start collection. Diagnostic dropped/failed counters are response headers.
- `/health/process/incident`: the first snapshot of the latest unhealthy incident, or JSON `null` before one occurs. This is in-memory evidence and is lost at restart.

A change in state or reason queues a transition record. Reason changes during an open incident are logged at informational severity under the same incident ID. The first `Unhealthy` verdict creates one incident ID and one error record. The incident and its first snapshot survive subsequent `Degraded`/`Unhealthy` transitions: stale sibling evidence changes confidence in the verdict, not whether the failure recovered. Only a `Healthy` verdict closes the incident, emits a recovery record, and rearms incident capture. Structured records include at most 32 instances, prioritizing failed instances, with the total count and truncation flag; the cached snapshot retains the entire configured set. The transition queue holds at most eight records. Full queues increment a dropped counter and keep the latest incident snapshot; sink exceptions increment a failed counter. The consumer blocks when idle and wakes for records or shutdown. A permanently blocked or failing sink cannot guarantee delivered logs. Verdict publication never waits for it.

Each incident explicitly reports `managed_stacks_unavailable; collector_not_validated`. **No automatic managed-stack collector or dump upload is implemented.** The watchdog's own stack is not evidence of other managed threads. Before adding/enabling collection, validate a supported Linux image collector, denied access, hard collection time/output bounds, bounded stack-text extraction, and an approved durable destination. A dump is not stack text. See Microsoft's [dump collection permission guidance](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/faq-dumps).

## Validation and rollout gates

The stack incorporates #4945's `c3b40a3f21340589b5d2943cde32b8875d6d013d` event-cache fix. Its two eviction regressions must pass along with process health tests. The fix remains in the upstream commit, outside the #4946 diff.

Local validation on November 2, 2026, against that combined stack:

- Full microservice suite on Windows/.NET 10 after review fixes: **412 passed, 0 failed**, including 28 new process-health tests and both upstream eviction regressions. The pre-existing explicit `ZoneService_BootsInZoneScope_AndHandlesTraffic` test was not executed (also excluded in the baseline). HTTP tests use the real EmbedIO listener; socket contract tests use the real requester with `TestSocket` transports. Existing real-loopback WebSocket tests are included in the full suite.
- Release builds of the runtime and isolated probe passed for .NET 8, 9, and 10 with `SKIP_GENERATION=true`. Builds retain existing package/compiler warnings; no new dependency was added to the runtime.
- Separate-process starvation probes passed on .NET 8/9/10. Unhealthy heartbeat observations arrived at approximately 30.34/30.34/30.35 seconds, respectively, and pool recovery was observed at 36.42/36.41/36.41 seconds. These are local Windows results, not Linux/ECS recovery measurements.
- Regression tests first reproduced incident loss when sibling evidence became stale, premature heartbeat recovery after one acknowledgement, and missing socket-check failure evidence. All pass with the fixes. The combined process-health/socket-recovery selection passed 35 tests. The reconstructed paused full-service reconnect test also passed five consecutive separate runs.
- Independent read-only source review covered concurrency, lifecycle, recovery evidence, and HTTP compatibility; material findings were corrected and focused regressions passed.
- The upstream branch was rechecked after the review fixes and remains `c3b40a3`. At the initial implementation's CI check, microservice CI passed; Windows CLI, Windows build, and some Unity checks were still running. CI was not rechecked during the local review fixes. Earlier changes-requested reviews are not an approval of this new stack.

Run focused coverage with:

```powershell
dotnet test microservice/microserviceTests/microserviceTests.csproj -c Release -p:SKIP_GENERATION=true --filter FullyQualifiedName~ProcessHealth
```

`microservice/processHealthProbe` deliberately starves its **own** process's pool for 30 seconds, then verifies detection, delayed acknowledgement/recovery and bounded stop. Run it as a separate process; never inject pool starvation into a shared test host or deployed task:

```powershell
$env:DOTNET_ThreadPool_UseWindowsThreadPool='0'
dotnet run --project microservice/processHealthProbe/ProcessHealthProbe.csproj -c Release -f net10.0 -p:SKIP_GENERATION=true
```

The probe uses a service without a socket: pool recovery returns it to `Degraded` and retains the incident until the process is healthy. It does not demonstrate a healthy microservice, HTTP service under starvation, gateway behavior, managed stack capture, or ECS replacement.

Sampling still scans pending requests to calculate oldest request age. A single-thread Windows/.NET 10 probe measured roughly 1.6–2 ms per snapshot at 100,000 pending requests, without concurrent mutation. This does not establish latency under constrained Linux CPU or request contention. Sibling corroboration is computed once per sample; diagnostic delivery has no idle polling. The separate workers remain necessary to isolate blocking providers.

Still required before production policy decisions:

- Combined real-gateway one-way faults on non-primary instances, a reconnect while an event handler remains in flight, sustained failed reconnects, and route-specific/all-platform slowness. Mock transport tests are narrower evidence.
- Linux runtime starvation and collector tests, plus durable evidence retention. An early paused full-service reconnect probe timed out during re-authentication cleanup; its original cause remains unknown. A reconstructed regression now pauses the full service before the replacement's auth wake, verifies that it remains in recovery, then completes real daemon authentication, provider registration, and shutdown using `TestSocket` gateway responses. This is local integration evidence, not a real gateway or ECS timing guarantee.
- Producer-generated deployment manifest, actual task-definition health check, port and command availability, non-success HTTP exit behavior, ECS service replacement/readiness, and the affected October 29 task's revision and retained health/event history. Source defaults cannot establish deployed wiring or explain the incident.
- At least one week of canary observations, with false-positive review and separately authorized enforcement. This change supplies no enforcement switch and cannot establish a two-minute ECS recovery objective by itself.
