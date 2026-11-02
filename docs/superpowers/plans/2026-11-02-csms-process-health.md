# C# microservice process health implementation plan

**Goal:** Implement #4946 as a local, observation-only stack on #4945.

**Architecture:** Extend the existing dedicated-thread ProcessHealthMonitor. Publish immutable all-instance snapshots, queue at most one pool heartbeat, and track recovery episodes independently of connection replacement. Send bounded transition/incident records to a dedicated diagnostics worker so logging and stack collection cannot block process verdict publication.

**Tech stack:** C# 11, runtime targets .NET 8/9/10, NUnit on .NET 10, EmbedIO.

**Spec:** Approved starting brief at `C:/Beamable/BeamableBackendProduct/docs/superpowers/specs/2026-11-02-csms-process-health-design.md`.

## Constraints

- Base is `origin/fix/microserviceHanging`, initially `f700fa8b30b85256c718cad204f3f7d3fdf3a946`; future draft PR targets that branch.
- No push, PR creation, merge, deployment, process replacement enforcement, or self-exit.
- Preserve existing `/health` status and success/failure text. Expose the proposed verdict separately.
- No additional socket probe or periodic detector. Keep event-cache correction upstream in #4945.
- Structured diagnostics are mandatory. Automatic managed stacks and durable retention remain Linux image acceptance gates.

## Review focus

- Startup with a non-primary instance not initialized must remain starting.
- Heartbeat acknowledgement racing a sample, late acknowledgement, and queued work after stop must not create unbounded probes or revive a stopped monitor.
- Repeated failed reconnects must preserve the recovery episode; idle sockets and shared silence must not imply isolated process failure.
- Logging/collector blockage must not block verdicts; incident evidence and pending diagnostic work must be bounded.
- Real HTTP requests must preserve the legacy contract and expose only cached, bounded metadata.

## Tasks

1. **Monitor and verdict:** Write focused failing NUnit tests for heartbeat/start/stop/recovery and all-instance aggregation, then implement immutable snapshots, controlled monotonic sampling and recovery episodes in the existing monitor. Initial observation parameters: heartbeat 30s, heartbeat recovery 5s, failed socket recovery 120s, sibling reply window 30s. These are implementation observation values, not approved production policy defaults.
2. **Health and diagnostics:** Write failing tests for HTTP compatibility, non-primary visibility, incident deduplication, bounded logging and collector failure. Inject the monitor into the existing diagnostic server, add `/health/process`, and implement a bounded dedicated diagnostics worker. Managed-stack capture must explicitly report unavailable unless a supported collector is configured; never label the watchdog's own stack as all-thread evidence.
3. **Validation and local commit:** Run focused tests, full microservice suite, supported-target builds and real transport coverage. Recheck #4945 head/CI/event-cache finding. Document gaps and commit only #4946 files with the Codex attribution trailer.

## Execution record

- User authorized immediate implementation from the approved brief; no additional design approval round.
- Upstream recheck: f700fa8, ready for review, microservice CI success; several Windows/Unity checks in progress. Event-cache eviction finding remains outstanding.
