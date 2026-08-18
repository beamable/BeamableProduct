#!/bin/bash

# Shared helper: runs the Beamable CLI from source in this repository.
#
# Sourced by setup-web.sh / dev-web.sh / teardown-web.sh, which are thin wrappers around
# `beam web ...` commands. The CLI owns all of the real logic (including path handling), which is
# why these scripts no longer need the cygpath dance they used to.
#
# Override the target framework if your toolchain differs:
#   BEAM_CLI_FRAMEWORK=net9.0 ./dev-web.sh
#
# Set BEAM_CLI_NO_BUILD=1 to skip the rebuild between repeated runs (faster, but stale if you
# just changed CLI source).

BEAM_CLI_FRAMEWORK="${BEAM_CLI_FRAMEWORK:-net10.0}"

# Runs `beam <args...>` via `dotnet run`, from the repository root.
beam_cli() {
  local repo_root="$1"; shift

  local run_args=(run -f "$BEAM_CLI_FRAMEWORK" --project "$repo_root/cli/cli")
  if [ -n "$BEAM_CLI_NO_BUILD" ]; then
    run_args+=(--no-build)
  fi

  echo "  [cmd] dotnet ${run_args[*]} -- $*"
  dotnet "${run_args[@]}" -- "$@"
}
