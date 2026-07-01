#!/usr/bin/env bash
#
# run-local-stack.sh
#
# One-command bring-up of the local Beamable loop (order matters):
#   1. C# Gateway + Caddy proxy     (BeamableAPI, Gateway :5000, Caddy :8080) — the HTTP gateway
#   2. Scala backend services       (BeamableBackend tools/*, backing services as HOST JVMs)
#   3. Portal frontend              (agentic-portal Vite dev server :4950)
#   4. Microservice(s)              (beam project run)
#   5. Portal extension(s)          (beam project run --portal-url ...)
#
# The C# stack starts FIRST — it IS the HTTP gateway (:5000) and hosts discovery the Scala
# backing services resolve against; otherwise they loop on "Binding not found".
# Scala step: launches every runnable module under tools/ in parallel as a host JVM
# (java -cp <jar>:<deps> <mainClass>, Temurin 8), exactly like IntelliJ, using server.conf so
# localhost/set-core-aliases resolve to your infra (mongo :27015/:27017, ActiveMQ :61616). The
# legacy Scala `gateway` module is EXCLUDED (the gateway is the C# stack); adjust via SCALA_EXCLUDE.
# Also starts the one redis container the services need (:6375). Requires the jars built.
# --skip-scala to run them in IntelliJ yourself; SCALA_SERVICES="..." to pin a subset.
#
# Backend host = http://localhost:8080 (Caddy). Portal UI = http://localhost:4950.
# The CLI used is the freshly-built local build (Beamable.Tools.dll) so it exercises
# the --portal-url fix.
#
# Foreground orchestrator: each piece starts in the background, all logs are tailed
# here, and Ctrl+C tears everything (this script started) back down.
#
# Usage:
#   scripts/run-local-stack.sh [--skip-scala] [--skip-api] [--skip-portal]
#                              [--skip-services] [--skip-extensions]
#                              [--stop-docker-on-exit] [--no-build]
#   SCALA_SERVICES="gateway auth realms" scripts/run-local-stack.sh   # Scala subset
#   SCALA_CP_REFRESH=1 scripts/run-local-stack.sh                     # rebuild classpaths
#
set -uo pipefail
# Job-control / monitor mode: each background job becomes its own process group,
# so `kill -- -$pid` in cleanup() tears down the whole child tree (e.g. the
# `dotnet run` child the microservice spawns) rather than orphaning grandchildren.
set -m

# ---------------------------------------------------------------------------
# Configuration — edit these to match your machine / what you want to run.
# ---------------------------------------------------------------------------
SCALA_DIR="${SCALA_DIR:-/Users/felipearruda/Documents/Work/BeamableBackend}"
API_DIR="${API_DIR:-/Users/felipearruda/Documents/Work/BeamableAPI}"
PORTAL_DIR="${PORTAL_DIR:-/Users/felipearruda/Documents/Work/agentic-portal}"
CLI_DIR="${CLI_DIR:-/Users/felipearruda/Documents/Work/BeamableProduct/cli}"

# The beam workspace (the directory that holds .beamable/) — also where the
# microservices and portal extensions live.
WORKSPACE_DIR="${WORKSPACE_DIR:-$PORTAL_DIR}"

# Endpoints.
HOST="${HOST:-http://localhost:8080}"          # backend API (Caddy)
PORTAL_URL="${PORTAL_URL:-http://localhost:4950}"  # portal UI (Vite dev server)
GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"

# What to run. Space-separated beam ids.
SERVICES=(${SERVICES:-CampaignService})
EXTENSIONS=(${EXTENSIONS:-sample})

# Freshly-built local CLI (net10.0). Built by this script unless --no-build.
BEAM_DLL="${BEAM_DLL:-$CLI_DIR/cli/bin/Debug/net10.0/Beamable.Tools.dll}"

# ASPNETCORE_ENVIRONMENT for the Gateway. Defaults to "Local" so it loads appsettings.Local.json
# (which provides DD_SERVICE/DD_ENV/DD_VERSION, mongo, etc.) — the same env the IDE run uses.
# Without it the binary runs as Production and crashes on OpenTelemetry (empty serviceName).
# Use "LocalNoActors" to run the gateway without the in-process actor host.
GATEWAY_ASPNETCORE_ENV="${GATEWAY_ASPNETCORE_ENV:-Local}"

# The built C# Gateway binary to launch (not `dotnet run`). Built automatically if missing.
GATEWAY_BIN="${GATEWAY_BIN:-$API_DIR/BeamableGateway/bin/Debug/net10.0/BeamableGateway}"

# ---------------------------------------------------------------------------
# Flags
# ---------------------------------------------------------------------------
# Scala backend is started by default — as HOST JVMs (the same way IntelliJ runs them), so
# they use your existing infra. Pass --skip-scala if you'd rather run them in IntelliJ.
SKIP_SCALA=0; SKIP_API=0; SKIP_PORTAL=0; SKIP_SERVICES=0; SKIP_EXTENSIONS=0
STOP_DOCKER_ON_EXIT=0; NO_BUILD=0
for arg in "$@"; do
  case "$arg" in
    --skip-scala) SKIP_SCALA=1 ;;
    --with-scala) SKIP_SCALA=0 ;;   # accepted for symmetry; this is already the default
    --skip-api) SKIP_API=1 ;;
    --skip-portal) SKIP_PORTAL=1 ;;
    --skip-services) SKIP_SERVICES=1 ;;
    --skip-extensions) SKIP_EXTENSIONS=1 ;;
    --stop-docker-on-exit) STOP_DOCKER_ON_EXIT=1 ;;
    --no-build) NO_BUILD=1 ;;
    -h|--help) grep -E '^#( |$)' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown flag: $arg (try --help)"; exit 2 ;;
  esac
done

# ---------------------------------------------------------------------------
# Logging / process tracking
# ---------------------------------------------------------------------------
LOG_DIR="$(mktemp -d "${TMPDIR:-/tmp}/beam-local-stack.XXXXXX")"
PIDS=()         # background PIDs this script owns
TAIL_PID=""

c_blue=$'\033[34m'; c_green=$'\033[32m'; c_yellow=$'\033[33m'; c_red=$'\033[31m'; c_reset=$'\033[0m'
say()  { echo "${c_blue}==>${c_reset} $*"; }
ok()   { echo "${c_green}  ok${c_reset} $*"; }
warn() { echo "${c_yellow}  ! ${c_reset} $*"; }
die()  { echo "${c_red}error:${c_reset} $*" >&2; exit 1; }

cleanup() {
  trap - INT TERM EXIT
  echo
  say "Shutting down (this may take a few seconds)..."
  [[ -n "$TAIL_PID" ]] && kill "$TAIL_PID" 2>/dev/null || true
  # Kill our background processes and their children.
  for pid in "${PIDS[@]:-}"; do
    [[ -z "$pid" ]] && continue
    kill -TERM "-$pid" 2>/dev/null || kill -TERM "$pid" 2>/dev/null || true
  done
  sleep 1
  for pid in "${PIDS[@]:-}"; do
    [[ -z "$pid" ]] && continue
    kill -KILL "-$pid" 2>/dev/null || kill -KILL "$pid" 2>/dev/null || true
  done
  if [[ "$STOP_DOCKER_ON_EXIT" == "1" ]]; then
    say "Stopping docker containers..."
    [[ "$SKIP_API" == "0" ]]   && (cd "$API_DIR" && docker compose down) 2>/dev/null || true
    [[ "$SKIP_SCALA" == "0" ]] && (cd "$SCALA_DIR/docker/local" && docker compose down) 2>/dev/null || true
  else
    warn "Docker containers left running (deps stay warm). Stop them with --stop-docker-on-exit, or:"
    warn "  (cd $API_DIR && docker compose down)"
    warn "  (cd $SCALA_DIR/docker/local && docker compose down)   # stops the redis container"
  fi
  say "Logs are in: $LOG_DIR"
  ok "Done."
}
trap cleanup INT TERM EXIT

# Start a long-running command in its own process group, logging to a file.
# usage: start_bg <label> <logfile> <workdir> <command...>
start_bg() {
  local label="$1" logfile="$2" workdir="$3"; shift 3
  say "Starting ${label} (log: ${logfile})"
  ( cd "$workdir" && exec "$@" ) >"$logfile" 2>&1 &
  local pid=$!
  PIDS+=("$pid")
  ok "${label} pid=${pid}"
}

# Poll a URL until it responds (any HTTP status) or times out. Prints a heartbeat every
# ~12s with the latest real log line, so a long boot doesn't look like a hang.
# usage: wait_for_http <url> <label> <timeout_seconds> [logfile]
wait_for_http() {
  local url="$1" label="$2" timeout="${3:-90}" logfile="${4:-}" waited=0 next_beat=12 hint
  say "Waiting for ${label} at ${url} (timeout ${timeout}s)..."
  while (( waited < timeout )); do
    if curl -s -o /dev/null --max-time 3 "$url"; then ok "${label} is up (after ${waited}s)"; return 0; fi
    sleep 3; waited=$((waited + 3))
    if (( waited >= next_beat )); then
      next_beat=$((next_beat + 12))
      hint=""
      if [[ -n "$logfile" && -f "$logfile" ]]; then
        # last non-stacktrace log line, trimmed — shows what the service is doing right now
        hint=$(grep -vE '^[[:space:]]*(at |\.\.\.|Caused by)' "$logfile" 2>/dev/null | tail -1 | cut -c1-110)
      fi
      say "  ...still starting ${label} — ${waited}/${timeout}s${hint:+  |  ${hint}}"
    fi
  done
  warn "${label} did not respond within ${timeout}s — continuing anyway."
  [[ -n "$logfile" && -f "$logfile" ]] && warn "  last log ($logfile):" && tail -3 "$logfile" | sed 's/^/      /'
  return 0
}

# ---------------------------------------------------------------------------
# Preflight
# ---------------------------------------------------------------------------
say "Preflight checks"
command -v dotnet >/dev/null || die "dotnet not found on PATH"
command -v docker >/dev/null || die "docker not found on PATH"
docker info >/dev/null 2>&1 || die "docker daemon is not running — start Docker Desktop first"
[[ "$SKIP_PORTAL" == "0" ]] && { command -v npm >/dev/null || die "npm not found on PATH"; }
[[ -d "$WORKSPACE_DIR/.beamable" ]] || die "no .beamable workspace at $WORKSPACE_DIR"
ok "tooling present"

# /etc/hosts aliases map docker service names (mongo_router, broker, ...) to localhost.
# Needed when host processes (or the Scala services) reach infra by those names.
if [[ "$SKIP_SCALA" == "0" ]] && ! grep -q "mongo_router" /etc/hosts 2>/dev/null; then
  warn "Scala docker-name aliases not found in /etc/hosts. If the Scala services can't"
  warn "reach mongo/broker, run this ONCE (uses sudo) and re-run: $SCALA_DIR/bin/set-core-aliases"
fi

# ---------------------------------------------------------------------------
# Build the local CLI so we test our own changes (the --portal-url fix).
# ---------------------------------------------------------------------------
if [[ "$NO_BUILD" == "0" ]]; then
  say "Building local CLI (dotnet build cli/cli.csproj)"
  dotnet build "$CLI_DIR/cli/cli.csproj" -clp:ErrorsOnly --nologo >"$LOG_DIR/cli-build.log" 2>&1 \
    || { cat "$LOG_DIR/cli-build.log"; die "CLI build failed"; }
  ok "CLI built"
fi
[[ -f "$BEAM_DLL" ]] || die "built CLI not found at $BEAM_DLL (run without --no-build, or set BEAM_DLL)"
beam() { dotnet "$BEAM_DLL" "$@"; }

# ---------------------------------------------------------------------------
# Point the portal frontend at the local backend.
# ---------------------------------------------------------------------------
if [[ "$SKIP_PORTAL" == "0" ]]; then
  say "Configuring portal frontend (VITE_API_BASE=$HOST)"
  ENV_LOCAL="$PORTAL_DIR/.env.local"
  if [[ -f "$ENV_LOCAL" ]] && grep -q '^VITE_API_BASE=' "$ENV_LOCAL"; then
    # macOS/BSD sed in-place
    sed -i '' "s#^VITE_API_BASE=.*#VITE_API_BASE=$HOST#" "$ENV_LOCAL"
  else
    echo "VITE_API_BASE=$HOST" >> "$ENV_LOCAL"
  fi
  ok "$ENV_LOCAL -> VITE_API_BASE=$HOST"
fi

# ---------------------------------------------------------------------------
# 1. C# stack FIRST — BeamableAPI docker deps + Caddy, then the C# Gateway.
#    The C# stack hosts the service-discovery the Scala services resolve against (e.g. the
#    `dbids` binding), so it MUST be up before the Scala services start — otherwise every
#    Scala service loops on "Binding not found".
# ---------------------------------------------------------------------------
if [[ "$SKIP_API" == "0" ]]; then
  say "Starting BeamableAPI docker deps + Caddy (docker compose up -d)"
  ( cd "$API_DIR" && docker compose up -d ) >"$LOG_DIR/api-docker.log" 2>&1 \
    || { cat "$LOG_DIR/api-docker.log"; die "BeamableAPI docker compose failed"; }
  ok "Caddy + deps up (:8080)"

  # Run the built BeamableGateway binary directly (not `dotnet run`). Build it first if missing.
  if [[ ! -x "$GATEWAY_BIN" ]]; then
    say "Building BeamableGateway (binary not found at $GATEWAY_BIN)"
    ( cd "$API_DIR" && dotnet build BeamableGateway ) >"$LOG_DIR/gateway-build.log" 2>&1 \
      || { tail -30 "$LOG_DIR/gateway-build.log"; die "BeamableGateway build failed (see $LOG_DIR/gateway-build.log)"; }
  fi
  [[ -x "$GATEWAY_BIN" ]] || die "BeamableGateway binary still not found at $GATEWAY_BIN"
  # Run from the binary's own directory so appsettings*.json (copied to the output) are found.
  # Don't expand an empty array under set -u (bash 3.2); branch on whether an env override is set.
  if [[ -n "$GATEWAY_ASPNETCORE_ENV" ]]; then
    start_bg "C# Gateway" "$LOG_DIR/gateway.log" "$(dirname "$GATEWAY_BIN")" \
      env "ASPNETCORE_ENVIRONMENT=$GATEWAY_ASPNETCORE_ENV" "$GATEWAY_BIN"
  else
    start_bg "C# Gateway" "$LOG_DIR/gateway.log" "$(dirname "$GATEWAY_BIN")" \
      "$GATEWAY_BIN"
  fi
  wait_for_http "$GATEWAY_URL" "Gateway" 180 "$LOG_DIR/gateway.log"
  wait_for_http "$HOST" "Caddy" 30
else
  warn "Skipping BeamableAPI / Gateway (--skip-api)"
fi

# ---------------------------------------------------------------------------
# 2. Scala backend services — as HOST JVMs, exactly like IntelliJ (no docker, no GUI).
#    Each service is launched as `java -cp <jar>:<deps> <mainClass>` with Temurin 8, using
#    the default server.conf — so localhost/set-core-aliases resolve to your existing infra
#    (BeamableAPI mongo on :27015/:27017, ActiveMQ on :61616). The only extra infra these need
#    is redis on :6375, which is BeamableBackend's own redis container (no name conflict).
#    Started AFTER the C# stack (step 1) so service discovery is available, and all in parallel.
# ---------------------------------------------------------------------------
if [[ "$SKIP_SCALA" == "0" ]]; then
  # Which Scala services to run: every runnable module under tools/ (has a <mainClass> AND a
  # built jar). Override by setting SCALA_SERVICES="svc1 svc2 ..." to pin a subset.
  # Curated set of Scala backing services to run (the ones that actually need to be up).
  # Override with SCALA_SERVICES="svc1 svc2 ..." to change the set.
  DEFAULT_SCALA_SERVICES="dbflake gateway auth account session content stats beamo realms announcements events groups history leaderboards cloud-saving"
  SCALA_SERVICES=(${SCALA_SERVICES:-$DEFAULT_SCALA_SERVICES})
  [[ ${#SCALA_SERVICES[@]} -gt 0 ]] || die "No Scala services selected."
  J8="$(/usr/libexec/java_home -v 1.8 2>/dev/null)"
  [[ -x "$J8/bin/java" ]] || die "Temurin/JDK 8 not found (/usr/libexec/java_home -v 1.8). The Scala services need Java 8."
  command -v mvn >/dev/null || die "mvn not found — needed to build each service's classpath."
  CP_CACHE="${TMPDIR:-/tmp}/beam-scala-cp"; mkdir -p "$CP_CACHE"

  # redis (:6375) — BeamableBackend's own redis container; no conflict with BeamableAPI infra.
  say "Starting redis (:6375) for the Scala services"
  ( cd "$SCALA_DIR/docker/local" && docker compose up -d --no-deps redis ) >"$LOG_DIR/scala-redis.log" 2>&1 \
    || warn "Could not start redis container — services will retry redis (see logs). Check $LOG_DIR/scala-redis.log"

  # Launch one Scala service as a host JVM (classpath is cached across runs; SCALA_CP_REFRESH=1 to rebuild).
  launch_scala_service() {
    local svc="$1"
    local jar mainClass cp cpfile
    jar=$(ls "$SCALA_DIR"/tools/"$svc"/target/*-1.0-SNAPSHOT.jar 2>/dev/null | grep -v sources | head -1)
    mainClass=$(grep -m1 -oE "<mainClass>[^<]+</mainClass>" "$SCALA_DIR/tools/$svc/pom.xml" 2>/dev/null | sed -E 's#</?mainClass>##g')
    if [[ -z "$jar" || -z "$mainClass" ]]; then
      warn "Skipping Scala service '$svc' (jar or mainClass missing — build it in IntelliJ / mvn package)"; return
    fi
    cpfile="$CP_CACHE/cp-$svc.txt"
    if [[ "${SCALA_CP_REFRESH:-0}" == "1" || ! -s "$cpfile" ]]; then
      say "Resolving classpath for $svc (first run; cached afterwards)"
      ( cd "$SCALA_DIR" && JAVA_HOME="$J8" mvn -q -pl "tools/$svc" dependency:build-classpath -Dmdep.outputFile="$cpfile" ) \
        >"$LOG_DIR/scala-cp-$svc.log" 2>&1 \
        || { warn "Classpath build failed for $svc (see $LOG_DIR/scala-cp-$svc.log)"; return; }
    fi
    # Prepend the module output dirs (like IntelliJ) BEFORE the ~/.m2 jars: the built
    # target/classes hold rendered config resources (e.g. core/awsglobal.conf with the aws
    # cloudfront secret) that are NOT packaged into the installed m2 jars. Without core's
    # target/classes the services throw "Missing config: aws.cloudfront..." and never register.
    cp="$SCALA_DIR/tools/$svc/target/classes:$SCALA_DIR/core/target/classes:$jar:$(cat "$cpfile")"
    start_bg "Scala: $svc" "$LOG_DIR/scala-$svc.log" "$SCALA_DIR" \
      env "JAVA_HOME=$J8" "$J8/bin/java" -cp "$cp" "$mainClass"
  }

  # Start every service in parallel (each start_bg backgrounds its JVM immediately).
  say "Starting ${#SCALA_SERVICES[@]} Scala services in parallel: ${SCALA_SERVICES[*]}"
  for svc in "${SCALA_SERVICES[@]}"; do
    launch_scala_service "$svc"
  done
  # These are backing services (RPC over mongo-registered bindings) — there is no single HTTP
  # "gateway" to poll here (the HTTP gateway is the C# stack on :5000, already up). Give them a
  # short settle to connect to infra and register, then move on; they keep initializing in the
  # background (watch scala-*.log). SCALA_SETTLE=N to change the pause.
  settle="${SCALA_SETTLE:-20}"
  say "Letting the Scala services connect + register with discovery (~${settle}s; they keep starting in the background)"
  waited=0; while (( waited < settle )); do sleep 5; waited=$((waited+5)); say "  ...Scala services warming up — ${waited}/${settle}s"; done
  ok "Scala services launched (${#SCALA_SERVICES[@]}); see $LOG_DIR/scala-*.log for per-service status"
else
  say "Skipping Scala backend (--skip-scala) — run the tools/ services yourself (e.g. IntelliJ)"
fi

# ---------------------------------------------------------------------------
# 3. Portal frontend (Vite dev server).
# ---------------------------------------------------------------------------
if [[ "$SKIP_PORTAL" == "0" ]]; then
  if [[ ! -d "$PORTAL_DIR/node_modules" ]]; then
    say "Installing portal deps (npm install)"
    ( cd "$PORTAL_DIR" && npm install ) >"$LOG_DIR/portal-install.log" 2>&1 \
      || { cat "$LOG_DIR/portal-install.log"; die "npm install failed (private @beamable scope? try 'npm run install:public' in $PORTAL_DIR)"; }
    ok "portal deps installed"
  fi
  start_bg "Portal frontend" "$LOG_DIR/portal.log" "$PORTAL_DIR" npm run dev
  wait_for_http "$PORTAL_URL" "Portal" 120 "$LOG_DIR/portal.log"
else
  warn "Skipping portal frontend (--skip-portal)"
fi

# ---------------------------------------------------------------------------
# 4. Microservice(s).
# ---------------------------------------------------------------------------
if [[ "$SKIP_SERVICES" == "0" ]]; then
  for svc in "${SERVICES[@]}"; do
    start_bg "Microservice: $svc" "$LOG_DIR/service-$svc.log" "$WORKSPACE_DIR" \
      dotnet "$BEAM_DLL" project run --ids "$svc" --host "$HOST" \
        --logs v --no-log-file
  done
else
  warn "Skipping microservices (--skip-services)"
fi

# ---------------------------------------------------------------------------
# 5. Portal extension(s) — passing --portal-url so the landing URL points local.
# ---------------------------------------------------------------------------
if [[ "$SKIP_EXTENSIONS" == "0" ]]; then
  for ext in "${EXTENSIONS[@]}"; do
    start_bg "Portal extension: $ext" "$LOG_DIR/extension-$ext.log" "$WORKSPACE_DIR" \
      dotnet "$BEAM_DLL" project run --ids "$ext" --host "$HOST" --portal-url "$PORTAL_URL" \
        --logs v --no-log-file
  done
else
  warn "Skipping portal extensions (--skip-extensions)"
fi

# ---------------------------------------------------------------------------
# Done — tail everything until Ctrl+C.
# ---------------------------------------------------------------------------
echo
say "Stack is starting. Backend=$HOST  Portal=$PORTAL_URL"
say "Open the portal URL printed by each extension's '${c_green}Portal URL:${c_reset}' log line."
say "Tailing logs from $LOG_DIR — press Ctrl+C to stop everything."
echo

# Tail all logs; keep running in foreground so the trap fires on Ctrl+C.
tail -n +1 -F "$LOG_DIR"/*.log &
TAIL_PID=$!
wait "$TAIL_PID"
