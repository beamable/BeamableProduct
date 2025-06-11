#!/usr/bin/env bash

COLLECTOR_VERSION_FILE="microservice/beamable.tooling.common/Microservice/VersionManagement/collector-version.json"
PROPS_FILE="microservice/microservice/Targets/Beamable.Microservice.Runtime.props"
COLLECTOR_VERSION_PROPERTY="collectorVersion"
PROPS_PROPERTY_TO_SET="BeamCollectorVersion"
COMMENT_TAG="<!-- The proper value for BeamCollectorVersion will be injected below this line for non dev builds -->"


VERSION=$(jq -r ."$COLLECTOR_VERSION_PROPERTY" "$COLLECTOR_VERSION_FILE")

if [ "$VERSION" = "null" ] || [ -z "$VERSION" ]; then
  echo "Error: $COLLECTOR_VERSION_PROPERTY not found in $COLLECTOR_VERSION_FILE"
  exit 1
fi

VERSION=$(echo "$VERSION" | tr -d '\r\n')
PROPS_PROPERTY_TO_SET=$(echo "$PROPS_PROPERTY_TO_SET" | tr -d '\r\n')

awk -v tag="$COMMENT_TAG" -v prop="$PROPS_PROPERTY_TO_SET" -v ver="$VERSION" '
  $0 ~ tag {
    print
    print "\t\t<" prop ">" ver "</" prop ">"
    next
  }
  { print }
' "$PROPS_FILE" > "$PROPS_FILE.tmp" && mv "$PROPS_FILE.tmp" "$PROPS_FILE"