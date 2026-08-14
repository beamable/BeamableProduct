# `beam local setup` — fresh-machine setup for the local stack

Gets a machine that has never run the Beamable stack to the point where `beam local up` works, on **macOS,
Windows and Linux**.

`beam local validate` is the check. `beam local setup` is the fix.

```bash
beam local setup                     # install everything into ~/.beamable-toolchain
beam local setup --toolchain-dir D:\beam-tools   # ...or wherever you want it
beam local validate --with-aws       # confirm
beam local up --build                # run the stack
```

---

## What it does

### 1. Installs a private, pinned toolchain

Downloads and verifies these into `--toolchain-dir`, then points the manifest at them. Nothing is installed
system-wide, nothing goes on your `PATH`, and no package manager (brew / winget / sdkman / nvm) is involved.

| Tool | Pin | Why it is pinned |
|---|---|---|
| JDK 8 | Temurin 8 (Adoptium) | The Scala backend is Scala 2.11 with a JDK 8 source/target |
| JDK 8 on **macOS arm64** | **Azul Zulu 8** | Adoptium publishes **no aarch64 macOS build of JDK 8** — its API 404s. The x64 build "works" but runs all ~18 Scala services under Rosetta translation |
| Maven | 3.9.9 | Maven picks its JDK from `JAVA_HOME`, so an unpinned `mvn` can compile Scala 2.11 under a JDK 17/21 from an IDE bundle |
| .NET SDK | 10.0.100 | BeamableAPI targets `net10.0` and has no `global.json`, so an unpinned build follows whichever SDK is newest |
| Node | 22 LTS | The portal is built against Node 22 (`Dockerfile` is `node:22-alpine`, `amplify.yml` does `nvm use 22`) |

Every archive is checksum-verified (SHA-256, or SHA-512 for Maven) before it is extracted, and extraction goes to
a staging directory that is renamed into place — an interrupted run never leaves a half-installed tool that looks
valid.

**Docker is checked, never installed.** It needs administrator rights and a GUI installer on both macOS and
Windows. Setup verifies the *daemon* is responding (not just that the binary exists — a stopped Docker Desktop
has a perfectly good `docker` binary and every compose step still fails) and tells you what to install.

### 2. Generates the BeamableBackend config files

Three files are gitignored, so **every fresh clone is missing them**, and `beam local up` never created them:

- `core/src/main/resources/awsglobal.conf`
- `tools/beamo/src/main/resources/server.conf`
- `tools/beamo/src/test/resources/server.conf`

The repo's own `bin/set-local-vars` renders these, but it is a Python script with a `#!.venv/bin/python`
shebang — it needs Python plus a committed virtualenv and **cannot run on Windows at all**. Setup does the same
work natively: it reads the repo's GitHub `local` environment variables and fills the `.liquid` templates.

> The README only mentions `awsglobal.conf`. The other two matter just as much — **beamo will not start without
> them**.

Needs a GitHub token, resolved from `--github-token`, then `$GITHUB_TOKEN`/`$GH_TOKEN`, then `gh auth token`.

Existing files are left alone unless you pass `--force`, so local edits survive a setup run.

### 3. Points the portal at the local backend

`<portal>/.env.local` is **gitignored**, so it is missing from every fresh clone or copied folder — and its
absence is the most confusing failure in the whole setup. The portal's `API_BASE` falls back to
`https://api.beamable.com` when `VITE_API_BASE` is unset, so a portal served from `localhost:4950` sends its
**login to production**. The local seed account (`beam@beamable.com` / `123456`) does not exist there, so login
fails while every local service is healthy and `beam local ps` is green.

Setup writes `VITE_API_BASE=<the manifest's host>`. The check is on the **key**, not the file: a `.env.local`
that exists for some other override but has no `VITE_API_BASE` still silently points at production.

Your other overrides are preserved, and a value that deliberately names a different host (dev/staging) is
reported rather than rewritten — use `--force` to repoint it.

> Vite bakes env at startup but watches `.env` files, so it restarts on its own. **Hard-refresh the browser**
> afterwards; the old modules are cached client-side.

### 4. Checks the AWS prerequisites

See the next section. Check-only — it creates, deletes and modifies nothing.

---

## The AWS prerequisite

**Real AWS access is required. There is no LocalStack anywhere in this stack.**

This is the failure worth understanding before it happens. `DefaultAWSCredentials` in BeamableBackend reads a
`~/.aws` profile and then `AssumeRole`s into per-scope roles — and the Scala **`auth`** service fetches its JWT
signing key from **AWS Secrets Manager at runtime**. So with no credentials, the stack comes all the way up,
every process reports healthy, `beam local ps` is green — and **nothing can log in**. That looks exactly like a
broken backend, which is why it gets a named preflight check instead of a line in a README.

`beam local setup --only aws` verifies, in order:

| Check | Blocking | What it proves |
|---|---|---|
| `sts get-caller-identity` | yes | The profile the backend reads has usable credentials |
| Assume the **services** role | yes | S3, ECR, ECS and Secrets Manager access |
| Assume the **storage** role | yes | Microservice container access |
| Assume the **analytics** role | yes | S3 + Athena access |
| Read the **JWT signing key** from Secrets Manager | yes | Login will actually work |
| `head-bucket` on the trials / content / geolocation buckets | warning | Those specific features work |
| The scheduler SQS queue | not checked | Reported only — see below |

Role ARNs, bucket names and the secret id are read from the **rendered `awsglobal.conf`**, never hardcoded — so
private-cloud and self-hosted setups are checked against their own accounts.

The scheduler queue is deliberately **reported, not probed**. The only SQS operation the product performs is
`SendMessageBatch` (the Loader enqueuing job executions), and testing it would publish a real message to a queue
shared with the whole dev environment. The receiving side is the deployed `BeamableScheduler.Dispatcher` Lambda,
which SQS invokes under its own execution role, so nothing local reads the queue either. If you need local
scheduling, the services role needs `sqs:SendMessageBatch` on that queue — but being denied any *other* SQS
action (`GetQueueAttributes`, say) tells you nothing, because nothing calls it.

Two details that matter:

- The secret and bucket checks run with **assumed-role credentials**, mirroring what the backend does. Checking
  them with your base profile would pass for a principal that cannot assume the role at all.
- Roles are tried **directly first, then chained through the services role**. Some roles trust the platform
  service role rather than individual developers; a chained success is reported as
  `(via the services role)` because it is a materially different answer from a flat failure — it means the role
  is reachable and you do *not* need a trust-policy change.

### What an administrator may need to do

Every failing check prints the real AWS error followed by the action that fixes it. The two that typically need
someone with AWS admin rights:

1. **Add the developer's IAM principal to the role's trust policy** (`sts:AssumeRole`). This is the usual
   new-developer step. The check names the exact principal and role ARN.
2. **Create the JWT signing secret** (`beamable.jwt.signingKey.local` in `us-west-2` by default) or grant the
   services role `secretsmanager:GetSecretValue` on it.

Credentials themselves are yours to configure — `aws configure`, or `aws sso login` if your org uses SSO. The
backend reads the profile named by `aws.default.profile.name`, which defaults to `default`.

---

## Options

```
beam local setup [--toolchain-dir <path>]   # install into / reuse from this directory
                 [--only jdk8,maven,dotnet,node,scala-config,portal-config,aws]
                 [--skip ...]
                 [--force]                  # re-download; overwrite generated config files
                 [--prefer-system]          # adopt an installed tool if it matches the pin
                 [--offline]                # install only from the download cache
                 [--dry-run]                # report what would happen, change nothing
                 [--github-token <token>]   # for the generated config files
                 [--aws-region <region>]    # default us-west-2
                 [--config <manifest>]      # default .beamable/local-stack.json
                 [--scala-dir <path>] [--api-dir <path>] [--portal-dir <path>]  # only if there is no manifest yet
```

The toolchain directory is resolved from `--toolchain-dir`, then `$BEAM_TOOLCHAIN_DIR`, then
`~/.beamable-toolchain`. It both installs into and reuses that directory, so **pointing several workspaces at one
directory shares a single install** and a second run downloads nothing.

Setup is idempotent: re-running with everything in place does no network I/O and exits 0.

> **Do not put the toolchain inside a `.beamable` folder.** That folder is the marker for a Beamable
> *workspace*: a `~/.beamable` would make the CLI treat your whole home directory as one, which misfires the
> first-run telemetry consent prompt and makes every beam command run outside a workspace fail with
> `Failed to read input in non-interactive mode`. Setup warns if you point `--toolchain-dir` inside one.


---

## Layout

```
<toolchain-dir>/
  toolchain.json      # what is installed: version, home, and whether it was downloaded or adopted
  downloads/          # verified archives, reused across machines and workspaces
  jdk8/<version>/
  maven/3.9.9/
  dotnet/10.0.100/
  node/v22.x.y/
```

The manifest gains a `toolchain` block plus a matching `javaHome`. **No steps are rewritten** — your edited step
list, service selection and JVM flags survive a setup run untouched.

---

## How the manifest uses it

Steps reference commands through tokens that `beam local up` substitutes:

| Token | Resolves to |
|---|---|
| `${java}` | The JDK 8 **home** (used as `${java}/bin/java`) |
| `${maven}` | The `mvn` executable |
| `${npm}`, `${node}` | The `npm` / `node` executables |
| `${dotnet}` | The `dotnet` executable |

**Every token falls back to the bare command name when no toolchain is recorded**, so a manifest written with
them still runs on a machine where setup was never used, and an older manifest keeps working unchanged.

Substituting the command is not enough on its own, so `up` also **prepends the toolchain's `bin` directories to
each step's `PATH` and pins `JAVA_HOME`**. Every one of these tools execs another: `npm` execs `node`, `mvn`
execs `java`, `dotnet build` resolves SDKs. Without that, a toolchain-pinned `mvn` would still compile the Scala
reactor with whatever JDK is first on your `PATH` — the exact drift the toolchain exists to remove. It is also
what makes an *older* manifest, whose steps still say bare `mvn`/`npm`, pick up the toolchain anyway.

---

## Before you can run setup

Setup cannot install the thing that runs it. On a completely fresh machine:

1. Install the [.NET SDK](https://dotnet.microsoft.com/download).
2. `dotnet tool install -g Beamable.Tools`
3. Install [Docker Desktop](https://docs.docker.com/get-docker/) and start it.
4. Clone `BeamableAPI`, `BeamableBackend`, `BeamableProduct` and the portal (setup does not clone them).
5. `beam local setup`

Setup still installs its own *pinned* .NET SDK, so the builds do not depend on whichever SDK launched the CLI.

---

## Troubleshooting

**`The config profile () could not be found`** — an `AWS_PROFILE` set to an empty string. Unset it rather than
blanking it.

**A tool shows `source=system` in `beam local validate`** — it works today and can change under you tomorrow.
Run `beam local setup --only <tool>` to pin it.

**`os.arch = x86_64` from the JDK on an Apple Silicon Mac** — you are on the Adoptium x64 build under Rosetta.
`beam local setup --only jdk8 --force` replaces it with the native aarch64 Zulu build.

**Checksum mismatch** — the download was corrupted or the upstream artifact changed. Nothing is installed when
this happens; re-run, and the bad cache entry is discarded automatically.

**The portal loads but the local test account cannot log in** — `VITE_API_BASE` is unset, so the portal is
logging in against production. `beam local validate` reports this as a failing `portal env` check; fix it with
`beam local setup --only portal-config`, then hard-refresh the browser.

**Offline / restricted network** — run setup once somewhere with access, then copy `<toolchain-dir>/downloads/`
to the target machine and use `--offline`.
