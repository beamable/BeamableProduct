# Native Linux stop investigation

Diagnostics only, based on PR #4937 head `8e01b15b6cce64660464ba100f1ead29843db688`.
This branch is not a proposed product fix and should not be merged.

## Question

Unity 2021 Linux CI fails the held-stop-acknowledgement test, then later tests in
the same fixture fail to receive requests. Windows passes use a different HTTP
handler and do not establish Linux correctness. A local native Linux Editor
cannot start tests without a Linux license. Use the existing CI licensing setup
without exporting credentials.

## Comparison

The branch-only workflow runs four sequential native Linux Unity 2021.3.29f1 jobs:

1. Existing in-process Mono HttpListener, failing test alone.
2. Separate .NET 10 server, same test alone.
3. Existing in-process server, complete BeamWebCommandStopTests class.
4. Separate server, same complete class.

The external process mirrors the fixture protocol; it is not the real Beam CLI.
It removes server execution from Unity's Mono runtime while retaining the same
request IDs, held gates, response bodies, statuses and retry behavior. File gate
polling adds approximately 10 ms latency; process startup completes before test
operations. An observer handshake ensures fixture receipt signals are set before
the server proceeds, preserving the original immediate-assertion ordering. These
are explicit experimental differences.

All existing test bodies, assertions, wait deadlines and product cancellation
settings remain unchanged. Production StopImpl has passive phase observations
around the original awaits/cancellation and observes the actual timeout token,
raw HTTP task, thread/context identity, queue size and thread-pool availability.
Log writes drain on observer timers, outside the observed continuation. Timing
perturbation from instrumentation remains possible and must be considered.

## Evidence to inspect

Download `linux-stop-<server>-<scope>` artifacts. They contain the Editor log,
Unity test results, per-command `stop-*.log` and per-fixture protocol logs/signals.
Check `cancelled`, `http`, `capturedQueue` and `phase` near the ten-second deadline.
If the HTTP task remains pending after cancellation, investigate the transport;
if the task completes while its continuation remains queued, investigate Editor
scheduling. If only the in-process variant fails, investigate fixture/runtime
coupling; that result alone does not prove real CLI behavior is correct.
If both modes pass, broaden to preceding tests/full suite rather than declaring
this fixed. Process/timing changes can mask the original failure.

The Unity action step has a 12-minute watchdog and the job has 45 minutes to leave
room for setup and artifact upload. Neither changes a test or product timeout.
Always-upload handles ordinary step failure/timeout; runner loss or whole-job
cancellation can still lose artifacts. Cache is restore-only. The workflow does
not deploy, release, update a PR or modify either stack branch.

## Local checks

The self-contained comparison server passed protocol checks inside the Linux
Unity container. This validates the server only, not native Unity cancellation.
Windows Unity 2021.3.29f1 passed all 16 fixture tests with both server variants.
The first external check exposed a file-observer ordering race, corrected by the
explicit observation handshake; the final unchanged-test rerun passed 16/16.
Server protocol checks passed 4/4 on both Windows and native Linux, including
observation ordering. Workflow structure and all 10 inline shell scripts were
checked; the GitHub workflow itself has not run. Windows results cannot substitute
for the native Linux Unity comparison.

## Follow-up: observe real factory discovery

The original four-case experiment is run 41017622235 at `eddcfd6c3`.
Both isolated native Linux cases failed, with delayed worker execution; the
external case did not reach shutdown. The current workflow narrows the follow-up
to the original in-process single test and adds passive real-factory logging.
It does not change the expected version, comparisons, discovery decisions,
process lifetime, test assertions or timing limits.

`factory.log` records expected/returned version, override/ignore flags, owner
comparison, ping result, launched child PID and tracked child count, plus worker
minimum/maximum/headroom. This checks whether CI's `0.0.123.1` SDK expectation
rejects the CLI's normalized `0.0.123` reply and repeatedly launches servers.
This mismatch and discovery behavior predate the stack; causality is not yet
confirmed. The local Windows harness uses matching `0.0.123` values and passes
16/16 with this additional trace. Its real factory launches one server and
accepts it, illustrating another difference from the CI environment.
