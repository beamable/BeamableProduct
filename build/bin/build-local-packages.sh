#!/bin/bash

set -euo pipefail

VERSION="${1:?Package version is required}"
OUTPUT_DIR="${2:?Package output directory is required}"
COPY_TO_UNITY="${3:-false}"
SOLUTION=./build/LocalBuild/LocalBuild.sln

dotnet restore "$SOLUTION"
dotnet build "$SOLUTION" --configuration Release \
  -p:PackageVersion="$VERSION" -p:CombinedVersion="$VERSION" \
  -p:InformationalVersion="$VERSION" -p:Warn=0 -p:BeamBuild=true

if [[ "$COPY_TO_UNITY" == true ]]; then
  # The target runs the CLI with --no-build against the Release output above.
  dotnet build cli/beamable.common -f net10.0 -t:CopyCodeToUnity \
    -p:BEAM_COPY_CODE_TO_UNITY=true \
    -p:BeamCopyCommonFlags="--no-build -c Release"
fi

dotnet pack "$SOLUTION" --configuration Release --no-build -o "$OUTPUT_DIR" \
  -p:PackageVersion="$VERSION" -p:CombinedVersion="$VERSION" \
  -p:InformationalVersion="$VERSION" -p:SKIP_GENERATION=true -p:BeamBuild=true

bash ./build/bin/check-local-packages.sh "$OUTPUT_DIR" "$VERSION"
