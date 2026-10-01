# Beamable Custom Otel Collector

Running `./build.sh --version 0.0.123` from this folder builds binaries for all six supported
platforms and installs them under the local app data directory. Set `OUTPUT_DIRECTORY` to build
into another directory.

PR CI builds the collector once, archives the six binaries and `clickhouse-config.yaml`, then
passes the archive to `setup.sh` through `BEAM_OTEL_COLLECTOR_ARCHIVE`. The setup script uses
`build.sh --archive <path>` to install it without Go. The archive path may be relative to the
repository root or absolute.

## Schemas
- Setup queries for ClickHouse live in the `Schemas/` folder.

# Contributing
This project has the same [contribution policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#Contributing) as the main repository.

# License
This project has the same [license policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#License) as the main repository.
