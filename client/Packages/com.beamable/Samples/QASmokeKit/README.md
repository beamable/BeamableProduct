# QA Smoke Kit

A fast smoke-test harness bundled with the Beamable Unity SDK. It exercises the most
load-bearing SDK surfaces — `BeamContext` readiness, `Accounts.OnReady` resilience, the
`BeamContext` retry buffer, and the `MustReferenceContent` content picker — so a tester can
confirm a release behaves correctly in a few minutes.

Because the kit ships inside `com.beamable`, it tracks the code it tests: when the SDK
changes, the harness imported alongside it stays in sync.

> Always run against a disposable QA realm with no real player or content data. Several of
> the recipes below plant bad tokens and force failures.

## How to read results

Every line the harness emits is prefixed with `[SMOKE]`, and each check logs either
`[SMOKE] PASS ...` or `[SMOKE] FAIL ...`. Read the verdicts straight from the Unity Editor
log rather than relaying the Console by hand:

```
grep "\[SMOKE\]" <Unity Editor log>
```

The Editor log lives at the platform-standard path (for example
`~/Library/Logs/Unity/Editor.log` on macOS).

## The scripts

A play-mode component does nothing until it lives on an active GameObject in the loaded
scene. The included `QASmokeKit.unity` scene is pre-wired with a single GameObject named
**QA Smoke Runner** that already has `SmokeRunner` attached — open it and press Play.

### `SmokeRunner.cs`

The happy-path readiness harness. Attach it to a GameObject and press Play (or just open the
included scene). It awaits `BeamContext.Default.OnReady`, logs the resolved
`cid` / `pid` / `playerId`, then awaits `Accounts.OnReady`. Touches only the most stable SDK
surface, so it compiles against the current release without edits. Extend the marked section
with stat / inventory / microservice assertions as needed.

### `SmokeRetryWatcher.cs`

A passive log watcher for fault-injection recipe B. While attached, it listens for
exceptions or errors during `BeamContext` init and emits `[SMOKE] FAIL` if it sees an
`IndexOutOfRangeException` or an overflow.

### `SmokeReferenceContent.cs`

Defines a `smoke_ref` content type that gives the `MustReferenceContent` picker an obvious,
top-level test surface. See the content-picker test below.

## Fault-injection recipe A — `Accounts.OnReady` stale-token resilience

`Accounts.OnReady` must resolve even when a stale or invalid **remembered** device token is
present, while still strictly rejecting an invalid **active** token. The relevant code is in
`Runtime/Player/PlayerAccounts.cs`. A happy-path pass will not exercise this — you must
plant a bad remembered token.

1. Sign in normally so the SDK persists a remembered token. It is stored in `PlayerPrefs`
   under a key of the form `{prefix}{cid}.{pid}.access_token` (and a matching `...refresh`
   key).
2. Plant a bad remembered token: overwrite that `PlayerPrefs` entry with garbage, or sign in
   a second account and then invalidate its server-side refresh token.
3. Re-init: restart the Editor, or re-enter Play with `SmokeRunner` attached.
4. **PASS** = `[SMOKE] PASS Accounts.OnReady resolved` appears while a genuinely bad
   **active** token is still rejected. **FAIL** = `OnReady` hangs or throws.

## Fault-injection recipe B — `BeamContext` retry-buffer overflow

Pre-fix, `BeamContext.Try()` wrote `errors[attempt]` into a buffer sized to
`CoreConfiguration.ContextRetryDelays.Length`. With `EnableInfiniteContextRetries = true`,
once `attempt` passed the array length the buffer overflowed into an
`IndexOutOfRangeException`. The fix clamps the index to the last slot. The relevant code is
in `Runtime/BeamContext.cs`.

1. In the Core Configuration asset (Beamable > Core Configuration, or the SDK settings
   window): set `EnableInfiniteContextRetries = true` and shorten `ContextRetryDelays` to
   e.g. `[1, 1]` so the array is exhausted in seconds.
2. Induce failure: turn off networking, or point config defaults at an unreachable host, so
   init keeps failing.
3. Attach `SmokeRetryWatcher` to a GameObject, press Play, and let it retry well past the
   array length (a dozen or more attempts).
4. **PASS** = retries keep cycling, the error buffer stays bounded, and no `[SMOKE] FAIL`
   line appears. **FAIL** = an `IndexOutOfRangeException` surfaces during init.

## Content-picker test — `MustReferenceContent`

Once `SmokeReferenceContent.cs` compiles, the Content Manager offers a new `smoke_ref`
content type. Create one and inspect its two fields:

- **`currency`** (`CurrencyRef`, `[MustReferenceContent]`) — the Editor shows a content
  picker. Confirm the dropdown filters to `CurrencyContent` (e.g. the default
  `currency.gems` / `currency.coins`). Test a valid pick, a cleared pick, and confirm
  wrong-type content is not offered.
- **`currencyId`** (`string`, `[MustReferenceContent(true, typeof(CurrencyContent))]`) — type
  a bogus id such as `currency.does_not_exist`. The invalid value must be **flagged** by
  validation and **not** silently auto-rewritten or cleared. Confirm the typed text stays
  put.
