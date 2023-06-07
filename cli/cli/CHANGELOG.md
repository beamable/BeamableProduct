# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
- Added `project add` command that allows to create new project and add it to an existing solution

### Added

- `beam org new` creates a new Beamable organization
- `beam project logs` tails service logs

### Changed

- fix path issues in `project new` with different names for solution and project
- Split `services deploy [--remote]` command into `services deploy` (for remote) and `services run` for running services
  in local docker

## [1.15.2]

### Added

- Project commands such as `project new`
- Basic app structure
