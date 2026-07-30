# Pull Request CLI (`beam org pr`) — Implementation Plan

## Context

Beamable's backend has a **manifest pull-request** system: a governed, cross-org workflow where someone proposes a change to a target realm's root manifest and a target-realm admin reviews/approves it before it deploys (`BeamoPullRequestController` / `PullRequestService` in the BeamableAPI repo). The generated CLI client already exists (`BeamBeamopullrequest.gs.cs`) but there are **no CLI commands** exposing it. This plan adds a `beam org pr <command>` command group wrapping those endpoints so users can submit, review, comment on, approve, and reject manifest PRs from the CLI.

Confirmed decisions:
- **Targeting is by request scope, not options.** The API resolves the target scope from the `X-BEAM-SCOPE` header (built from the current context's `cid.pid`), so every `pr` command acts on the **current realm**. To target a different realm/customer, use the standard global `--cid`/`--pid` overrides — exactly like the rest of the Beamo command family. There are no `--target-*` options.
- `submit` v1 takes its proposal from a **realm snapshot**: `--source-realm <pid>` (required) snapshots that realm's current manifest and proposes it into the current (target) realm, setting `SubmitPullRequestRequest.sourceScopeId`. There is no "current realm" source option — the target *is* the current realm, so snapshotting it would be a no-op. File/id manifest sources are deferred (they'd require new type-mapping into the PR `proposed` payload, which has no existing converter).
- The single-PR / approve flows render the **server-computed diff** (`ManifestDiffResponse`); the CLI does not compute diffs itself.

> Scope note: the backend generalized "realm" to "scope" (a realm or a zone), and a PR source must match the target's kind. Zones are not fully implemented yet, so the CLI treats scope ids as realm ids (pids) for now and ignores zones. Fields are named `scope` on the wire (`sourceScopeId`) but exposed to users as `--source-realm`.

## Prerequisite wiring

1. **Register the PR API in DI.** `IBeamBeamopullrequestApi` is not registered. Add to `beamable.common/Runtime/OpenApiSystem.cs` (next to line 91, the bundle registration):
   ```csharp
   builder.AddOrOverrideScoped<IBeamBeamopullrequestApi, BeamBeamopullrequestApi>();
   ```
   Resolve in commands via `args.Provider.GetService<IBeamBeamopullrequestApi>()`.

## New command group + subcommands

Create files under `cli/cli/Commands/Organization/PullRequest/`, namespace `cli` (match sibling org commands like `GameListCommand.cs`). All leaf commands implement `ISkipManifest` (they need cid/pid config but not the local Beamo manifest, mirroring `GameListCommand`). Enforce naming rules: kebab-case names/options, descriptions start uppercase, no trailing period, each description unique.

### `PrCommand.cs` — group
`CommandGroup` named `pr`, description e.g. "Commands for manifest pull requests". Also host a shared static diff renderer here (see below).

### `submit` → `PostPrs` (`AtomicCommand`)
- **Options:** `--source-realm <pid>` (required; snapshot this realm's manifest as the proposal), `-c` / `--comment <text>` (optional PR comment).
- **Guards** (throw `CliException`): `--source-realm` missing/empty.
- **Handle:** build `SubmitPullRequestRequest` with `sourceScopeId` = `--source-realm` value; set `comment` when provided; leave `proposed` and `aclWidenings` empty (backend materializes the snapshot and widens ACLs at approve time). Call `PostPrs(req)` — the target scope is the current context. Return `{ id = response.id }`.

### `list` → `GetPrs` (list overload) (`AtomicCommand`)
- **Options:** optional `--status <Pending|Approved|Rejected|Superseded>`, `--limit`, `--offset` (all map to the `Optional<...>` params).
- **Handle:** call `GetPrs(limit, offset, status)`. Return an array projected from `ProposedManifest` (id, status, sourceScopeId, submittedByAccountId, created, resolvedAt, resultManifestId).

### `diff` → `GetPrs` (single overload) (`AtomicCommand`)
- **Options:** `--id <pullRequestId>` (required; guard with `CliException`).
- **Handle:** call `GetPrs(id)`. Render PR header info + the `diff` via the shared renderer, and return the structured `pullRequest` + `diff`.

### `comment` → `PostPrsComments` (`AtomicCommand`)
- **Options:** `--id <pullRequestId>` (required), `-m` / `--message <text>` (required, non-empty).
- **Guards:** `CliException` if `--id` missing or message null/whitespace.
- **Handle:** `PostPrsComments(id, new CommentPullRequestRequest{ message })`. Return success indicator (id).

### `approve` → `PostPrsApprove` (`AtomicCommand`)
- **Options:** `--id <pullRequestId>` (required).
- **Handle:** first fetch the PR via `GetPrs(id)` and render info + diff (shared renderer). Warn that **all other pending PRs targeting this realm will be superseded**. Then confirm with the established idiom — `args.Quiet || string.Equals("yes", AnsiConsole.Prompt(new TextPrompt<string>("... Type 'yes' to continue.")), InvariantCultureIgnoreCase)`; abort if not confirmed. On confirm call `PostPrsApprove(id)` and return `{ id, checksum, createdAt }` from `BeamoPullRequestActorManifestChecksum`.

### `reject` → `PostPrsReject` (`AtomicCommand`)
- **Options:** `--id <pullRequestId>` (required; guard).
- **Handle:** `PostPrsReject(id)`; return a success indicator.

### Shared diff renderer (static helper on `PrCommand`)
Takes `ManifestDiffResponse` and prints, modeled on `BundleDiff.Print` (`cli/Services/Bundles/BundleDiff.cs:134-184`) / `DeployUtil.PrintPlanInfo`'s list style:
- `components` → group `ManifestDiffEntry` by `change` (Added/Changed/Removed), listing `kind` + `name`.
- `references` → list bundle changes: `bundleName`: `oldChecksum` → `newChecksum`.
Used by both `diff` and `approve`.

## Registration in `App.cs`

After the existing org registrations (`App.cs:731-734`), add (nested-group pattern from `StorageGroupCommand`, `App.cs:568-574`):
```csharp
Commands.AddSubCommand<PrCommand, CommandGroupArgs, OrganizationCommand>();
Commands.AddSubCommand<PrSubmitCommand,  PrSubmitCommandArgs,  PrCommand>();
Commands.AddSubCommand<PrListCommand,    PrListCommandArgs,    PrCommand>();
Commands.AddSubCommand<PrDiffCommand,    PrDiffCommandArgs,    PrCommand>();
Commands.AddSubCommand<PrCommentCommand, PrCommentCommandArgs, PrCommand>();
Commands.AddSubCommand<PrApproveCommand, PrApproveCommandArgs, PrCommand>();
Commands.AddSubCommand<PrRejectCommand,  PrRejectCommandArgs,  PrCommand>();
```

## Critical files

- **Modify:** `beamable.common/Runtime/OpenApiSystem.cs` (DI), `cli/cli/App.cs` (registration).
- **Create:** `cli/cli/Commands/Organization/PullRequest/PrCommand.cs` + one file per subcommand (`PrSubmitCommand.cs`, `PrListCommand.cs`, `PrDiffCommand.cs`, `PrCommentCommand.cs`, `PrApproveCommand.cs`, `PrRejectCommand.cs`).
- **Reference (reuse patterns, do not duplicate):** generated API `BeamBeamopullrequest.gs.cs` + models in `Models.gs.cs` (`SubmitPullRequestRequest`, `ProposedManifest`, `GetPullRequestResponse`, `ManifestDiffResponse`, `CommentPullRequestRequest`, `PullRequestStatus`); confirmation idiom in `ServicesPromoteCommand.cs` / `ReleaseDeploymentCommand.cs:144-153`; diff list-printing in `BundleDiff.cs`; command shape in `GameListCommand.cs`.

## Testing / verification

1. **Build:** `dotnet build cli.sln` — must compile clean.
2. **Wiring smoke test:** `dotnet run --project cli/cli -- org pr --help` shows the group; `... org pr submit --help`, `list --help`, `diff --help`, `comment --help`, `approve --help`, `reject --help` show correct options/descriptions.
3. **Guards:** `... org pr submit` with both/neither source → CliException; `diff` / `comment` / `approve` / `reject` without `--id` → CliException; `comment --id X -m ""` → CliException; `list --status <invalid>` → CliException.
4. **Unit tests** (`tests/PullRequestTests/PrCommandTests.cs`, extend `CLITest`): mock `IBeamBeamopullrequestApi` via `Mock<T>` and assert each command calls the expected method with the right id and that guards throw. Confirm `NamingPass` still passes (unique descriptions, kebab-case).
5. **End-to-end** (real realm, via `./dev.sh` from repo root then global `beam`): point the CLI at the target realm using the standard global `--cid`/`--pid` (or the configured current realm), then `beam org pr submit --source-realm <source-pid> --comment "test"`, `beam org pr list`, `beam org pr diff --id <id>`, `beam org pr comment --id <id> -m "hi"`, `beam org pr approve --id <id>` (and `--quiet`), `beam org pr reject --id <id>`.
