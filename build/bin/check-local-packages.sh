#!/bin/bash

set -euo pipefail

PACKAGE_DIR="${1:?Package directory is required}"
VERSION="${2:?Package version is required}"

packages=(
  Beamable.Common
  Beamable.Microservice.Runtime
  Beamable.Microservice.SourceGen
  Beamable.Server.Common
  Beamable.Templates
  Beamable.Tooling.Common
  Beamable.Tools
  Beamable.UnityEngine
  Beamable.UnityEngine.Addressables
  beamable.microservice.otel.exporter
  beamable.otel.common
)

for package in "${packages[@]}"; do
  package_file="$PACKAGE_DIR/$package.$VERSION.nupkg"
  if [[ ! -s "$package_file" ]]; then
    echo "Missing local package: $package_file" >&2
    exit 1
  fi
done

echo "Validated ${#packages[@]} local packages at version $VERSION"
