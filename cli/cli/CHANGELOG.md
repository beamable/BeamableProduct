# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `beam docs` creates and uploads CLI documentation to Readme.com

## [1.16.0]

### Added

- `beam org new` creates a new Beamable organization
- `beam project logs` tails service logs
- `CLIException` now return error codes with valuable semantics (unknown and reportable errors are error code `1`;
  anything above 1 is a usage error that a user should be able fix)

### Changed

- fix path issues in `project new` with different names for solution and project
- Split `services deploy [--remote]` command into `services deploy` (for remote) and `services run` for running services
  in local docker

## [1.15.2]

### Added

- Project commands such as `project new`
- Basic app structure
