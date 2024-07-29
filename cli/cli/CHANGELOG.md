# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0]

### Added
- `--unmask-logs` option will show full tokens in verbose logs
- `--no-log-file` option will prevent verbose logs from being written to temp file
- `beam project enable` and `beam project disable` commands will set the `<BeamEnabled>` setting.
- Can pass MSBuild dlls location through environment variable to the CLI
- `beam services build` uses Docker Buildkit to build Standalone Microservice images
- `beam services bundle` produces a `.tar` file for a Standalone Microservice
- `--docker-cli-path` option overrides docker cli location used for Buildkit
- `net8.0` support for Standalone Microservices
- `beam project ps --raw` includes an `executionVersion` representing the version of the Beamable SDK being used in the service

### Changed
- Standalone Microservices are created with `net8.0` by default

### Fixed
- JSON output will correctly render optional types

## [2.0.1] - 2024-06-17

### Added
- `--no-filter` option to `beam listen server`

### Fixed
- `beam project stop` will stop services running in docker
- `beam service ps`  was not working when calling it because it was trying to get the ImageId of storage objects
- common lib handling uses `.` as a default path instead of the empty string 
- `UpdateDockerfile` update to fix common lib handling for docker builds

### Changed
 - `beam service ps` now doesn't have the `--remote` flag and always return information updated with both local and remote

## [2.0.0] - 2024-05-24

### Added

- `--raw` option will output commands in machine readable JSON format
- `beam listen player` command monitors notifications sent to the logged in CLI user
- `beam listen server` command monitors server events
- `beam project run` command will run a dotnet project
- `beam project stop` will stop running dotnet projects
- `beam project build` will build a dotnet project
- Unreal Microservice client generation now correctly generates non-primitives used in C#MS signatures, including containers and nested containers.
- Unreal Microservice client generation now happens for all microservices at once and support shared code.
- `BEAM_DOCKER_URI` environment variable will override docker connection uri
- Added Bulk Edit Content command to the CLI (`beam content bulk-edit`)

### Changed

- Updates the Serilog and Spectre dependencies.
- Updated dotnet framework dependencies to maximize and enforce compatibility (minimum dotnet 6)
- Commands will use `--raw` output automatically when piped to another process
- Root commands with no action will automatically print `--help`
- `docs`, `profile`, `generate-interface` and http commands are marked as `[INTERNAL]`
- Commands automatically save the latest log to the temp path.
- Commands will log single outputs as JSON by default.
- Console logging no longer includes log level and timestamp.
- Logs containing GUID based tokens will be masked and only the last 4 characters will be shown.
- Unreal Microservice Client Code generation no longer appends the microservice name to serializable types.
- Microservices install with current CLI version
- Standalone Microservices no longer have a `LoadEnvironmentVariables` method, and connection strings are handled in the existing `Prepare` method.
- `beam project generate-env` command writes a blank `.env` file and returns connection strings over STDOUT instead.
- Generating microservice clients for Unreal now outputs them to a Plugin called `[ProjectName]MicroserviceClients` instead of placing it in some existing module.
- Internal commands are hidden unless `--help-all` is passed.

### Fixed

- `project add` Dockerfile path fixes.
- `project new-storage` path fixes.
- Progress bars and logs do not appear side by side.
- Fixed issue that caused incorrect code-gen of Unreal wrapper types in SAMS-Client code
- Docker will not connect at common unix home directory if `/var/run/docker.sock` is not available

### Removed

- `beam content` no longer directly opens content folder.

## [1.19.17] - 2024-04-04
### Changed
- `BEAM_DOCKER_URI` environment variable will override docker connection uri
- Standalone Microservices no longer have a `LoadEnvironmentVariables` method, and connection strings are handled in the existing `Prepare` method.
- `beam project generate-env` command writes a blank `.env` file and returns connection strings over STDOUT instead.
- Docker will not connect at common unix home directory if `/var/run/docker.sock` is not available

## [1.19.16] - 2024-03-2
no changes

## [1.19.15] - 2024-03-07
### Fixed
- Progress bars and logs do not appear side by side.
- Unreal Microservice client generation now correctly generates non-primitives used in C#MS signatures

## [1.19.14] - 2024-02-06

### Fixed
- Docker path issue when adding storage objects

## [1.19.13] - 2024-02-05
no changes

## [1.19.12] - 2024-01-22

### Added
- Better validation and error messages for add-unreal-project command code-path;
- Unreal Microservice client generation now correctly identifies whether or not the OSS UE Plugin is there and, if so, it'll add the microservices code to that module instead.
- Unreal Microservice client generation now checks whether or not the linked project is using the OnlineSubsystemBeamable plugin and, if so, checks if it is configured correctly. This catches the case where people add the OSS after the Microservice was already added to the project modules;
- CLI will now check if its necessary to run Unreal's Generate VS Project Files command after generating client code and, if so, will run and wait for it as part of the generate-client command (it is needed when new client callables are added/removed);

### Fixed
- Fixed issue that caused paths not to be stored relative to the `.beamable` folder correctly
- Fixed issue that caused incorrect `\\` to be used instead of `/`
- Fixed serializer generation to correctly use `TCHAR` as opposed to `wchar_t`
- Fixed `FGuid` serialization to always serialize with digits + hyphen + lower case;

## [1.19.11] - 2024-01-12

### Added
- `beam config realm` command suite for working with realm config via the CLI.

### Fixed
- Stack traces from Dependency Injection, `GetService`, show inner stack trace instead of Reflection based stack trace.

## [1.19.10] - 2024-01-05

### Fixed
- fixed issue an issue that would cause an NRE if an existing service had no federated component when running services deploy command

## [1.19.9] - 2023-12-20

no changes

## [1.19.8] - 2023-12-15

### Fixed

- OpenAPI generation fixes.

## [1.19.7] - 2023-11-29

no changes

## [1.19.6] - 2023-11-22

no changes

## [1.19.5] - 2023-11-15

### Added

- Auto generation handles trials and session services.

### Changed

- API code-gen now generates structs with properly initialized fields.

## [1.19.4] - 2023-11-02

### Fixed
- `--reporter-use-fatal` channel supports JSON strings

## [1.19.3] - 2023-10-26

no changes

## [1.19.2] - 2023-10-11

### Fixed
- `beam services deploy` no longer times out.

### Changed
- Templates update with refactor to improve it receiving updates and fixes in the future.

## [1.19.1] - 2023-09-22

no changes

## [1.19.0] - 2023-09-20

### Added

- `content tag`, `content tag add/rm` commands. We are supporting passing content ids as list seperated by commas or as a regex pattern.
- Improved `CliRequester` error logs.

### Fixed

- Fixed if older version of templates are installed, allow `beam project new` continue without installing latest templates
- Detect no services found scenario in `beam services ps`.
- Fixed Powershell users having blue-on-blue text when selecting options.
- When executing a microservice that depends on a storage through the IDE, the storage was not booting up in docker.
- Add more information logs when executing C#MS through the IDE.

## [1.18.0] - 2023-09-01

### Added

- Support for storing local content from multiple namespaces.
- Filter out storage objects from `beam services enable` selection wizard.
- `beam project new` now have --disable flag to create service as disabled on publish.
- Ability to retry again if alias or username or password is entered incorrectly.
- Commands which require config to work will be cancelled if no config is available.

### Fixed

- `beam services deploy` fetches current realm snapshot before deploy, allowing publication of new services without old services.
- Validate cid and resolve alias to cid on Microservice deploy.
- Standalone microservices now write federated components to the manifest when deployed.
- Creating a new project with NET 6.0 no longer fails to install templates.

### Changed

- `run-nbomber` cli command accepts a json file as body for request instead of an argument.

## [1.17.3] - 2023-08-21

### Added

- Add `Beamable.Common` as dependency to SAMS Common project

### Changed

- `beam project {new/add}` does update SAMS Beamable dependencies during project creation.
- `beam project update-unity-beam-package` command for installing and updating Beam packages in Unity projects.
- Requests commands (`beam {get/put/post/delete/me}`) always output result to console.

### Fixed

- Fix issue with archived realms showing in options when selecting realm
- Rerunning `beam services run` will detect code changes
- New projects created with `beam project new` will include the required `Microsoft.OpenApi.Readers` package
- If an internal `dotnet` command fails, `beam` will now emit the logs of the failed command
- Improved installed templates detection

## [1.17.2] - 2023-08-10

### Added

- Added missing XML documentation to CLI related code, enable `TreatWarningsAsErrors` in CLI project.

### Fixed

- `--log` option correctly changes desired log level.
- `beam project {new/add}` commands work if called from other directory than the one with Beamable config.

## [1.17.1] - 2023-08-10

### Added

- `beam services run` takes a `--force-amd-cpu-arch` flag that will force the built CPU architecture to `linux/amd64`
- Auto install Beamable templates
- Add `beam services stop` to stop locally running container for selected Beamo services
- Add refresh token to open-swagger url

### Fixed

- Fix issue with cli crash when linking unity/unreal project that scans over protected folder
- Fix `beam project open-swagger` and `beam project open-mongo` when service name or storage is not specified
- Fix `beam project open-swagger` and `beam project open-mongo` when multiple service name or storage exist in the same directory, user can now select the service name or storage to use
- `beam services deploy` will force build services to `linux/amd64` CPU architecture for usage on Beamable Cloud
- Fix `beam open-mongo` returning wrong command to execute when storage is not running
- Fix open-swagger case sensitivity problem

### Changed

- newly created service will have `ShouldBeEnabledOnRemote` as true in `BeamoServiceDefinition`
- when creating new storage, service dependencies are all selected by default

## [1.17.0] - 2023-07-27

### Added

- `beam project generate-ignore-file` command to generate an ignore file in config folder for given VCS
- `beam services get-connection-string my-storage-name` retrieves the local connection string
  for the specified micro-storage
- `beam services get-connection-string my-storage-name --remote` retrieves the remote connection string
  for the specified micro-storage
- Add `--quiet` to ignore confirmation step when retrieving connection string

## [1.16.2] - 2023-07-12

### Added

- create default `.gitignore` file during `beam init` if none is present
- `beam version` commands to manage CLI version install
- Add NET 7.0 as second target for CLI

### Fixed

- Fix issues with `ShowLoading` helper function caused by rethrowing
- Pull `mongo-express` in `project open-mongo` command if needed

### Changed

- If there is only one Microservice, `beam project open-swagger` work without passing Microservice ID
- If there is only one Storage, `beam project open-mongo` work without passing Storage ID

## [1.16.1] - 2023-06-28

### Added

- `beam services deploy` accepts an optional `--docker-registry-url`

### Fixed

- `CliRequester` incorrectly assuming token needs refreshing instead of failed request
- `beam services deploy` uses docker registry endpoint derived from call to `/basic/beamo/registry`

## [1.16.0] - 2023-06-27

### Added

- `beam org new` creates a new Beamable organization
- `beam project logs` tails service logs
- `CLIException` now return error codes with valuable semantics (unknown and reportable errors are error code `1`;
  anything above 1 is a usage error that a user should be able fix)
- `beam docs` creates and uploads CLI documentation to Readme.com
- Added `project add` command that allows to create new project and add it to an existing solution

### Changed

- fix path issues in `project new` with different names for solution and project
- Split `services deploy [--remote]` command into `services deploy` (for remote) and `services run` for running services
  in local docker
- Autogenerated SDK for Unity now includes proto-actor API.
- fix issues with failing `project new` command
- `beam project open-mongo` opens the browser to `localhost` instead of `0.0.0.0`

### Fixed

- `beam project generate-env` loads `.dll` files into new context, allowing for multiple versions of similar libraries

## [1.15.2] - 2023-05-18

### Added

- Project commands such as `project new`
- Basic app structure