# Out-of-process Beam stop diagnostic server

Diagnostic-only fixture replica. No SDK production files were changed.

## Launch

Linux (self-contained .NET 10, already built):

```sh
/probe/external-server/publish-linux/BeamStopProbeServer 18081 /probe/controls/run-001
```

Windows or a machine with .NET 10 installed:

```text
dotnet external-server/bin/Release/net10.0/BeamStopProbeServer.dll 18081 controls/run-001
```

Arguments are `<port> <control-directory>`. The listener binds only `127.0.0.1`.
Use a fresh control directory per fixture. Redirect stdout/stderr to a per-fixture log.
The server exits after the configured transaction(s), or when `shutdown` appears.
The parent diagnostic runner should retain its own bounded process watchdog.

## Configuration

The server opens its listener and emits `ready.json` before waiting for its first
request. Immediately after accepting that request, before parsing its body, it
reads an optional `config.json` snapshot. Thus fixture object initializers can
update final settings after process startup but before sending the first request.
Write the file atomically using a temporary file and rename.

```json
{
  "retryStop": true,
  "writeOutput": true,
  "stopStatus": 204,
  "awaitSignalObservation": false,
  "infoPids": null,
  "owner": "fixture-owner",
  "version": "0.0.123"
}
```

Without a config file, defaults are retryStop=false, writeOutput=true, stopStatus=204.
`awaitSignalObservation` defaults to false. When true, the server waits after each
execute/stop/retry received signal for the matching `observed-execute`,
`observed-stop`, or `observed-retry` gate. The fixture bridge creates that gate only
after completing its corresponding received TaskCompletionSource. This preserves
the in-process fixture's signal-before-response order across the process boundary.
Optional environment defaults: `PROBE_RETRY_STOP=1`, `PROBE_WRITE_OUTPUT=0`,
`PROBE_STOP_STATUS=500`. The config file overrides these defaults.

When infoPids is non-null, the first request and each subsequent request receive
`{"owner":...,"version":...,"pid":...}` for the configured PID sequence, matching
the original fixture's discovery branch, then the server exits.

## Gates and signals

Create an empty file to open each gate:

- `allow-headers`: send `X-Beam-Request-Id`, then `: ready\n` and flush.
- `release-worker`: after receiving /stop, optionally send the original fixture's
  `data: {"type":"test","data":{},"ts":1}\n`, then close the execute response.
- `allow-ack`: close the first stop response with stopStatus.
- `allow-retry-ack`: close the retry response with 204 when retryStop=true.
- `shutdown`: close the listener and exit without waiting for other gates.
- `observed-execute`, `observed-stop`, `observed-retry`: acknowledge bridge receipt
  of the corresponding signal when awaitSignalObservation=true. These gates must
  not be precreated; create each only after the fixture's signal is completed.

For the stalled-acknowledgement case, precreate allow-headers and release-worker;
leave allow-ack absent until the test releases it.

Signals are written atomically as `<name>.json`:
`ready`, `execute-received`, `headers-sent`, `stop-received`, `execute-closed`,
`ack-sent`, `retry-received`, `retry-ack-sent`, `info-0-sent` (etc.), `complete`, `fault`.

Signal envelope:

```json
{"utc":"...","elapsedMs":123.0,"pid":123,"data":{"requestId":"...","path":"/stop"}}
```

Signal-specific data varies. Stdout additionally records event name, thread ID,
configuration application and gate waits/opens with UTC and monotonic elapsed time.
No HTTP control endpoint is used: the test's HTTP client is exercised only by the
actual execute/stop/info protocol.

## Verification completed

- .NET 10 Release build: zero warnings/errors.
- Self-contained linux-x64 publish completed.
- Independent Python standard-library HTTP client passed on Windows:
  registration/SSE framing, held 500 acknowledgement then 204 retry, and canceled
  first client followed by released acknowledgement and successful retry.
- The self-contained executable passed those checks inside
  `unityci/editor:ubuntu-2021.3.29f1-base-3.1.0` with `--cpus=4`.
- Additional Linux check verified config replacement after ready, then two
  discovery replies from the final infoPids/owner/version configuration.
- After adding optional signal-observation gates, all four independent smoke
  cases passed on both Windows and that Linux image. The observation-enabled
  case verified that execute headers, execute closure after stop, and retry
  acknowledgement remain blocked until the corresponding observed gate appears.

Smoke invocation in that Linux image:

```sh
PROBE_SERVER_EXECUTABLE=/probe/publish-linux/BeamStopProbeServer python3 /probe/smoke.py
```

Each smoke run retains a directory containing controls, JSON signals and server.log.
These checks validate the external server protocol, not Unity client cancellation
or the native Editor scheduling. Those remain the parent investigation's comparison.

## Contract provenance

Read at repo head `8e01b15b6cce64660464ba100f1ead29843db688`:

- `BeamWebCommandStopTests.cs` fixture Serve: requestId, SSE ready/output markers,
  separately held execute completion/acknowledgement, optional retry, discovery.
- `ServerService.cs` HandleExec: registers requestId before header/SSE flush.
- `ServerService.cs` HandleStop: waits for invocation completion before route sends 204.

This process intentionally mirrors the fixture rather than invoking the real CLI;
it isolates the in-process Unity/Mono HttpListener boundary for the comparison.
