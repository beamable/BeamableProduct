#!/bin/bash

# Shared helper: runs the Beamable CLI from source in this repository.
#
# Sourced by setup-web.sh / dev-web.sh / teardown-web.sh, which are thin wrappers around
# `beam web ...` commands. The CLI owns all of the real logic; these scripts only marshal paths.
#
# Override the target framework if your toolchain differs:
#   BEAM_CLI_FRAMEWORK=net9.0 ./dev-web.sh
#
# Set BEAM_CLI_NO_BUILD=1 to skip the rebuild between repeated runs (faster, but stale if you
# just changed CLI source).

BEAM_CLI_FRAMEWORK="${BEAM_CLI_FRAMEWORK:-net10.0}"

# Convert a POSIX path to a native Windows path under Cygwin/MSYS, so the native `dotnet` binary and
# the .NET CLI (neither of which understands /cygdrive/... or /c/... paths) receive a path they can
# resolve. A no-op on Linux/macOS, or anywhere cygpath is unavailable. `-m` emits a forward-slash
# ("mixed") Windows path, which survives bash quoting without backslash escaping and is accepted by
# both Windows and .NET; it also preserves spaces, so "C:/Users/Some Name/..." passes through intact
# when quoted. Callers must quote the result.
beam_native_path() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -m "$1"
  else
    printf '%s' "$1"
  fi
}

# Runs `beam <args...>` via `dotnet run`, from the repository root.
beam_cli() {
  local repo_root="$1"; shift

  # dotnet consumes --project itself, BEFORE our CLI code runs, so this path in particular must be
  # native: a Cygwin/MSYS path here is what produces "the provided file path does not exist".
  local project_path
  project_path="$(beam_native_path "$repo_root/cli/cli")"

  local run_args=(run -f "$BEAM_CLI_FRAMEWORK" --project "$project_path")
  if [ -n "$BEAM_CLI_NO_BUILD" ]; then
    run_args+=(--no-build)
  fi

  echo "  [cmd] dotnet ${run_args[*]} -- $*"
  dotnet "${run_args[@]}" -- "$@"
}
