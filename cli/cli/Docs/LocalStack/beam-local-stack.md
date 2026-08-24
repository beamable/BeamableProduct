---
name: beam-local-stack
description: Run, inspect and debug the full local Beamable stack in this workspace — the docker deps, the C# hosts, the Scala services, the portal and its extensions — with `beam local up/ps/logs/stop`. Use it to find which repository a service lives in, which step name to pass to `--only`/`--skip`, and why a step is not coming up.
---

# The local Beamable stack

`beam local` runs a whole Beamable backend on this machine from a single JSON manifest. The manifest
lists processes (*steps*) in the order they must come up, each with an optional readiness gate, and the
orchestrator launches them, waits for each gate, and tears them all down together.

The manifest is machine-specific — it holds absolute paths into four or five repository checkouts. The
generated section at the bottom of this file describes **the manifest in this workspace**, including the
exact step names the commands below take.

## Commands

| Command | What it does |
|---|---|
| `beam local init` | Write (or update) the manifest. Prompts for each value; `--quiet` accepts every default |
| `beam local up` | Bring the stack up, in manifest order, waiting on each readiness gate |
| `beam local ps` | Show what is running, from the recorded run-state |
| `beam local logs [step]` | Tail one step's log, or every step's |
| `beam local stop [step]` | Stop one step, or the whole stack |
| `beam local validate` | Check the manifest without running anything |

Every command takes `--config <path>` to point at a manifest other than
`<workspace>/.beamable/local-stack.json`.

### `beam local up`

```
beam local up                      # everything the manifest enables
beam local up --only "<step>"      # just these steps (comma/space separated, exact names)
beam local up --skip "<step>"      # everything except these
beam local up --build              # also run the build steps (see below)
beam local up --detach             # leave it running and return the prompt
beam local up --no-web-registry    # fast path: skip the local web package registry steps this run
beam local up --with-web-registry  # run them this run even though `init` turned them off
beam local up --save-logs          # keep logs under .beamable/local-stack-logs/run-<id>
```

`--only` and `--skip` take **step names verbatim** — the strings in the table at the end of this file.
They contain spaces and colons (`scala: gateway`, `build: c# gateway`), so quote them.

`up` also overrides manifest values for one run: `--host`, `--portal-url`, `--java-path`. When the saved
login is no longer valid (typically after a docker cleanup wiped the database) it creates a local realm;
`--no-create-realm` turns that into a warning instead.

### Build steps

A step marked `build` in the manifest is a compile step, not a service. It runs to completion before its
run step and is **skipped on a plain `up`** — pass `--build` to run it.

Two exceptions worth knowing:

- A build step that declares a `requiredOutput` runs on its own when that output is missing, so a fresh
  clone self-heals instead of launching a binary that was never built. The .NET hosts do this; their
  declared output is exactly the binary their run step launches.
- The slow builds (`build: scala`, `build: portal deps`) declare no output on purpose — a surprise
  multi-minute `mvn clean package` on a plain `up` is worse than an error. Run them with `--build`.
- The web-registry steps are also flagged `build`, but `up` opts them in whenever the manifest's
  `webRegistry` choice is on (see below); `--no-web-registry` opts back out for one run.

### The local web package registry

When it is on, `up` starts a local npm registry, publishes this repo's `@beamable/*` web packages to it,
and repins the portal extensions at those versions — so the portal runs **your** web SDK build. Leaving it
off is faster, but the portal then resolves the web SDK from the published packages, which is a common
source of "my web SDK change isn't showing up".

**The choice is made once, at `init`, and lasts until the next `init`.** `beam local init` asks whether to
run the registry — defaulting to **no**, since it only matters while iterating on `@beamable/sdk` or
`@beamable/portal-toolkit` — and records the answer as `webRegistry` at the top of the manifest.
`--no-web-registry` / `--with-web-registry` answer the question without prompting.

The three steps are **always written** to the manifest, so switching the choice never means regenerating
it. On `up`, `--no-web-registry` and `--with-web-registry` override the recorded choice **for that run
only** and never write back; `--with-web-registry` wins if both are passed.

```
beam local init --with-web-registry --force   # turn it on for good
beam local init --no-web-registry --force     # turn it off for good
```

### Logs and teardown

```
beam local logs                    # every recorded step
beam local logs "<step>" -f        # follow one step
beam local stop                    # stop the stack, non-destructively
beam local stop "<step>"           # stop a single step
beam local stop --purge            # ALSO removes container volumes — wipes the local database
```

`--purge` deletes the accounts, customers and realms in the local database. A plain `stop` never does.

## Reading the manifest

Each step is either a raw process (`command` + `arguments`), a shell script (`shell: true`), or a beam
invocation (`beam: true`, where `arguments` is a beam sub-command line and the CLI is resolved for you).
Useful fields:

| Field | Meaning |
|---|---|
| `enabled` | `false` skips the step entirely |
| `build` | Compile step — `--build` only, unless `requiredOutput` is missing |
| `group` | Consecutive steps sharing a group launch together and their gates are awaited concurrently |
| `waitForExit` | Run-to-completion (docker compose, builds) rather than a long-running service |
| `port` | The port the process binds; `up` fails fast when something else already holds it |
| `readyWhenHttp200` / `readyWhenHttpOk` / `readyWhenLogContains` | The readiness gate |
| `readyRetries` | Relaunch count for a service that can lose a startup race with a dependency |
| `stopArguments` / `purgeStopArguments` | How `stop` (and `stop --purge`) reverses a run-to-completion step |

These `${...}` tokens are substituted into arguments, working directories, environment values and URLs:

- `${host}` — the backend API host
- `${portalUrl}` — the portal frontend URL
- `${java}` — the Java 8 `JAVA_HOME` the Scala services run under, resolved at run time from
  `--java-path` / `BEAM_JAVA_HOME` / auto-detection when the manifest does not bake one in
- `${mainClass}` — a Scala service's main class, discovered at `init` time

A path still holding an `<EDIT: ...>` placeholder was never filled in. The orchestrator refuses to
resolve it, so the step cannot run — fill it in by hand or re-run `beam local init` with the matching
`--*-dir` option.

## Changing what the stack runs

To add or remove microservices and portal extensions without touching anything else in the manifest:

```
beam local init --update-services --services "<ids>" --extensions "<ids>"
```

That rewrites only the microservice/extension steps. Regenerate this skill alongside it by adding
`--skill` (this file is overwritten on every init that passes it, so it can never describe a stack you
no longer have).

## When a step will not come up

1. `beam local ps` — is the step even running, or did it exit?
2. `beam local logs "<step>"` — the failure is almost always in its own log.
3. Check the step's `workingDirectory` in the manifest against the repositories table below: a step
   pointed at the wrong checkout fails in confusing ways.
4. A readiness timeout usually means a dependency is missing rather than the service being slow — the
   docker step must be up before the C# hosts, and the Scala auth service must be up before any beam
   step, because `up` logs in before its first beam invocation.
5. A "port already in use" failure is reported before launch; find the older process (a previous `up`
   that was killed rather than stopped) and `beam local stop` it.
6. After a `stop --purge`, the local database is empty — the next `up` recreates the realm.

{{THIS_STACK}}
