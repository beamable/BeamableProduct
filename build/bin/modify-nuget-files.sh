# review inputs
echo "ENVIRONMENT = ${ENVIRONMENT}"
echo "VERSION = ${VERSION}"

# update shipped version number
sed -i "s/__REPLACE_PACKED_BEAMABLE_VERSION__/${VERSION}/" microservice/microservice/Targets/Beamable.Microservice.Runtime.props
sed -i "s/__REPLACE_PACKED_BEAMABLE_VERSION__/${VERSION}/" cli/beamable.common/Targets/Beamable.Common.props
